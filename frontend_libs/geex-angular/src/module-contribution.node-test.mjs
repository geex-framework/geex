import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("Geex module contributions", () => {
  it("composes multi-provider modules before overrides and init", () => {
    const contribution = fs.readFileSync(path.join(__dirname, "module-contribution.ts"), "utf8");
    const geex = fs.readFileSync(path.join(__dirname, "geex.ts"), "utf8");
    assert.match(contribution, /GEEX_MODULE_CONTRIBUTIONS/);
    assert.match(contribution, /multi:\s*true/);
    assert.match(contribution, /makeEnvironmentProviders/);
    assert.match(geex, /contribution\.createModules/);
    assert.match(geex, /Object\.assign\(modules, overrides\)/);
    assert.match(geex, /Geex module.*already registered/);
  });
});
