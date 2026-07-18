const fs = require("fs");
const path = require("path");

const root = path.join(__dirname, "..");
const dist = path.join(root, "dist");

const srcPlugin = path.join(root, "src/esbuild/gql-redirect.plugin.js");
const destDir = path.join(dist, "esbuild");
const destPlugin = path.join(destDir, "gql-redirect.plugin.js");

fs.mkdirSync(destDir, { recursive: true });
if (fs.existsSync(srcPlugin)) {
  fs.copyFileSync(srcPlugin, destPlugin);
}

const risonSrc = path.join(root, "src/rison/rison.js");
const risonDestDir = path.join(dist, "rison");
if (fs.existsSync(risonSrc)) {
  fs.mkdirSync(risonDestDir, { recursive: true });
  fs.copyFileSync(risonSrc, path.join(risonDestDir, "rison.js"));
}

// Angular application builder resolves JS next to typings (`dist/index.d.ts` → `dist/index`).
const fesm = "./fesm2022/geexcode-geex-angular.mjs";
const indexJs = `export * from "${fesm}";\n`;
fs.writeFileSync(path.join(dist, "index.js"), indexJs);
fs.writeFileSync(path.join(dist, "index.mjs"), indexJs);

console.log("copied esbuild plugin / rison assets / dist index re-exports");
