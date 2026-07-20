import assert from "node:assert/strict";
import { describe, it } from "node:test";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("GeexStartupService", () => {
  it("initializes debugger blocker from startup options", () => {
    const source = fs.readFileSync(path.join(__dirname, "geex-startup.service.ts"), "utf8");
    assert.match(source, /this\.debuggerBlocker\.init\(\)/);
    assert.match(source, /this\.options\.oauth\.getConfig\(\)/);
    const types = fs.readFileSync(path.join(__dirname, "types.ts"), "utf8");
    assert.match(types, /blockDebugger\?/);
    assert.doesNotMatch(types, /onDebuggerInit/);
  });

  it("bootstraps OIDC once in a linear pipeline", () => {
    const source = fs.readFileSync(path.join(__dirname, "geex-startup.service.ts"), "utf8");
    assert.match(source, /private bootstrapped/);
    assert.match(source, /tryOidcCodeCallback/);
    assert.match(source, /await this\.tryOidcCodeCallback\(\);\s*[\s\S]*this\.ensureSessionWatch\(\);\s*[\s\S]*await this\.geex\.init\(\);\s*[\s\S]*await this\.bindUiSession\(\)/);
    assert.match(source, /WechatWeb/);
    assert.doesNotMatch(source, /geex\.init\(true\)/);
  });

  it("resolves auth user without EmptyError into bootstrap", () => {
    const source = fs.readFileSync(path.join(__dirname, "geex-startup.service.ts"), "utf8");
    assert.match(source, /resolveAuthUser/);
    assert.match(source, /readAuthUser/);
    assert.match(source, /defaultValue:\s*undefined/);
    assert.match(source, /federateAuthenticate did not produce a user|geex\.authentication\.user\(\) missing after federateAuthenticate/);
  });

  it("loads discovery before tryLogin on OIDC callback", () => {
    const source = fs.readFileSync(path.join(__dirname, "geex-startup.service.ts"), "utf8");
    assert.match(
      source,
      /tryOidcCodeCallback[\s\S]*loadDiscoveryDocument\(\);[\s\S]*ensureOAuthTokenEndpoint\(\);[\s\S]*tryLogin\(\)/,
    );
  });

  it("reads menus and well-known setting names", () => {
    const source = fs.readFileSync(path.join(__dirname, "geex-startup.service.ts"), "utf8");
    assert.match(source, /GEEX_DEFAULT_MENUS/);
    assert.match(source, /GEEX_APP_NAME_SETTING/);
    assert.match(source, /GEEX_APP_MENU_SETTING/);
    assert.match(source, /GEEX_LOCALIZATION_DATA_SETTING/);
    assert.match(source, /GEEX_LOCALIZATION_LANGUAGE_SETTING/);
    assert.doesNotMatch(source, /GEEX_SETTINGS_UI_BINDINGS/);
    assert.doesNotMatch(source, /GEEX_I18N_SETTINGS_BINDINGS/);
    assert.doesNotMatch(source, /options\.defaultMenus/);
    assert.doesNotMatch(source, /options\.settingKeys/);
  });
});
