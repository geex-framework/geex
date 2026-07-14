import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("geex-angular menu contribution contract", () => {
  it("exports GEEX_MENU_CONTRIBUTIONS token", () => {
    const source = fs.readFileSync(path.join(__dirname, "menu-contribution.ts"), "utf8");
    assert.match(source, /export const GEEX_MENU_CONTRIBUTIONS/);
    assert.match(source, /GeexMenuContribution/);
    assert.match(source, /resolve\(/);
  });

  it("re-exports menu contribution from package index", () => {
    const source = fs.readFileSync(path.join(__dirname, "index.ts"), "utf8");
    assert.match(source, /menu-contribution/);
  });
});
