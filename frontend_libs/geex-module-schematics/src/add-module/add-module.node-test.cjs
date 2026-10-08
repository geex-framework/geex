const { describe, it } = require("node:test");
const assert = require("node:assert/strict");
const path = require("node:path");
const { HostTree } = require("@angular-devkit/schematics");
const { SchematicTestRunner } = require("@angular-devkit/schematics/testing");
const { toCamelAlias } = require("./index.js");
const { version } = require("../../package.json");
const vm = require("node:vm");

const collectionPath = path.join(__dirname, "../collection.json");

describe("generated attachment upload flows", () => {
  async function createUpload(kind, outcome, options = {}) {
    const runner = new SchematicTestRunner("geex-module-schematics", collectionPath);
    const tree = await runner.runSchematic("add-module", { name: "blob-storage", path: "src/app/modules" }, createAppTree());
    const widget = kind === "widget";
    const source = tree.read(`src/app/modules/blob-storage/${widget ? "widgets/upload/geex-upload.widget.ts" :
      "components/upload/geex-upload.component.ts"}`).toString("utf8");
    assert.match(source, /import \{ attachBlob \} from "@geexcode\/geex-extensions-blob-storage"/);
    const callback = widget
      ? source.slice(source.indexOf("  ngOnInit():"), source.indexOf("  private defaultStorageType"))
        .replace("ngOnInit(): void", "ngOnInit()")
        .replace(/ as GeexUploadWidgetSchema/g, "").replace(/: GeexUploadWidgetSchema/g, "")
        .replace("args: NzUploadXHRArgs", "args")
      : source.slice(source.indexOf("  uploadGeexBlobObject ="), source.indexOf("  private async fetchFilesByIds"))
        .replace("(args: NzUploadXHRArgs): Subscription", "(args)");
    const calls = [], subscription = {}, empty = {};
    const storage = { defaultStorageType: "Db" };
    const context = {
      Blob, Subscription: { EMPTY: empty }, geex: { blobStorage: storage }, GEEX_I18N: {},
      toBool: (value, fallback) => value == null ? fallback : !!value,
      attachBlob: (...args) => {
        calls.push(args);
        return { subscribe: observer => {
          if (outcome instanceof Error) observer.error(outcome);
          else observer.next(outcome);
          return subscription;
        } };
      },
    };
    vm.runInNewContext(`class Upload { ${callback} }\nglobalThis.Upload = Upload;`, context);
    const instance = new context.Upload();
    let upload;
    if (widget) {
      instance.ui = options;
      instance.injector = { get: () => ({ BlobStorage: { uploadClick: "Upload" } }) };
      instance.ngOnInit();
      upload = instance.ui.customRequest;
    } else {
      instance.blobStorage = storage;
      instance.storageType = options.storageType;
      upload = instance.uploadGeexBlobObject;
    }
    return { upload, calls, subscription, empty, storage };
  }

  for (const kind of ["widget", "component"]) {
    it(`${kind} forwards transformed content and completes with the selected Blob`, async () => {
      const selected = { id: "existing", url: "/files?fileId=existing" };
      const probe = await createUpload(kind, selected, { storageType: "FileSystem" });
      const original = { name: "original.png" }, transformed = new Blob(["transformed"], { type: "image/png" });
      const progress = [], completions = [];
      const returned = probe.upload({ file: original, postFile: transformed,
        onProgress: value => progress.push(value.percent), onSuccess: (...args) => completions.push(args) });
      assert.equal(returned, probe.subscription);
      assert.deepEqual(probe.calls, [[probe.storage, transformed, "original.png", "FileSystem"]]);
      assert.deepEqual(completions, [[selected, original, null]]);
      assert.deepEqual(progress, [0, 100]);
    });

    it(`${kind} uses module storage defaults and forwards lookup or upload errors`, async () => {
      const failure = new Error("lookup failed");
      const probe = await createUpload(kind, failure);
      const file = { name: "file.txt" }, failures = [];
      probe.upload({ file, postFile: new Blob(["file"]), onError: (...args) => failures.push(args),
        onSuccess: () => assert.fail("failed upload completed") });
      assert.equal(probe.calls[0][3], "Db");
      assert.deepEqual(failures, [[failure, file]]);
    });

    it(`${kind} does not invoke attachment logic for unsupported postFile values`, async () => {
      const probe = await createUpload(kind, {});
      assert.equal(probe.upload({ file: { name: "file.txt" }, postFile: "data:" }), probe.empty);
      assert.equal(probe.calls.length, 0);
    });
  }

  it("preserves an application-supplied widget customRequest", async () => {
    const customRequest = () => {};
    const probe = await createUpload("widget", {}, { customRequest });
    assert.equal(probe.upload, customRequest);
    assert.equal(probe.calls.length, 0);
  });
});

