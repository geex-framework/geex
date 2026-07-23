import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexMessaging", () => {
  it("registers messaging through module contributions with authentication hard dependency", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-messaging.ts"), "utf8");
    assert.match(source, /GEEX_MESSAGING_OPTIONS/);
    assert.match(source, /provideGeexModuleContribution/);
    assert.match(source, /return \{ messaging \}/);
    assert.match(source, /modules\["authentication"\]/);
    assert.match(source, /provideGeexMessaging\(\) requires provideGeexAuthentication\(\)/);
  });

  it("augments GeexModuleMap with messaging", () => {
    const types = fs.readFileSync(path.join(__dirname, "messaging.types.ts"), "utf8");
    assert.match(types, /declare module "@geexcode\/geex-angular"/);
    assert.match(types, /messaging: MessagingModule/);
  });

  it("covers editMessage and deleteMessageDistributions", () => {
    const gql = fs.readFileSync(path.join(__dirname, "graphql.ts"), "utf8");
    assert.match(gql, /editMessage\(/);
    assert.match(gql, /deleteMessageDistributions\(/);
    const types = fs.readFileSync(path.join(__dirname, "messaging.types.ts"), "utf8");
    assert.match(types, /editMessage\(/);
    assert.match(types, /deleteMessageDistributions\(/);
    const moduleSource = fs.readFileSync(path.join(__dirname, "messaging.module.ts"), "utf8");
    assert.match(moduleSource, /async editMessage/);
    assert.match(moduleSource, /async deleteMessageDistributions/);
  });
});
