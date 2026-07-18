import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexAuthenticationWechat", () => {
  it("exposes the standard provider and compatibility alias", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-wechat-auth.ts"), "utf8");
    assert.match(source, /provideGeexAuthenticationWechat/);
    assert.match(source, /GEEX_AUTHENTICATION_WECHAT_OPTIONS/);
    assert.match(source, /EnvironmentProviders/);
    assert.match(source, /makeEnvironmentProviders/);
    assert.match(source, /provideWechatAuth = provideGeexAuthenticationWechat/);
  });
});
