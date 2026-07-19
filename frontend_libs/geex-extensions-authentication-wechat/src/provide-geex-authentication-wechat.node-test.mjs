import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexAuthenticationWechat", () => {
  it("registers the wechatAuth module through core module contributions", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-authentication-wechat.ts"), "utf8");
    assert.match(source, /GEEX_AUTHENTICATION_WECHAT_OPTIONS/);
    assert.match(source, /provideGeexModuleContribution/);
    assert.match(source, /\(\{\s*wechatAuth:/);
    assert.match(source, /EnvironmentProviders/);
    assert.match(source, /makeEnvironmentProviders/);
  });

  it("uses core-owned GeexModule contracts", () => {
    const types = fs.readFileSync(path.join(__dirname, "wechat-auth.types.ts"), "utf8");
    assert.match(types, /from "@geexcode\/geex-angular"/);
    assert.match(types, /declare module "@geexcode\/geex-angular"/);
    assert.match(types, /wechatAuth:/);
  });
});
