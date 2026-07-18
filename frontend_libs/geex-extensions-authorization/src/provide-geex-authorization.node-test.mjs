import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("geex-extensions-authorization", () => {
  it("exports AuthorizeGuard and provideGeexAuthorization", () => {
    const api = fs.readFileSync(path.join(__dirname, "public-api.ts"), "utf8");
    assert.match(api, /authorize\.guard/);
    assert.match(api, /provide-geex-authorization/);
    assert.match(api, /local-storage-acl\.service/);

    const provide = fs.readFileSync(path.join(__dirname, "provide-geex-authorization.ts"), "utf8");
    assert.match(provide, /export function provideGeexAuthorization/);
    assert.match(provide, /GEEX_AUTHORIZATION_OPTIONS/);
    assert.match(provide, /EnvironmentProviders/);
    assert.match(provide, /makeEnvironmentProviders/);
    assert.match(provide, /AuthorizeGuard/);
    assert.match(provide, /LocalStorageACLService/);
  });

  it("guard uses geex tenant and IdentityClaims", () => {
    const guard = fs.readFileSync(path.join(__dirname, "authorize.guard.ts"), "utf8");
    assert.match(guard, /from "@geexcode\/geex-angular"/);
    assert.match(guard, /IdentityClaims/);
    assert.match(guard, /GEEX_SUPER_ADMIN_USER_ID/);
    assert.doesNotMatch(guard, /claims\?\.sub === "000000000000000000000001"/);
    assert.match(guard, /geex\["tenant"\]/);
  });
});
