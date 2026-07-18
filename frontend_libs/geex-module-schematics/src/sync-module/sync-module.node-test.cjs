const { describe, it } = require("node:test");
const assert = require("node:assert/strict");
const path = require("node:path");
const { HostTree } = require("@angular-devkit/schematics");
const { SchematicTestRunner } = require("@angular-devkit/schematics/testing");

const collectionPath = path.join(__dirname, "../collection.json");

async function addModule(name) {
  const runner = new SchematicTestRunner("geex-module-schematics", collectionPath);
  const tree = new HostTree();
  return {
    runner,
    tree: await runner.runSchematic("add-module", { name, path: "src/app/modules" }, tree),
  };
}

describe("sync-module schematic", () => {
  it("requires an installed module and its manifest", async () => {
    const runner = new SchematicTestRunner("geex-module-schematics", collectionPath);
    const empty = new HostTree();
    await assert.rejects(
      () => runner.runSchematic("sync-module", { name: "missing", path: "src/app/modules" }, empty),
      /not found/,
    );

    const appTree = new HostTree();
    appTree.create("src/app/modules/demo-mod/index.ts", "export const keep = true;\n");
    await assert.rejects(
      () => runner.runSchematic("sync-module", { name: "demo-mod", path: "src/app/modules" }, appTree),
      /manifest not found/,
    );
  });

  for (const [name, ownedFile] of Object.entries({
    identity: "widgets/org-tree/index.ts",
    "blob-storage": "graphql/operations.gql",
    "approval-flows": "pages/edit/edit.page.html",
    mocking: "pages/mocking-home.page.ts",
  })) {
    it(`restores missing ${name} overlay files and preserves modified files by default`, async () => {
      const { runner, tree: installed } = await addModule(name);
      const root = `src/app/modules/${name}`;
      const modifiedPath = `${root}/index.ts`;
      const missingPath = `${root}/${ownedFile}`;
      installed.overwrite(modifiedPath, "export const userModified = true;\n");
      installed.delete(missingPath);

      const synced = await runner.runSchematic("sync-module", { name, path: "src/app/modules" }, installed);
      assert.equal(synced.exists(missingPath), true);
      assert.equal(synced.read(modifiedPath).toString("utf8"), "export const userModified = true;\n");
    });
  }

  it("force overwrites template-owned files while preserving unrelated files and installedAt", async () => {
    const { runner, tree: installed } = await addModule("blob-storage");
    const root = "src/app/modules/blob-storage";
    const ownedPath = `${root}/pages/list/list.page.ts`;
    const unrelatedPath = `${root}/user-notes.ts`;
    installed.overwrite(ownedPath, "export const modified = true;\n");
    installed.create(unrelatedPath, "export const keep = true;\n");
    const beforeManifest = JSON.parse(installed.read(".geex/modules.json").toString("utf8"));
    const installedAt = beforeManifest.modules["blob-storage"].installedAt;

    const synced = await runner.runSchematic(
      "sync-module",
      { name: "blob-storage", path: "src/app/modules", force: true },
      installed,
    );
    assert.match(synced.read(ownedPath).toString("utf8"), /class BlobStorageListPage/);
    assert.equal(synced.read(unrelatedPath).toString("utf8"), "export const keep = true;\n");
    const afterManifest = JSON.parse(synced.read(".geex/modules.json").toString("utf8"));
    assert.equal(afterManifest.modules["blob-storage"].installedAt, installedAt);
  });

  it("uses the template recorded in the manifest", async () => {
    const { runner, tree: installed } = await addModule("custom-module");
    const manifest = JSON.parse(installed.read(".geex/modules.json").toString("utf8"));
    manifest.modules["custom-module"].template = "approval-flows";
    installed.overwrite(".geex/modules.json", `${JSON.stringify(manifest, null, 2)}\n`);

    const synced = await runner.runSchematic(
      "sync-module",
      { name: "custom-module", path: "src/app/modules", force: true },
      installed,
    );
    assert.equal(synced.exists("src/app/modules/custom-module/widgets/approve/approve-button.component.ts"), true);
  });
});
