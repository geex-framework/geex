import "@angular/compiler";
import { test } from "node:test";
import assert from "node:assert/strict";
import { from, of, throwError } from "rxjs";
import { register } from "node:module";
import { createRequire } from "node:module";
register("./test-loader.mjs", import.meta.url);
const { createBackupsModule } = await import("../dist/fesm2022/geexcode-geex-extensions-backups.mjs");
const requireApollo = createRequire(import.meta.resolve("apollo-angular"));
const { ApolloClient, ApolloLink, InMemoryCache } = requireApollo("@apollo/client");

test("list bypasses cache and preserves pagination and filters", async () => {
  const variables = { skip: 20, take: 10, filter: { status: { eq: "Failed" } } };
  const data = { backupsEnabled: true, backups: { items: [], totalCount: 24 } };
  const module = createBackupsModule({ get: () => ({ query: options => {
    assert.equal(options.fetchPolicy, "no-cache");
    assert.equal(options.errorPolicy, "none");
    assert.deepEqual(options.variables, variables);
    return of({ data });
  } }) });
  assert.deepEqual(await module.list(variables), data);
});

test("start reports rejected jobs and expire sends only the selected ID", async () => {
  const calls = [];
  const module = createBackupsModule({ get: () => ({ mutate: options => {
    assert.equal(options.errorPolicy, "none");
    calls.push(options);
    return of({ data: { startBackup: false, expireBackup: true } });
  } }) });
  assert.equal(await module.start(), false);
  assert.equal(await module.expire("chosen-backup"), true);
  assert.deepEqual(calls[1].variables, { id: "chosen-backup" });
});

test("real Apollo rejects GraphQL errors despite host ignore defaults", async () => {
  const client = new ApolloClient({
    cache: new InMemoryCache(),
    link: new ApolloLink(() => of({ data: { backupsEnabled: true, backups: null }, errors: [{ message: "permission denied" }] })),
    defaultOptions: { query: { errorPolicy: "ignore" }, mutate: { errorPolicy: "ignore" } },
  });
  const module = createBackupsModule({ get: () => ({
    query: options => from(client.query(options)),
    mutate: options => from(client.mutate(options)),
  }) });
  try {
    for (const action of [() => module.list({}), () => module.start(), () => module.startTracked(), () => module.expire("id")])
      await assert.rejects(action, /permission denied/);
  } finally { client.stop(); }
});

test("partial or missing responses are errors, while explicit rejection is preserved", async () => {
  for (const data of [undefined, {}, { backupsEnabled: true }, { backupsEnabled: true, backups: null }]) {
    const module = createBackupsModule({ get: () => ({ query: () => of({ data }), mutate: () => of({ data }) }) });
    await assert.rejects(() => module.list({}), /Incomplete/);
    await assert.rejects(() => module.start(), /Missing/);
    await assert.rejects(() => module.startTracked(), /Missing/);
    await assert.rejects(() => module.expire("id"), /Missing/);
  }
  for (const id of [null, "persisted-backup-id"]) {
    const module = createBackupsModule({ get: () => ({ mutate: () => of({ data: { startBackupTracked: id } }) }) });
    assert.equal(await module.startTracked(), id);
  }
});

test("transport failures propagate instead of reporting success", async () => {
  const module = createBackupsModule({ get: () => ({ mutate: () => throwError(() => new Error("offline")) }) });
  await assert.rejects(() => module.start(), /offline/);
});
