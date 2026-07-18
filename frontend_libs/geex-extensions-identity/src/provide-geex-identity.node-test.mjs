import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexIdentity", () => {
  it("registers identity modules and Apollo policies through core contributions", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-identity.ts"), "utf8");
    assert.match(source, /GEEX_IDENTITY_OPTIONS/);
    assert.match(source, /provideGeexModuleContribution/);
    assert.match(source, /return \{ tenant, auth, identity \}/);
    assert.match(source, /provideGeexApolloTypePolicies/);
    assert.match(source, /User:\s*\{/);
    assert.match(source, /Org:\s*\{/);
    assert.match(source, /EnvironmentProviders/);
    assert.match(source, /makeEnvironmentProviders/);
  });

  it("uses core-owned GeexModule contracts", () => {
    const moduleContract = fs.readFileSync(path.join(__dirname, "geex-module.ts"), "utf8");
    assert.match(moduleContract, /from "@geexcode\/geex-angular"/);
    assert.doesNotMatch(moduleContract, /type GeexModule.*=\s*\{/s);
  });
});
