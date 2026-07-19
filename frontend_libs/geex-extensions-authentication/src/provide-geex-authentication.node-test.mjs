import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexAuthentication", () => {
  it("registers the auth module through core module contributions", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-authentication.ts"), "utf8");
    assert.match(source, /GEEX_AUTHENTICATION_OPTIONS/);
    assert.match(source, /provideGeexModuleContribution/);
    assert.match(source, /\(\{\s*auth:/);
    assert.match(source, /EnvironmentProviders/);
    assert.match(source, /makeEnvironmentProviders/);
  });

  it("uses core-owned GeexModule contracts and depends on identity's User type", () => {
    const types = fs.readFileSync(path.join(__dirname, "auth.types.ts"), "utf8");
    assert.match(types, /from "@geexcode\/geex-angular"/);
    assert.match(types, /from "@geexcode\/geex-extensions-identity"/);
    assert.match(types, /declare module "@geexcode\/geex-angular"/);
    assert.match(types, /reload\(\)/);
  });

  it("keeps auth init single-flight and exposes explicit reload", () => {
    const module = fs.readFileSync(path.join(__dirname, "auth.module.ts"), "utf8");
    assert.match(module, /reload:\s*\(\)\s*=>/);
    assert.doesNotMatch(module, /_initializedToken/);
    assert.doesNotMatch(module, /tokenChanged/);
  });
});
