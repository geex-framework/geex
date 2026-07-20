import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexCaptcha", () => {
  it("registers captcha through core module contributions", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-captcha.ts"), "utf8");
    assert.match(source, /GEEX_CAPTCHA_OPTIONS/);
    assert.match(source, /provideGeexModuleContribution/);
    assert.match(source, /\(\{\s*captcha:/);
  });

  it("exports CaptchaProvider and augments GeexModuleMap", () => {
    const types = fs.readFileSync(path.join(__dirname, "captcha.types.ts"), "utf8");
    assert.match(types, /enum CaptchaProvider/);
    assert.match(types, /Sms = "Sms"/);
    assert.match(types, /Image = "Image"/);
    assert.match(types, /captcha: CaptchaModule/);
  });
});
