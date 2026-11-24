const { packageVersion } = require("./template-utils");

function ensureFavoritesDependency(tree, name) {
  if (!["favorites", "identity", "approval-flows"].includes(name) || !tree.exists("package.json")) return;
  const manifest = JSON.parse(tree.read("package.json").toString("utf8"));
  const dependency = "@geexcode/geex-extensions-favorites";
  if (["dependencies", "devDependencies", "optionalDependencies", "peerDependencies"].some(section =>
    Object.hasOwn(manifest[section] ?? {}, dependency))) return;
  manifest.dependencies ??= {};
  manifest.dependencies[dependency] = packageVersion;
  tree.overwrite("package.json", `${JSON.stringify(manifest, null, 2)}\n`);
}
exports.ensureFavoritesDependency = ensureFavoritesDependency;
