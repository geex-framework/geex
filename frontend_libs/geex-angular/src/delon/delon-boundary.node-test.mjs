import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const srcRoot = path.resolve(__dirname, "..");

describe("delon core boundary", () => {
  it("exports Delon base surface from public-api", () => {
    const api = fs.readFileSync(path.join(srcRoot, "public-api.ts"), "utf8");
    assert.match(api, /from "\.\/delon"/);
  });

  it("declares Delon peers in package.json", () => {
    const pkg = JSON.parse(fs.readFileSync(path.join(srcRoot, "..", "package.json"), "utf8"));
    assert.ok(pkg.peerDependencies["@delon/abc"]);
    assert.ok(pkg.peerDependencies["@delon/theme"]);
    assert.ok(pkg.peerDependencies["ng-zorro-antd"]);
  });

  it("provides GeexReuseTabStrategy and GeexRouter sources", () => {
    assert.equal(fs.existsSync(path.join(__dirname, "router/geex-router.ts")), true);
    assert.equal(fs.existsSync(path.join(__dirname, "router/geex-reuse-tab.strategy.ts")), true);
    assert.equal(fs.existsSync(path.join(__dirname, "base/routed.component.base.ts")), true);
    assert.equal(fs.existsSync(path.join(__dirname, "layout/list-page-layout.component.ts")), true);
  });

  it("exports Modal and TreeTable bases from delon index", () => {
    const index = fs.readFileSync(path.join(__dirname, "index.ts"), "utf8");
    assert.match(index, /modal\.component\.base/);
    assert.match(index, /tree-table\.component\.base/);
  });
});

describe("delon override hooks", () => {
  it("RoutedComponent exposes reload / form / decode hooks", () => {
    const src = fs.readFileSync(path.join(__dirname, "base/routed.component.base.ts"), "utf8");
    assert.match(src, /handleRouteReload/);
    assert.match(src, /decodeQueryParam/);
    assert.match(src, /buildParamsForm/);
    assert.match(src, /beforeOnRouted/);
    assert.match(src, /afterOnRouted/);
    assert.match(src, /protected isEqualToDefault/);
  });

  it("RoutedListComponent exposes table / batch hooks", () => {
    const src = fs.readFileSync(path.join(__dirname, "base/routed-list.component.base.ts"), "utf8");
    assert.match(src, /onTableCheckbox/);
    assert.match(src, /onTablePage/);
    assert.match(src, /onTableSort/);
    assert.match(src, /filterBatchIds/);
    assert.match(src, /buildBatchMutation/);
    assert.match(src, /confirmBatch/);
  });

  it("RoutedEditComponent exposes dirty / confirm hooks", () => {
    const src = fs.readFileSync(path.join(__dirname, "base/routed-edit.component.base.ts"), "utf8");
    assert.match(src, /isEntityDirty/);
    assert.match(src, /unsavedConfirmTitle/);
  });

  it("ModalComponentBase exposes afterClose", () => {
    const src = fs.readFileSync(path.join(__dirname, "base/modal.component.base.ts"), "utf8");
    assert.match(src, /afterClose/);
    assert.match(src, /inject\(NzModalRef\)/);
  });

  it("TreeTableComponentBase exposes node accessors", () => {
    const src = fs.readFileSync(path.join(__dirname, "base/tree-table.component.base.ts"), "utf8");
    assert.match(src, /getNodeKey/);
    assert.match(src, /getNodeChildren/);
    assert.match(src, /GEEX_I18N/);
  });
});
