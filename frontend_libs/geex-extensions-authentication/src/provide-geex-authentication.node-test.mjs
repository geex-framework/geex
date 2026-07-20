import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexAuthentication", () => {
  it("registers the authentication module through core module contributions", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-authentication.ts"), "utf8");
    assert.match(source, /GEEX_AUTHENTICATION_OPTIONS/);
    assert.match(source, /provideGeexModuleContribution/);
    assert.match(source, /\(\{\s*authentication:/);
    assert.match(source, /EnvironmentProviders/);
    assert.match(source, /makeEnvironmentProviders/);
  });

  it("uses core-owned GeexModule contracts and depends on identity's User type", () => {
    const types = fs.readFileSync(path.join(__dirname, "authentication.types.ts"), "utf8");
    assert.match(types, /from "@geexcode\/geex-angular"/);
    assert.match(types, /from "@geexcode\/geex-extensions-identity"/);
    assert.match(types, /declare module "@geexcode\/geex-angular"/);
    assert.match(types, /reload\(\)/);
  });

  it("keeps auth init single-flight and exposes explicit reload", () => {
    const module = fs.readFileSync(path.join(__dirname, "authentication.module.ts"), "utf8");
    assert.match(module, /reload:\s*\(\)\s*=>/);
    assert.doesNotMatch(module, /_initializedToken/);
    assert.doesNotMatch(module, /tokenChanged/);
  });

  it("requires discovery, userinfo login_provider, and federateAuthenticate for session user", () => {
    const module = fs.readFileSync(path.join(__dirname, "authentication.module.ts"), "utf8");
    assert.match(module, /await oAuthService\.loadDiscoveryDocument\(\)/);
    assert.match(module, /await oAuthService\.loadUserProfile\(\)/);
    assert.match(module, /login_provider/);
    assert.match(module, /federateAuthenticate/);
    assert.doesNotMatch(module, /if\s*\(\s*!oAuthService\.userinfoEndpoint\s*\)/);
  });

  it("owns login path / oauth storage wiring", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-authentication.ts"), "utf8");
    assert.match(source, /loginPath/);
    assert.match(source, /GEEX_LOGIN_PATH/);
    assert.doesNotMatch(source, /afterLoginNavigate/);
    assert.doesNotMatch(source, /GEEX_AFTER_LOGIN_NAVIGATE/);
    assert.match(source, /OAuthModule\.forRoot/);
  });
});
