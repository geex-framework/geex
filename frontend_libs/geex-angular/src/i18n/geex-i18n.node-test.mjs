import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("geex-i18n cycle guard", () => {
  it("translate loader does not inject GEEX_I18N", () => {
    const source = fs.readFileSync(path.join(__dirname, "geex-translate-loader.ts"), "utf8");
    assert.doesNotMatch(source, /inject\(GEEX_I18N[,)]/);
    assert.match(source, /GEEX_I18N_PACKS/);
  });

  it("GEEX_I18N provider does not depend on GeexI18nService", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-i18n.ts"), "utf8");
    assert.match(source, /createGeexI18nDictionaryProxy/);
    assert.doesNotMatch(source, /deps:\s*\[GeexI18nService\]/);
  });
});
