import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provide-geex-apollo", () => {
  it("exports headless Geex Apollo cache and http option factories", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-apollo.ts"), "utf8");
    assert.match(source, /export function createGeexInMemoryCache/);
    assert.match(source, /export function createGeexUriLink/);
    assert.match(source, /export function createGeexHttpApolloOptions/);
    assert.match(source, /geexApolloDefaultOptions/);
    assert.match(source, /export function geexDefaultTypePolicies/);
    assert.match(source, /export function createGeexGraphqlErrorLink/);
    assert.match(source, /export function createGeexSilentContextLink/);
    assert.match(source, /export function createGeexWsApolloOptions/);
    assert.match(source, /export function createGeexUploadHttpLink/);
    assert.match(source, /export function provideGeexApollo/);
    assert.match(source, /extract-files\/extractFiles\.mjs/);
    assert.match(source, /enableUpload/);
    assert.match(source, /createGeexUploadHttpLink\(httpLink\)/);
  });

  it("keeps feature type policies out of core", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-apollo.ts"), "utf8");
    assert.match(source, /Setting:\s*\{/);
    assert.doesNotMatch(source, /User:\s*\{/);
    assert.doesNotMatch(source, /Org:\s*\{/);
    assert.match(source, /GEEX_APOLLO_TYPE_POLICY_CONTRIBUTIONS/);
    assert.match(source, /provideGeexApolloTypePolicies/);
  });

  it("re-exports apollo helpers from package entry", () => {
    const api = fs.readFileSync(path.join(__dirname, "public-api.ts"), "utf8");
    assert.match(api, /provide-geex-apollo/);
  });

  it("exports IdentityClaims from package entry", () => {
    const api = fs.readFileSync(path.join(__dirname, "public-api.ts"), "utf8");
    assert.match(api, /identity-claims/);
  });
});
