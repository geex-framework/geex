const { strings } = require("@angular-devkit/core");
const {
  chain,
  MergeStrategy,
  mergeWith,
  SchematicsException,
} = require("@angular-devkit/schematics");
const { packageVersion, selectTemplate, templateSource } = require("../template-utils");

function readManifest(tree) {
  const manifestPath = ".geex/modules.json";
  if (!tree.exists(manifestPath)) {
    throw new SchematicsException(`Module manifest not found at ${manifestPath}. Run add-module first.`);
  }
  return {
    manifestPath,
    manifest: JSON.parse(tree.read(manifestPath).toString("utf8")),
  };
}

function resolveTargetPath(manifestEntry, name, options) {
  if (manifestEntry?.path) {
    return manifestEntry.path.replace(/\\/g, "/");
  }
  return `${options.path}/${name}`;
}

function syncOneModule(options, name) {
  return (tree, context) => {
    const { manifestPath, manifest } = readManifest(tree);
    const manifestEntry = manifest.modules?.[name];
    if (!manifestEntry) {
      throw new SchematicsException(`Module '${name}' is not recorded in ${manifestPath}.`);
    }

    const targetPath = resolveTargetPath(manifestEntry, name, options);
    if (!tree.exists(`${targetPath}/index.ts`)) {
      throw new SchematicsException(`Module not found at ${targetPath}. Run add-module first.`);
    }

    const selectedTemplate = selectTemplate(manifestEntry.template);
    const shouldOverwrite = options.force || options.overwrite;
    const shouldInclude = filePath => {
      const normalized = String(filePath).replace(/^\//, "");
      if (!shouldOverwrite && (tree.exists(filePath) || tree.exists(normalized))) {
        context.logger.info(`skip existing ${filePath}`);
        return false;
      }
      return true;
    };
    const mergeStrategy = shouldOverwrite ? MergeStrategy.Overwrite : MergeStrategy.Default;
    // Sync only the recorded template. Never re-apply blank onto overlay modules,
    // or missing scaffold files would pollute customized installs (e.g. identity).
    const rules = [];
    if (selectedTemplate === "blank") {
      rules.push(mergeWith(templateSource("blank", options, name, targetPath, shouldInclude), mergeStrategy));
    } else {
      rules.push(
        mergeWith(
          templateSource(`extensions/${selectedTemplate}`, options, name, targetPath, shouldInclude),
          MergeStrategy.Overwrite,
        ),
      );
    }
    rules.push(t => {
      const nextManifest = JSON.parse(t.read(manifestPath).toString("utf8"));
      nextManifest.modules[name] = {
        ...nextManifest.modules[name],
        path: targetPath,
        templateVersion: packageVersion,
        syncedAt: new Date().toISOString(),
      };
      t.overwrite(manifestPath, `${JSON.stringify(nextManifest, null, 2)}\n`);
      return t;
    });

    return chain(rules)(tree, context);
  };
}

function syncModule(options) {
  return (tree, context) => {
    const { manifest } = readManifest(tree);
    const moduleNames = options.all
      ? Object.keys(manifest.modules || {})
      : options.name
        ? [strings.dasherize(options.name)]
        : [];

    if (moduleNames.length === 0) {
      throw new SchematicsException("Option 'name' is required, or pass --all to sync every recorded module.");
    }

    return chain(moduleNames.map(name => syncOneModule(options, name)))(tree, context);
  };
}

exports.syncModule = syncModule;
