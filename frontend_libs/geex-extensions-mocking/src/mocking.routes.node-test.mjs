import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const routesSource = fs.readFileSync(path.join(__dirname, "mocking.routes.ts"), "utf8");

function extractRouteBlock(source, pathLiteral) {
  const marker = `path: "${pathLiteral}"`;
  const start = source.indexOf(marker);
  assert.ok(start >= 0, `route ${pathLiteral} should exist`);
  const braceStart = source.lastIndexOf("{", start);
  let depth = 0;
  for (let i = braceStart; i < source.length; i++) {
    if (source[i] === "{") depth++;
    if (source[i] === "}") {
      depth--;
      if (depth === 0) {
        return source.slice(braceStart, i + 1);
      }
    }
  }
  assert.fail(`unable to parse route block for ${pathLiteral}`);
}

describe("mocking wechat authorize routes", () => {
  it("keeps public authorize route outside superAdmin canMatch", () => {
    const authorizeBlock = extractRouteBlock(routesSource, "wechat/authorize/:token");
    assert.match(authorizeBlock, /mockingEnabledCanMatch/);
    assert.doesNotMatch(authorizeBlock, /mockingSuperAdminCanMatch/);
  });

  it("declares authorize before admin children root", () => {
    const authorizePos = routesSource.indexOf('path: "wechat/authorize/:token"');
    const adminChildrenPos = routesSource.indexOf("children: [");
    assert.ok(authorizePos >= 0);
    assert.ok(adminChildrenPos >= 0);
    assert.ok(authorizePos < adminChildrenPos, "authorize must be declared before admin children");
  });

  it("does not nest authorize under admin children", () => {
    const childrenMarker = "children: [";
    const childrenStart = routesSource.indexOf(childrenMarker);
    assert.ok(childrenStart >= 0);
    const childrenBody = routesSource.slice(childrenStart);
    assert.doesNotMatch(childrenBody, /wechat\/authorize\/:token/);
    assert.doesNotMatch(childrenBody, /payments\/:token/);
  });

  it("resolves OAuthService synchronously before async capabilities work", () => {
    const fnStart = routesSource.indexOf("export const mockingSuperAdminCanMatch");
    const fnBody = routesSource.slice(fnStart, routesSource.indexOf("export const mockingRoutes"));
    const oauthInjectPos = fnBody.indexOf("inject(OAuthService");
    const thenPos = fnBody.indexOf(".then(");
    const awaitPos = fnBody.indexOf("await ");
    assert.ok(oauthInjectPos >= 0, "should inject OAuthService");
    assert.equal(awaitPos, -1, "should not use async/await in canMatch");
    assert.ok(thenPos > oauthInjectPos, "inject(OAuthService) must happen before .then(...)");
  });

  it("mockingEnabledCanMatch does not inject OAuthService", () => {
    const fnStart = routesSource.indexOf("export const mockingEnabledCanMatch");
    const fnBody = routesSource.slice(fnStart, routesSource.indexOf("export const mockingSuperAdminCanMatch"));
    assert.doesNotMatch(fnBody, /OAuthService/);
  });
});
