import assert from "node:assert/strict";
import { describe, it } from "node:test";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("GeexStartupService", () => {
  it("runs startup callbacks inside the configured injector context", () => {
    const source = fs.readFileSync(path.join(__dirname, "geex-startup.service.ts"), "utf8");
    assert.match(source, /runInInjectionContext\(this\.injector/);
    assert.match(source, /this\.options\.onDebuggerInit/);
  });

  it("bootstraps OIDC once in a linear pipeline", () => {
    const source = fs.readFileSync(path.join(__dirname, "geex-startup.service.ts"), "utf8");
    assert.match(source, /private bootstrapped/);
    assert.match(source, /tryOidcCodeCallback/);
    assert.match(source, /await this\.tryOidcCodeCallback\(\);\s*[\s\S]*this\.ensureSessionWatch\(\);\s*[\s\S]*await this\.geex\.init\(\);\s*[\s\S]*await this\.bindUiSession\(\)/);
    assert.match(source, /WechatWeb/);
    assert.doesNotMatch(source, /geex\.init\(true\)/);
  });
});
