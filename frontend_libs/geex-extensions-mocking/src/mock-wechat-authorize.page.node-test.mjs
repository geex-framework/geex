import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const pageSource = fs.readFileSync(path.join(__dirname, "pages/mock-wechat-authorize.page.ts"), "utf8");

describe("mock wechat authorize page zoneless rendering", () => {
  it("stores async UI state in signals", () => {
    assert.match(pageSource, /profiles\s*=\s*signal</);
    assert.match(pageSource, /error\s*=\s*signal\(/);
    assert.match(pageSource, /done\s*=\s*signal\(/);
    assert.match(pageSource, /loading\s*=\s*signal\(/);
  });

  it("binds template to signal reads so zoneless CD refreshes", () => {
    assert.match(pageSource, /profiles\(\)/);
    assert.match(pageSource, /error\(\)/);
    assert.match(pageSource, /done\(\)/);
    assert.match(pageSource, /loading\(\)/);
  });

  it("does not mutate plain fields inside subscribe for profiles/error/done", () => {
    assert.doesNotMatch(pageSource, /\.subscribe\s*\(/);
    assert.doesNotMatch(pageSource, /this\.profiles\s*=/);
    assert.doesNotMatch(pageSource, /this\.error\s*=/);
    assert.doesNotMatch(pageSource, /this\.done\s*=/);
  });

  it("shows loading and empty-profile guidance", () => {
    assert.match(pageSource, /Loading profiles/);
    assert.match(pageSource, /No enabled mock WeChat profiles/);
  });
});
