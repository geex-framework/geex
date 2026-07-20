import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexAuditLogs", () => {
  it("registers auditLogs through core module contributions", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-audit-logs.ts"), "utf8");
    assert.match(source, /GEEX_AUDIT_LOGS_OPTIONS/);
    assert.match(source, /auditLogs:/);
    assert.match(source, /provideGeexModuleContribution/);
  });

  it("exposes loadAuditLogs and deleteAuditLogs helpers", () => {
    const moduleSource = fs.readFileSync(path.join(__dirname, "audit-logs.module.ts"), "utf8");
    assert.match(moduleSource, /loadAuditLogs/);
    assert.match(moduleSource, /deleteAuditLogs/);
  });
});
