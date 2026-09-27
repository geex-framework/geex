import { test } from "node:test";
import assert from "node:assert/strict";
import { register } from "node:module";
import { Subject, BehaviorSubject } from "rxjs";
register("./page-loader.mjs", import.meta.url);
const { BackupsListPage } = await import("../../geex-module-schematics/files/extensions/backups/pages/list.page.ts");
const { default: text } = await import("../../geex-module-schematics/files/extensions/backups/i18n/en-US.ts");

const record = (id = "backup", status = "Succeeded") => ({ id, status, databaseName: "isolated", source: "Manual",
  scheduledAt: "2026-09-21T03:00:00Z", startedAt: "2026-09-21T03:00:00Z", file: { id: "blob", url: "/download", fileSize: 1024 } });
const result = (items = [], totalCount = items.length, backupsEnabled = true) => ({ backupsEnabled, backups: { items, totalCount } });
function deferred() {
  let resolve, reject;
  const promise = new Promise((yes, no) => { resolve = yes; reject = no; });
  return { promise, resolve, reject };
}
async function setup(api = {}, permission = true) {
  const messages = [], confirmations = [], references = [], calls = [];
  const notifications = new Subject(), events = new Subject(), authEvents = new Subject();
  const state = { permissions: permission, api: {
    list: async variables => { calls.push(variables); return result(); },
    watch: () => ({ unsubscribe() {} }), startTracked: async () => "accepted-id", expire: async () => true, ...api,
  }, messaging: { watchPrivateNotifications: listener => notifications.subscribe(listener) }, services: {
    GEEX_I18N: { Backups: text }, ACLService: { can: () => state.permissions },
    Router: { events, url: "/backups", getCurrentNavigation: () => null },
    OAuthService: { events: authEvents }, GeexSubscriptionConnection: { changes: new BehaviorSubject("connected") },
    NzMessageService: Object.fromEntries(["success", "warning", "error"].map(kind => [kind, value => messages.push([kind, value])])),
    NzModalService: { confirm: options => {
      confirmations.push(options);
      const afterClose = new Subject();
      const ref = { afterClose, closed: false, configuration: {}, close() { this.closed = true; afterClose.next(); afterClose.complete(); },
        updateConfig(value) { Object.assign(this.configuration, value); } };
      references.push(ref); return ref;
    } }, DestroyRef: { onDestroy: action => { state.dispose = action; } },
  } };
  globalThis.history = { state: {} };
  globalThis.backupPageTest = state;
  const page = new BackupsListPage(); page.ngOnInit();
  await Promise.resolve(); calls.length = 0;
  return { page, messages, confirmations, references, calls, state, notifications, authEvents };
}

test("accepted manual submission clears the query while retaining normal disabled-schedule operation", async () => {
  const { page, confirmations, calls } = await setup({ list: async () => result([], 0, false) });
  page.database = "other"; page.status = "Failed"; page.pageIndex = 3;
  page.confirmStart(); await confirmations[0].nzOnOk();
  assert.equal(page.database, ""); assert.equal(page.status, ""); assert.equal(page.pageIndex, 1);
  assert.equal(page.enabled(), false); assert.equal(page.acting(), false);
});

test("busy, server errors and unconfirmed network outcomes remain distinct", async () => {
  for (const [outcome, expected] of [[null, text.busy], [{ errors: [{ message: "denied" }] }, text.failed], [new Error("offline"), text.resultUnknown]]) {
    let submissions = 0;
    const { page, messages, confirmations } = await setup({ startTracked: async () => { submissions++; if (outcome !== null) throw outcome; return null; } });
    page.confirmStart(); await confirmations[0].nzOnOk();
    assert.equal(messages[0][1], expected); assert.equal(submissions, 1); assert.equal(page.acting(), false);
  }
});

test("only a failed silent refresh of the displayed query retains old records", async () => {
  const { page, state, confirmations } = await setup({ list: async () => result([record()]) });
  state.api.list = async () => { throw new Error("offline"); };
  await page.load(true);
  assert.equal(page.data()[0].id, "backup"); assert.equal(page.hasResult(), true); assert.equal(page.loadError(), true);
  page.confirmExpire(page.data()[0]); assert.equal(confirmations.length, 0);
  page.confirmStart(); assert.equal(confirmations.length, 1);
  page.database = "new query"; page.search(); await Promise.resolve();
  assert.deepEqual(page.data(), []); assert.equal(page.hasResult(), false); assert.equal(page.total(), 0);
});

test("visible minute bounds ignore hidden seconds and include the end minute", async () => {
  const { page, calls } = await setup();
  page.dateRange = [new Date("2026-09-21T03:00:59.999Z"), new Date("2026-09-21T03:00:01.111Z")];
  page.search();
  assert.deepEqual(calls.at(-1).filter, { scheduledAt: { gte: "2026-09-21T03:00:00.000Z", lt: "2026-09-21T03:01:00.000Z" } });
  page.database = "draft"; await page.load(true);
  assert.equal(calls.at(-1).filter.databaseName, undefined);
  page.pageIndex = 3; page.onTableChange({ type: "ps", pi: 3, ps: 20 });
  assert.equal(calls.at(-1).skip, 0); assert.equal(calls.at(-1).take, 20);
});

