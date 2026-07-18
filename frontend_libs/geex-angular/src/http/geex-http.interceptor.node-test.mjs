import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("GeexHttpInterceptor", () => {
  it("ships default zh-CN status messages and login path tokens", () => {
    const tokens = fs.readFileSync(path.join(__dirname, "tokens.ts"), "utf8");
    assert.match(tokens, /GEEX_DEFAULT_HTTP_STATUS_MESSAGES/);
    assert.match(tokens, /GEEX_HTTP_STATUS_MESSAGES/);
    assert.match(tokens, /GEEX_LOGIN_PATH/);
    assert.match(tokens, /\/auth\/login/);
    assert.match(tokens, /GEEX_AFTER_LOGIN_NAVIGATE/);
    assert.match(tokens, /SILENT_REQUEST/);
  });

  it("exposes override hooks on the interceptor", () => {
    const source = fs.readFileSync(path.join(__dirname, "geex-http.interceptor.ts"), "utf8");
    for (const hook of [
      "notifyHttpError",
      "onUnauthorized",
      "handleHttpStatus",
      "shouldAttachTenant",
      "buildLoginConfirmOptions",
      "handleGraphQLErrors",
      "handleGraphQLNetworkError",
      "buildCommonHeaders",
    ]) {
      assert.match(source, new RegExp(hook));
    }
  });

  it("re-exports http surface from package entry", () => {
    const api = fs.readFileSync(path.join(__dirname, "..", "public-api.ts"), "utf8");
    assert.match(api, /export \* from "\.\/http"/);
  });
});
