const { describe, it } = require("node:test");
const assert = require("node:assert/strict");
const path = require("node:path");
const { HostTree } = require("@angular-devkit/schematics");
const { SchematicTestRunner } = require("@angular-devkit/schematics/testing");
const { toCamelAlias } = require("./index.js");
const { version } = require("../../package.json");

const collectionPath = path.join(__dirname, "../collection.json");

function createAppTree() {
  const tree = new HostTree();
  tree.create(
    "src/app/modules/module-registry.ts",
    `import * as exception from "./exception";
export const installedModules = {
  auth,
  exception,
} as const;
export const authenticatedModuleChildren: Routes = [
];
export const defaultMenus: Menu[] = [
  {
    children: [...identity.menuContribution, ...settings.menuContribution, ...tenant.menuContribution],
  },
];
export const moduleI18nZhCN = {
};
export const moduleI18nEnUS = {
};
`,
  );
  return tree;
}

describe("add-module schematic", () => {
  it("computes camel aliases for module names", () => {
    assert.equal(toCamelAlias("settings"), "settings");
    assert.equal(toCamelAlias("geex-mod-geex"), "geexModGeex");
  });

  it("adds the module import when the registry has no exception import", async () => {
    const runner = new SchematicTestRunner("geex-module-schematics", collectionPath);
    const appTree = createAppTree();
    const registryPath = "src/app/modules/module-registry.ts";
    appTree.overwrite(
      registryPath,
      appTree.read(registryPath).toString("utf8").replace('import * as exception from "./exception";\n', ""),
    );

    const tree = await runner.runSchematic("add-module", { name: "blob-storage", path: "src/app/modules" }, appTree);
    const registry = tree.read(registryPath).toString("utf8");
    assert.match(registry, /import \* as blobStorage from "\.\/blob-storage";/);
    assert.match(registry, /BlobStorage: blobStorage\.i18n\["zh-CN"\]/);
    assert.match(registry, /BlobStorage: blobStorage\.i18n\["en-US"\]/);
  });

  it("installs blank fallback and preserves unrelated manifest data", async () => {
    const runner = new SchematicTestRunner("geex-module-schematics", collectionPath);
    const appTree = createAppTree();
    appTree.create(
      ".geex/modules.json",
      JSON.stringify({ custom: { keep: true }, modules: { existing: { template: "identity" } } }),
    );

    const tree = await runner.runSchematic("add-module", { name: "demo-mod", path: "src/app/modules" }, appTree);
    assert.equal(tree.exists("src/app/modules/demo-mod/index.ts"), true);
    assert.equal(tree.exists("src/app/modules/demo-mod/demo-mod.routes.ts"), true);
    assert.equal(tree.exists(".geex/modules.json"), true);
    assert.equal(tree.exists("src/app/modules/demo-mod/widgets"), false);
    const manifest = JSON.parse(tree.read(".geex/modules.json").toString("utf8"));
    assert.deepEqual(manifest.custom, { keep: true });
    assert.deepEqual(manifest.modules.existing, { template: "identity" });
    assert.equal(manifest.modules["demo-mod"].template, "blank");
    assert.equal(manifest.modules["demo-mod"].templateVersion, version);

    const registry = tree.read("src/app/modules/module-registry.ts").toString("utf8");
    assert.match(registry, /import \* as demoMod from "\.\/demo-mod"/);
    assert.match(registry, /path: "demo-mod"/);

    await assert.rejects(
      () => runner.runSchematic("add-module", { name: "demo-mod", path: "src/app/modules" }, tree),
      /already exists/,
    );
  });

  for (const [name, expectedFiles] of Object.entries({
    identity: [
      "identity.routes.ts",
      "widgets/org-tree/org-tree-select.widget.ts",
      "pages/user/list.page.ts",
      "pages/user/user.routes.ts",
      "graphql/user.operations.gql.ts",
      "components/modals/add-user-modal.component.ts",
    ],
    "blob-storage": [
      "blob-storage.routes.ts",
      "graphql/operations.gql.ts",
      "pages/edit/edit.page.ts",
      "components/upload/geex-upload.component.ts",
      "widgets/upload/geex-upload.widget.ts",
    ],
    "approval-flows": [
      "approval-flows.routes.ts",
      "graphql/operations.gql.ts",
      "pages/edit/edit.page.ts",
      "components/approve/approve-button.component.ts",
      "widgets/approve/common-options.ts",
    ],
    mocking: [
      "mocking.routes.ts",
      "mocking.menu.ts",
      "pages/mocking-home.page.ts",
      "pages/mock-wechat-authorize.page.ts",
    ],
    authentication: ["authentication.routes.ts", "pages/login.page.ts"],
    settings: ["settings.routes.ts", "pages/setting-list.page.ts", "graphql/operations.gql.ts"],
    "multi-tenant": ["multi-tenant.routes.ts", "pages/tenant-list.page.ts", "components/tenant-switcher/tenant-switcher.component.ts"],
  })) {
    it(`merges the ${name} overlay without duplicate path segments`, async () => {
      const runner = new SchematicTestRunner("geex-module-schematics", collectionPath);
      const tree = await runner.runSchematic("add-module", { name, path: "custom/modules" }, createAppTree());
      for (const relativePath of expectedFiles) {
        assert.equal(tree.exists(`custom/modules/${name}/${relativePath}`), true, relativePath);
      }
      assert.equal(tree.files.some(file => /widgets\/([^/]+)\/\1\//.test(file)), false);
      const manifest = JSON.parse(tree.read(".geex/modules.json").toString("utf8"));
      assert.equal(manifest.modules[name].template, name);
      assert.equal(manifest.modules[name].path, `custom/modules/${name}`);
      assert.equal(manifest.modules[name].templateVersion, version);
      assert.ok(Date.parse(manifest.modules[name].installedAt));
      const registry = tree.read("src/app/modules/module-registry.ts").toString("utf8");
      assert.match(registry, new RegExp(`\\s${toCamelAlias(name)},`));
    });
  }
});

