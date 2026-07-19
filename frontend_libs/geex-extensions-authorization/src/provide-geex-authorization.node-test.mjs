import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexAuthorization", () => {
  it("registers the authorization module and thin adapters", () => {
    const provide = fs.readFileSync(path.join(__dirname, "provide-geex-authorization.ts"), "utf8");
    assert.match(provide, /GEEX_AUTHORIZATION_OPTIONS/);
    assert.match(provide, /provideGeexModuleContribution/);
    assert.match(provide, /\(\{\s*authorization:/);
    assert.match(provide, /AuthorizeGuard/);
    assert.match(provide, /makeEnvironmentProviders/);

    const api = fs.readFileSync(path.join(__dirname, "public-api.ts"), "utf8");
    assert.match(api, /authorization\.types/);
    assert.match(api, /authorize\.guard/);
  });

  it("keeps business logic on the module, not the guard", () => {
    const module = fs.readFileSync(path.join(__dirname, "authorization.module.ts"), "utf8");
    assert.match(module, /geex\["tenant"\]/);
    assert.match(module, /GEEX_SUPER_ADMIN_USER_ID/);

    const guard = fs.readFileSync(path.join(__dirname, "authorize.guard.ts"), "utf8");
    assert.match(guard, /geex\.authorization\.canActivate/);
    assert.doesNotMatch(guard, /OAuthService/);
  });
});
