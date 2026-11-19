import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import vm from "node:vm";
import ts from "typescript";
import { ApolloClient, ApolloLink, InMemoryCache, gql } from "@apollo/client";
import { Observable, firstValueFrom, filter } from "rxjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

function loadDefaultOptions(file) {
  const source = ts.createSourceFile(file, fs.readFileSync(file, "utf8"), ts.ScriptTarget.Latest, true);
  const declaration = source.statements.find(statement => ts.isVariableStatement(statement) &&
    statement.declarationList.declarations.some(item => item.name.getText(source) === "geexApolloDefaultOptions"));
  assert.ok(declaration, `Apollo defaults are missing in ${file}`);
  const code = ts.transpileModule(declaration.getText(source).replace(/^export\s+/, "") +
    "\nglobalThis.options = geexApolloDefaultOptions;", { compilerOptions: { target: ts.ScriptTarget.ES2022 } }).outputText;
  return vm.runInNewContext(code + "\noptions;");
}

const query = gql`query CachePolicyProbe { value }`;
function createClient(defaultOptions) {
  let requests = 0;
  const cache = new InMemoryCache();
  const client = new ApolloClient({ cache, defaultOptions, link: new ApolloLink(() => new Observable(observer => {
    requests++;
    observer.next({ data: { value: "network" } });
    observer.complete();
  })) });
  return { client, cache, requests: () => requests };
}

for (const [label, file] of [
  ["source", path.join(__dirname, "provide-geex-apollo.ts")],
  ["built package", path.join(__dirname, "../dist/fesm2022/geexcode-geex-angular.mjs")],
]) {
  describe(`Apollo cache behavior (${label})`, () => {
    it("default query fetches fresh data without changing the global cache", async () => {
      const probe = createClient(loadDefaultOptions(file));
      probe.cache.writeQuery({ query, data: { value: "cached" } });
      const before = probe.cache.extract();
      assert.equal((await probe.client.query({ query })).data.value, "network");
      assert.equal((await probe.client.query({ query })).data.value, "network");
      assert.equal(probe.requests(), 2);
      assert.deepEqual(probe.cache.extract(), before);
      probe.client.stop();
    });

    it("watchQuery retains cache-first behavior", async () => {
      const probe = createClient(loadDefaultOptions(file));
      probe.cache.writeQuery({ query, data: { value: "cached" } });
      const result = await firstValueFrom(probe.client.watchQuery({ query }).pipe(filter(value => !value.loading)));
      assert.equal(result.data.value, "cached");
      assert.equal(probe.requests(), 0);
      probe.client.stop();
    });

    it("explicit fetchPolicy overrides the query default", async () => {
      const probe = createClient(loadDefaultOptions(file));
      await probe.client.query({ query, fetchPolicy: "network-only" });
      assert.equal(probe.cache.readQuery({ query }).value, "network");
      await probe.client.query({ query, fetchPolicy: "cache-first" });
      assert.equal(probe.requests(), 1);
      probe.client.stop();
    });
  });
}

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
