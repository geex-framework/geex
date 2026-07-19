import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexIdentity", () => {
  it("registers the identity module through core module contributions and consumes tenant/auth from the modules bag", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-identity.ts"), "utf8");
    assert.match(source, /GEEX_IDENTITY_OPTIONS/);
    assert.match(source, /provideGeexModuleContribution/);
    assert.match(source, /modules\["tenant"\]/);
    assert.match(source, /modules\["auth"\]/);
    assert.match(source, /provideGeexMultiTenant.*provideGeexAuthentication/);
    assert.match(source, /return \{ identity \}/);
    assert.match(source, /provideGeexApolloTypePolicies/);
    assert.match(source, /User:\s*\{/);
    assert.match(source, /Org:\s*\{/);
    assert.match(source, /EnvironmentProviders/);
    assert.match(source, /makeEnvironmentProviders/);
  });

  it("no longer owns tenant/auth module creation", () => {
    assert.equal(fs.existsSync(path.join(__dirname, "tenant.module.ts")), false);
    assert.equal(fs.existsSync(path.join(__dirname, "auth.module.ts")), false);
  });

  it("uses core-owned GeexModule contracts", () => {
    const moduleContract = fs.readFileSync(path.join(__dirname, "geex-module.ts"), "utf8");
    assert.match(moduleContract, /from "@geexcode\/geex-angular"/);
    assert.doesNotMatch(moduleContract, /type GeexModule.*=\s*\{/s);
  });

  it("re-exports Tenant types from geex-extensions-multi-tenant for backward compat", () => {
    const types = fs.readFileSync(path.join(__dirname, "types.ts"), "utf8");
    assert.match(types, /from "@geexcode\/geex-extensions-multi-tenant"/);
    assert.match(types, /GEEX_DEFAULT_SUPER_ADMIN_USER_ID/);
  });
});