describe("generated upload component removal", () => {
  for (const decision of [undefined, false, true]) {
    it(`only deletes the resource for explicit true (decision: ${decision})`, async () => {
      const runner = new SchematicTestRunner("geex-module-schematics", collectionPath);
      const tree = await runner.runSchematic("add-module", { name: "blob-storage", path: "src/app/modules" }, createAppTree());
      const source = tree.read("src/app/modules/blob-storage/components/upload/geex-upload.component.ts").toString("utf8");
      const callback = source.slice(source.indexOf("  handleRemove ="), source.indexOf("  handlePreview ="))
        .replace("(file: NzUploadFile): boolean | Observable<boolean>", "(file)");
      const deletions = [];
      const storage = { defaultStorageType: "Db", delete: async request => deletions.push(request) };
      const context = { geex: { blobStorage: storage }, switchMap: project => project };
      vm.runInNewContext(`class Upload { ${callback} }\nglobalThis.Upload = Upload;`, context);
      const instance = new context.Upload();
      instance.blobStorage = storage;
      instance.fileList = [{ uid: "shared" }, { uid: "other" }];
      instance.pureValue = files => files.map(file => file.uid);
      instance.onChange = value => { instance.value = value; };
      instance.deleteRemoteFile = decision === undefined ? undefined : { pipe: project => project(decision) };
      assert.equal(await instance.handleRemove({ uid: "shared" }), true);
      assert.deepEqual(Array.from(instance.value), ["other"]);
      assert.equal(deletions.length, decision === true ? 1 : 0);
    });
  }
});

describe("generated upload widget removal", () => {
  async function removeWith(deleteDecision) {
    const runner = new SchematicTestRunner("geex-module-schematics", collectionPath);
    const tree = await runner.runSchematic("add-module", { name: "blob-storage", path: "src/app/modules" }, createAppTree());
    const source = tree.read("src/app/modules/blob-storage/widgets/upload/geex-upload.widget.ts").toString("utf8");
    const callback = source.slice(source.indexOf("  handleRemove ="), source.indexOf("  handlePreview ="))
      .replace(/\(file:\s*NzUploadFile\):\s*Observable<boolean>/, "(file)");
    assert.match(callback, /handleRemove/);
    const code = `class Widget { ${callback} }\nglobalThis.Widget = Widget;`;
    const deletions = [];
    const context = { from: promise => promise, geex: { blobStorage: { delete: async value => deletions.push(value) } } };
    vm.runInNewContext(code, context);
    const instance = new context.Widget();
    instance.fileList = [{ uid: "shared" }, { uid: "other" }];
    instance.ui = { storageType: "Db", deleteRemoteFile: deleteDecision === undefined ? undefined :
      { firstValuePromise: async () => deleteDecision } };
    instance._setValue = files => { instance.value = files.map(file => file.uid); };
    assert.equal(await instance.handleRemove({ uid: "shared" }), true);
    assert.deepEqual(Array.from(instance.value), ["other"]);
    return JSON.parse(JSON.stringify(deletions));
  }

  it("ordinary removal only unbinds the current field", async () => {
    assert.deepEqual(await removeWith(undefined), []);
  });

  it("false or empty deletion decisions preserve the shared resource", async () => {
    assert.deepEqual(await removeWith(false), []);
    assert.deepEqual(await removeWith(null), []);
  });

  it("an explicit true decision still invokes resource deletion", async () => {
    assert.deepEqual(await removeWith(true), [{ request: { ids: ["shared"], storageType: "Db" } }]);
  });
});

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
    "backups": ["backups.routes.ts", "backups.menu.ts", "pages/list.page.ts", "pages/list.page.html", "i18n/zh-CN.ts"],
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
