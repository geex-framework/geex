import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexApprovalFlows", () => {
  it("registers the approvalFlows module through core module contributions", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-approval-flows.ts"), "utf8");
    assert.match(source, /GEEX_APPROVAL_FLOWS_OPTIONS/);
    assert.match(source, /provideGeexModuleContribution/);
    assert.match(source, /\(\{\s*approvalFlows:/);
    assert.match(source, /EnvironmentProviders/);
    assert.match(source, /makeEnvironmentProviders/);
  });

  it("owns the GeexApproveStatus enum and GeexModuleMap contract", () => {
    const types = fs.readFileSync(path.join(__dirname, "approval-flows.types.ts"), "utf8");
    assert.match(types, /export enum GeexApproveStatus/);
    assert.match(types, /from "@geexcode\/geex-angular"/);
    assert.match(types, /declare module "@geexcode\/geex-angular"/);
    assert.match(types, /approvalFlows:/);
  });
});
