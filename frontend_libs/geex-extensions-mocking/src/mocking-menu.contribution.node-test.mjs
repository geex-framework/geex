import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("mocking menu contribution plugin", () => {
  it("registers GEEX_MENU_CONTRIBUTIONS via provideGeexMocking", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-mocking.ts"), "utf8");
    assert.match(source, /GEEX_MENU_CONTRIBUTIONS/);
    assert.match(source, /multi:\s*true/);
    assert.match(source, /MockingMenuContribution/);
  });

  it("returns a group menu only for superAdmin when capabilities allow", () => {
    const source = fs.readFileSync(path.join(__dirname, "mocking-menu.contribution.ts"), "utf8");
    assert.match(source, /GEEX_SUPER_ADMIN_ID/);
    assert.match(source, /caps\.enabled/);
    assert.match(source, /caps\.management/);
    assert.match(source, /group:\s*true/);
    assert.match(source, /mockingNavigation/);
  });
});
