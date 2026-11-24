import "@angular/compiler";
import { test } from "node:test";
import assert from "node:assert/strict";
import { register } from "node:module";
import { readFileSync } from "node:fs";
import { buildSchema, validate } from "graphql";
import { Subject, of, throwError } from "rxjs";
register("./test-loader.mjs", import.meta.url);
const { FavoritesService, createFavoriteQueryState, favoriteListVariables } = await import("../dist/fesm2022/geexcode-geex-extensions-favorites.mjs");
const documents = await import("../dist/fesm2022/geexcode-geex-extensions-favorites.mjs");
const configuration = [{ entityType: "ApprovalFlow", nameField: "Name" }];
const tick = () => new Promise(resolve => setImmediate(resolve));

test("all operations validate against the exported backend schema", { skip: !process.env.GEEX_FAVORITES_SCHEMA }, () => {
  // The integration host also contains intentionally invalid interface fixtures for schema tests.
  const schema = buildSchema(readFileSync(process.env.GEEX_FAVORITES_SCHEMA, "utf8"), { assumeValid: true });
  for (const name of ["FAVORITE_TYPES", "FAVORITE_STATUSES", "FAVORITES", "ADD_FAVORITE", "REMOVE_FAVORITE"])
    assert.deepEqual(validate(schema, documents[name]).map(error => error.message), [], name);
});

function setup(overrides = {}) {
  const calls = [], events = new Subject();
  let dispose;
  const apollo = {
    query: options => {
      calls.push(options);
      const name = options.query.definitions[0].name.value;
      if (name === "favoriteEntityTypes") return of({ data: { favoriteEntityTypes: configuration } });
      if (name === "favoriteStatuses") return of({ data: { favoriteStatuses: options.variables.entityIds.map(entityId => ({ entityType: "ApprovalFlow", entityId, isFavorite: false })) } });
      return of({ data: { favorites: { items: [], totalCount: 0 } } });
    },
    mutate: options => {
      calls.push(options);
      return of({ data: { addFavorite: { ...options.variables.request, isFavorite: true, createdOn: "2026-11-23T00:00:00Z" }, removeFavorite: true } });
    }, ...overrides,
  };
  const service = new FavoritesService({ get: token => token.name === "Apollo" ? apollo : token.name === "OAuthService"
    ? { events, hasValidAccessToken: () => true } : { onDestroy: callback => { dispose = callback; } } });
  return { service, calls, events, dispose: () => dispose() };
}

test("list buttons batch status requests and share confirmed mutation state", async () => {
  const { service, calls, dispose } = setup();
  await service.init();
  for (const id of ["one", "two", "one"]) service.requestStatus("ApprovalFlow", id);
  await tick();
  assert.equal(calls.length, 2);
  assert.deepEqual(calls[1].variables.entityIds, ["one", "two"]);
  assert.equal(service.state("ApprovalFlow", "one").isFavorite, false);
  await service.setFavorite("ApprovalFlow", "one", true);
  assert.equal(service.state("ApprovalFlow", "one").isFavorite, true);
  assert.equal(service.state("ApprovalFlow", "one").createdOn, "2026-11-23T00:00:00Z");
  await service.setFavorite("ApprovalFlow", "one", false);
  assert.equal(service.state("ApprovalFlow", "one").isFavorite, false);
  assert.equal(service.state("ApprovalFlow", "one").createdOn, null);
  assert.ok(calls.every(call => call.errorPolicy === "none"));
  dispose();
});

test("disabled types cause no requests and an empty configuration hides all types", async () => {
  const { service, calls, dispose } = setup();
  await service.init();
  service.requestStatus("Other", "one");
  await service.setFavorite("Other", "one", true);
  await tick();
  assert.equal(calls.length, 1);
  service.reset();
  assert.deepEqual(service.types(), []);
  assert.equal(service.enabled("ApprovalFlow"), false);
  dispose();
});

test("logout discards delayed status and mutation responses", async () => {
  for (const operation of ["status", "mutation"]) {
    const delayed = new Subject();
    const context = setup(operation === "status" ? { query: options => options.query.definitions[0].name.value === "favoriteEntityTypes"
      ? of({ data: { favoriteEntityTypes: configuration } }) : delayed } : { mutate: () => delayed });
    await context.service.init();
    const pending = operation === "mutation" ? context.service.setFavorite("ApprovalFlow", "one", true) : undefined;
    if (operation === "status") { context.service.requestStatus("ApprovalFlow", "one"); await tick(); }
    context.events.next({ type: "logout" });
    delayed.next({ data: { addFavorite: { entityType: "ApprovalFlow", entityId: "one", isFavorite: true },
      favoriteStatuses: [{ entityType: "ApprovalFlow", entityId: "one", isFavorite: true }] } });
    await pending; await tick();
    assert.deepEqual(context.service.types(), []);
    assert.deepEqual(context.service.states(), {});
    context.dispose();
  }
});

test("failed mutations preserve previous state and duplicate clicks are suppressed", async () => {
  const delayed = new Subject();
  let mutations = 0;
  const context = setup({ mutate: () => { mutations++; return delayed; } });
  await context.service.init();
  const first = context.service.setFavorite("ApprovalFlow", "one", true);
  await context.service.setFavorite("ApprovalFlow", "one", true);
  assert.equal(mutations, 1);
  delayed.error(new Error("offline"));
  await assert.rejects(first, /offline/);
  assert.equal(context.service.state("ApprovalFlow", "one").isFavorite, false);
  assert.equal(context.service.state("ApprovalFlow", "one").pending, false);
  context.dispose();
});

test("list failures are not presented as empty successful results", async () => {
  for (const response of [of({ data: {} }), throwError(() => new Error("denied"))]) {
    const { service, dispose } = setup({ query: () => response });
    await assert.rejects(service.list({ entityType: "ApprovalFlow", skip: 0, take: 10 }));
    dispose();
  }
});

test("date filtering covers whole local dates and ordering precedes remote pagination", () => {
  const first = createFavoriteQueryState(), second = createFavoriteQueryState();
  first.pageIndex = 3; first.pageSize = 20; first.sort = "ascend";
  first.dates = [new Date(2026, 10, 20, 12), new Date(2026, 10, 23, 12)];
  const variables = favoriteListVariables("ApprovalFlow", first);
  assert.equal(variables.skip, 40); assert.equal(variables.take, 20);
  assert.deepEqual(variables.sort, [{ createdOn: "ascend" }, { id: "ascend" }]);
  assert.equal(variables.filter.createdOn.gte, new Date(2026, 10, 20, 0).toISOString());
  assert.equal(variables.filter.createdOn.lt, new Date(2026, 10, 24, 0).toISOString());
  assert.equal(favoriteListVariables("Other", second).filter, undefined);
  assert.deepEqual(favoriteListVariables("Other", second).sort, [{ createdOn: "descend" }, { id: "descend" }]);
});

test("re-entering a favorites route reads the new configuration and propagates query errors", async () => {
  let answer = of({ data: { favoriteEntityTypes: configuration } });
  const { service, dispose } = setup({ query: () => answer });
  await service.init();
  answer = of({ data: { favoriteEntityTypes: [] } });
  assert.equal(service.enabled("ApprovalFlow"), true);
  await service.refresh();
  assert.equal(service.enabled("ApprovalFlow"), false);
  answer = throwError(() => new Error("settings unavailable"));
  await assert.rejects(service.refresh(), /settings unavailable/);
  dispose();
});
