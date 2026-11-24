function ensureFavoritesDependency(tree, name) {
  if (!["favorites", "identity", "approval-flows"].includes(name) || !tree.exists("package.json")) return;
  const manifest = JSON.parse(tree.read("package.json").toString("utf8"));
  manifest.dependencies ??= {};
  manifest.dependencies["@geexcode/geex-extensions-favorites"] ??= "^0.0.1";
  tree.overwrite("package.json", `${JSON.stringify(manifest, null, 2)}\n`);
}
exports.ensureFavoritesDependency = ensureFavoritesDependency;
