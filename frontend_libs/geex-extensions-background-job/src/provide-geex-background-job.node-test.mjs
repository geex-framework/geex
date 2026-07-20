import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexBackgroundJob", () => {
  it("registers backgroundJob through core module contributions", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-background-job.ts"), "utf8");
    assert.match(source, /backgroundJob:/);
    assert.match(source, /provideGeexModuleContribution/);
  });

  it("queries jobState with nested executionHistories", () => {
    const gql = fs.readFileSync(path.join(__dirname, "graphql.ts"), "utf8");
    assert.match(gql, /executionHistories/);
    const moduleSource = fs.readFileSync(path.join(__dirname, "background-job.module.ts"), "utf8");
    assert.match(moduleSource, /loadJobStates/);
  });
});