test("an empty out-of-range page is corrected before it is published", async () => {
  const { page, state } = await setup();
  const skips = [];
  state.api.list = async input => { skips.push(input.skip); return input.skip ? result([], 10) : result([record()], 10); };
  page.pageIndex = 2; await page.load(true);
  assert.deepEqual(skips, [10, 0]); assert.equal(page.pageIndex, 1); assert.equal(page.data()[0].id, "backup");
  page.pageIndex = 2; state.api.list = async () => result(); await page.load(true);
  assert.equal(page.pageIndex, 1); assert.equal(page.total(), 0); assert.equal(page.hasResult(), true);
});

test("out-of-order responses cannot replace newer data", async () => {
  const { page, state } = await setup();
  const old = deferred(), latest = deferred(); let next = 0;
  state.api.list = () => (next++ ? latest.promise : old.promise);
  const first = page.load(); const second = page.load();
  latest.resolve(result([record("new")])); await second;
  old.resolve(result([record("old")])); await first;
  assert.equal(page.data()[0].id, "new"); assert.equal(page.loading(), false);
});

test("a single confirmation guards repeated submissions and finishes before a slow history query", async () => {
  const mutation = deferred(); let submissions = 0;
  const { page, state, confirmations, references } = await setup({ startTracked: () => { submissions++; return mutation.promise; } });
  page.confirmStart(); page.confirmStart(); assert.equal(confirmations.length, 1);
  const pending = confirmations[0].nzOnOk();
  assert.deepEqual(references[0].configuration, { nzCancelDisabled: true, nzKeyboard: false });
  assert.equal(await confirmations[0].nzOnOk(), false); assert.equal(submissions, 1);
  const history = deferred(); state.api.list = () => history.promise;
  mutation.resolve("accepted"); await pending;
  assert.equal(page.acting(), false); assert.equal(references[0].closed, true);
  history.resolve(result());
});

test("final cleanup confirmation rechecks permission, current status and failed queries", async () => {
  for (const invalidate of [context => { context.state.permissions = false; }, context => context.page.loadError.set(true), context => context.page.data.set([record("backup", "Expired")])]) {
    let calls = 0;
    const context = await setup({ list: async () => result([record()]), expire: async () => { calls++; return true; } });
    context.page.confirmExpire(context.page.data()[0]); invalidate(context);
    await context.confirmations[0].nzOnOk(); assert.equal(calls, 0); assert.equal(context.references[0].closed, true);
  }
});

test("cleanup uses a target action and errors block downloads until a successful cleanup", async () => {
  const mutation = deferred();
  const { page, state, confirmations } = await setup({ list: async () => result([record()]), expire: () => mutation.promise });
  page.confirmExpire(page.data()[0]); const pending = confirmations[0].nzOnOk();
  assert.equal(page.pendingAction().kind, "expire"); assert.equal(page.canDownload(record()), false);
  const failed = { ...record(), errorMessage: "Archive cleanup failed" };
  state.api.list = async () => result([failed]); mutation.reject({ errors: [{ message: "failed" }] });
  await pending; await Promise.resolve();
  assert.equal(page.data()[0].errorMessage, failed.errorMessage); assert.equal(page.canDownload(page.data()[0]), false);
  state.api.expire = async () => true; state.api.list = async () => result([{ ...record("backup", "Expired"), file: null }]);
  page.confirmExpire(page.data()[0]); await confirmations[1].nzOnOk(); await Promise.resolve();
  assert.equal(page.data()[0].status, "Expired"); assert.equal(page.data()[0].errorMessage, undefined);
});

test("logout closes confirmations and stale callbacks cannot submit", async () => {
  let submissions = 0;
  const { page, confirmations, references, authEvents } = await setup({ startTracked: async () => { submissions++; return "id"; } });
  page.confirmStart(); authEvents.next({ type: "logout" });
  assert.equal(references[0].closed, true); await confirmations[0].nzOnOk(); assert.equal(submissions, 0);
});

test("a result from a previous visit cannot reset the new draft or issue a query", async () => {
  const mutation = deferred();
  const { page, state, confirmations, authEvents, calls } = await setup({ startTracked: () => mutation.promise });
  page.confirmStart(); const pending = confirmations[0].nzOnOk();
  authEvents.next({ type: "logout" }); authEvents.next({ type: "token_received" }); await Promise.resolve();
  page.database = "new visit"; calls.length = 0;
  mutation.resolve("old result"); await pending;
  assert.equal(page.database, "new visit"); assert.equal(calls.length, 0); state.dispose();
});

test("only persisted statuses are exposed and cleared archives retain their history label", async () => {
  const { page } = await setup();
  assert.deepEqual(page.statuses, ["Running", "Succeeded", "Failed", "Cancelled", "Expired"]);
  assert.equal(page.statusLabel("Expired"), "Cleaned up");
  page.database = "missing"; page.search(); assert.equal(page.emptyTitle(), text.noMatches);
});
