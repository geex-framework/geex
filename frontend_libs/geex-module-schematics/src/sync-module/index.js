const { strings } = require("@angular-devkit/core");
const {
  chain,
  MergeStrategy,
  mergeWith,
  SchematicsException,
} = require("@angular-devkit/schematics");
const { selectTemplate, templateSource } = require("../template-utils");

function syncModule(options) {
  return (tree, context) => {
    if (!options.name) {
      throw new SchematicsException("Option 'name' is required.");
    }
    const name = strings.dasherize(options.name);
    const targetPath = `${options.path}/${name}`;
    const manifestPath = ".geex/modules.json";
    if (!tree.exists(`${targetPath}/index.ts`)) {
      throw new SchematicsException(
        `Module not found at ${targetPath}. Run add-module first.`,
      );
    }
    if (!tree.exists(manifestPath)) {
      throw new SchematicsException(`Module manifest not found at ${manifestPath}. Run add-module first.`);
    }

    const manifest = JSON.parse(tree.read(manifestPath).toString("utf8"));
    const manifestEntry = manifest.modules?.[name];
    if (!manifestEntry) {
      throw new SchematicsException(`Module '${name}' is not recorded in ${manifestPath}.`);
    }
    const selectedTemplate = selectTemplate(manifestEntry.template);
    const shouldOverwrite = options.force || options.overwrite;
    const shouldInclude = filePath => {
      if (!shouldOverwrite && tree.exists(filePath)) {
        context.logger.info(`skip existing ${filePath}`);
        return false;
      }
      return true;
    };
    const mergeStrategy = shouldOverwrite ? MergeStrategy.Overwrite : MergeStrategy.Default;
    const rules = [
      mergeWith(templateSource("blank", options, name, targetPath, shouldInclude), mergeStrategy),
    ];
    if (selectedTemplate !== "blank") {
      rules.push(
        mergeWith(
          templateSource(`extensions/${selectedTemplate}`, options, name, targetPath, shouldInclude),
          MergeStrategy.Overwrite,
        ),
      );
    }

    return chain(rules)(tree, context);
  };
}

exports.syncModule = syncModule;
