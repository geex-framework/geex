import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexBlobStorage", () => {
  it("exposes options and token overrides as environment providers", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-blob-storage.ts"), "utf8");
    assert.match(source, /GeexBlobStorageOptions/);
    assert.match(source, /GEEX_BLOB_STORAGE_OPTIONS/);
    assert.match(source, /EnvironmentProviders/);
    assert.match(source, /makeEnvironmentProviders/);
  });
});
