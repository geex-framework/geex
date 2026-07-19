const { strings } = require("@angular-devkit/core");
const {
  chain,
  MergeStrategy,
  mergeWith,
  SchematicsException,
} = require("@angular-devkit/schematics");
const {
  packageVersion,
  selectTemplate,
  templateSource,
  toCamelAlias,
} = require("../template-utils");

function updateRegistry(tree, options, context) {
  const registryPath = "src/app/modules/module-registry.ts";
  if (!tree.exists(registryPath)) {
    context.logger.warn(`Skip registry update: ${registryPath} not found`);
    return;
  }

  const name = strings.dasherize(options.name);
  const alias = toCamelAlias(name);
  let source = tree.read(registryPath).toString("utf8");

  if (source.includes(`from "./${name}"`)) {
    context.logger.info(`Registry already imports ./ ${name}`);
    return;
  }

  const importLine = `import * as ${alias} from "./${name}";\n`;
  const importMatches = [...source.matchAll(/^import .*;\r?\n/gm)];
  const lastImport = importMatches.at(-1);
  if (lastImport) {
    const insertAt = lastImport.index + lastImport[0].length;
    source = `${source.slice(0, insertAt)}${importLine}${source.slice(insertAt)}`;
  } else {
    source = `${importLine}${source}`;
  }

  source = source.replace(
    /(export const installedModules = \{[\s\S]*?)(\r?\n\} as const;)/,
    `$1\n  ${alias},$2`,
  );

  // auth is mounted outside authenticated shell via authLoadChildren
  if (name !== "auth" && !source.includes(`path: "${name}"`)) {
    const routeEntry = `  { path: "${name}", loadChildren: () => import("./${name}/${name}.routes").then(m => m.${alias}Routes) },\n`;
    source = source.replace(
      /(export const authenticatedModuleChildren: Routes = \[[\s\S]*?)(\];)/,
      `$1${routeEntry}$2`,
    );
  }

  // Place menu contribution into 系统及配置 group children by default
  if (name !== "mocking" && name !== "auth" && !source.includes(`...${alias}.menuContribution`)) {
    source = source.replace(
      /(\[\.\.\.identity\.menuContribution, \.\.\.settings\.menuContribution, \.\.\.tenant\.menuContribution)/,
      `[...identity.menuContribution, ...settings.menuContribution, ...tenant.menuContribution, ...${alias}.menuContribution`,
    );
  }

  const i18nKey = strings.classify(name);
  if (name !== "auth" && name !== "identity" && name !== "tenant") {
    if (!source.includes(`${i18nKey}: ${alias}.i18n["zh-CN"]`)) {
      source = source.replace(
        /(export const moduleI18nZhCN = \{[\s\S]*?)(\r?\n\};)/,
        `$1\n  ${i18nKey}: ${alias}.i18n["zh-CN"],$2`,
      );
    }
    if (!source.includes(`${i18nKey}: ${alias}.i18n["en-US"]`)) {
      source = source.replace(
        /(export const moduleI18nEnUS = \{[\s\S]*?)(\r?\n\};)/,
        `$1\n  ${i18nKey}: ${alias}.i18n["en-US"],$2`,
      );
    }
  }

  tree.overwrite(registryPath, source);
}

function updateModulesJson(tree, options, template) {
  const manifestPath = ".geex/modules.json";
  const name = strings.dasherize(options.name);
  let manifest = { modules: {} };
  if (tree.exists(manifestPath)) {
    manifest = JSON.parse(tree.read(manifestPath).toString("utf8"));
  }
  manifest.modules = manifest.modules || {};
  manifest.modules[name] = {
    path: `${options.path}/${name}`,
    template,
    templateVersion: packageVersion,
    installedAt: new Date().toISOString(),
  };
  if (tree.exists(manifestPath)) {
    tree.overwrite(manifestPath, JSON.stringify(manifest, null, 2) + "\n");
  } else {
    tree.create(manifestPath, JSON.stringify(manifest, null, 2) + "\n");
  }
}

function addModule(options) {
  return (tree, context) => {
    if (!options.name) {
      throw new SchematicsException("Option 'name' is required.");
    }
    const name = strings.dasherize(options.name);
    const targetPath = `${options.path}/${name}`;
    const selectedTemplate = selectTemplate(name);
    const shouldOverwrite = options.force || options.overwrite;
    if (tree.exists(`${targetPath}/index.ts`) && !shouldOverwrite) {
      throw new SchematicsException(
        `Module already exists at ${targetPath}. Use --force to overwrite (destructive).`,
      );
    }

    const mergeStrategy = shouldOverwrite ? MergeStrategy.Overwrite : MergeStrategy.Default;
    const rules = [];
    if (selectedTemplate === "blank") {
      rules.push(mergeWith(templateSource("blank", options, name, targetPath), mergeStrategy));
    } else {
      rules.push(
        mergeWith(
          templateSource(`extensions/${selectedTemplate}`, options, name, targetPath),
          MergeStrategy.Overwrite,
        ),
      );
    }
    rules.push(
      (t, ctx) => {
        updateRegistry(t, { ...options, name }, ctx);
        updateModulesJson(t, { ...options, name }, selectedTemplate);
        return t;
      },
    );

    return chain(rules)(tree, context);
  };
}

exports.addModule = addModule;
exports.toCamelAlias = toCamelAlias;
