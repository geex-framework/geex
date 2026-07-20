import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexMultiTenant", () => {
  it("registers the multiTenant module through core module contributions", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-multi-tenant.ts"), "utf8");
    assert.match(source, /GEEX_MULTI_TENANT_OPTIONS/);
    assert.match(source, /provideGeexModuleContribution/);
    assert.match(source, /\(\{\s*multiTenant:/);
    assert.match(source, /EnvironmentProviders/);
    assert.match(source, /makeEnvironmentProviders/);
  });

  it("uses core-owned GeexModule contracts", () => {
    const types = fs.readFileSync(path.join(__dirname, "multi-tenant.types.ts"), "utf8");
    assert.match(types, /from "@geexcode\/geex-angular"/);
    assert.match(types, /declare module "@geexcode\/geex-angular"/);
  });
});
