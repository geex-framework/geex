import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexMocking", () => {
  it("uses the standard options token and environment providers", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-mocking.ts"), "utf8");
    assert.match(source, /GEEX_MOCKING_OPTIONS/);
    assert.match(source, /EnvironmentProviders/);
    assert.match(source, /makeEnvironmentProviders/);
  });
});
