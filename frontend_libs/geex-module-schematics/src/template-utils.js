const { strings } = require("@angular-devkit/core");
const { apply, applyTemplates, forEach, move, url } = require("@angular-devkit/schematics");

const { version: packageVersion } = require("../package.json");

const OVERLAY_TEMPLATES = new Set([
  "identity",
  "blob-storage",
  "approval-flows",
  "mocking",
  "auth",
  "settings",
  "tenant",
]);

function toCamelAlias(name) {
  const dashed = strings.dasherize(name);
  return dashed.replace(/-([a-z])/g, (_, character) => character.toUpperCase()).replace(/^[A-Z]/, character =>
    character.toLowerCase(),
  );
}

function selectTemplate(name) {
  return OVERLAY_TEMPLATES.has(name) ? name : "blank";
}

function templateVariables(options, name) {
  const camelAlias = toCamelAlias(name);
  return {
    ...strings,
    ...options,
    name,
    className: strings.classify(name),
    camelAlias,
    routesExport: `${camelAlias}Routes`,
    pagesRoutesExport: `${camelAlias}PagesRoutes`,
  };
}

function templateSource(directory, options, name, targetPath, shouldInclude) {
  const transforms = [
    applyTemplates(templateVariables(options, name)),
    move(targetPath),
  ];
  if (shouldInclude) {
    transforms.push(forEach(fileEntry => (shouldInclude(fileEntry.path) ? fileEntry : null)));
  }
  return apply(url(`../../files/${directory}`), transforms);
}

module.exports = {
  OVERLAY_TEMPLATES,
  packageVersion,
  selectTemplate,
  templateSource,
  toCamelAlias,
};
