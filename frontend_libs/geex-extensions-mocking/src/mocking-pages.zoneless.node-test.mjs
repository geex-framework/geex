import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const pagesDir = path.join(__dirname, "pages");

const asyncUiPages = [
  "mocking-wechat-profiles.page.ts",
  "mocking-sms.page.ts",
  "mock-wechat-authorize.page.ts",
  "mock-payment-checkout.page.ts",
];

describe("mocking pages zoneless state", () => {
  for (const fileName of asyncUiPages) {
    it(`${fileName} uses signals for async list/error state`, () => {
      const source = fs.readFileSync(path.join(pagesDir, fileName), "utf8");
      assert.match(source, /signal\(/);
      assert.doesNotMatch(source, /\.subscribe\s*\(/);
      assert.match(source, /firstValueFrom/);
    });
  }

  it("wechat profiles page binds list via profiles()", () => {
    const source = fs.readFileSync(path.join(pagesDir, "mocking-wechat-profiles.page.ts"), "utf8");
    assert.match(source, /profiles\s*=\s*signal/);
    assert.match(source, /profiles\(\)/);
    assert.match(source, /@for\s*\(\s*p of profiles\(\)/);
  });

  it("sms page binds list via messages()", () => {
    const source = fs.readFileSync(path.join(pagesDir, "mocking-sms.page.ts"), "utf8");
    assert.match(source, /messages\s*=\s*signal/);
    assert.match(source, /messages\(\)/);
    assert.match(source, /@for\s*\(\s*m of messages\(\)/);
  });
});
