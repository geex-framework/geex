import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexCommon", () => {
  it("exports a headless meta-provide without shipping admin UI", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-common.ts"), "utf8");
    assert.match(source, /export function provideGeexCommon/);
    assert.match(source, /return provideGeex\(/);
    assert.match(source, /Does not install admin business UI/);
  });

  it("re-exports provideGeexCommon from package entry", () => {
    const api = fs.readFileSync(path.join(__dirname, "public-api.ts"), "utf8");
    assert.match(api, /provide-geex-common/);
  });
});
