import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexApprovalFlows", () => {
  it("exposes readonly options and environment providers", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-approval-flows.ts"), "utf8");
    assert.match(source, /GeexApprovalFlowsOptions/);
    assert.match(source, /GEEX_APPROVAL_FLOWS_OPTIONS/);
    assert.match(source, /EnvironmentProviders/);
    assert.match(source, /makeEnvironmentProviders/);
  });
});
