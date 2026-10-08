import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { createHash } from "node:crypto";
import vm from "node:vm";
import ts from "typescript";
import { defer, firstValueFrom, from, map, of, switchMap } from "rxjs";

Object.defineProperty(Blob.prototype, "computeChecksumMd5", {
  configurable: true,
  value: async function () {
    return createHash("md5").update(Buffer.from(await this.arrayBuffer())).digest("hex");
  },
});

function loadFunction(file, name, globals = {}) {
  const source = ts.createSourceFile(file.pathname, readFileSync(file, "utf8"), ts.ScriptTarget.Latest, true);
  const declaration = source.statements.find(node => ts.isFunctionDeclaration(node) && node.name?.text === name);
  assert.ok(declaration, `${name} is missing in ${file}`);
  const code = ts.transpileModule(declaration.getText(source).replace(/^export\s+/, "") + `\n${name};`, {
    compilerOptions: { target: ts.ScriptTarget.ES2022 },
  }).outputText;
  return vm.runInNewContext(code, { Blob, File, Date, defer, firstValueFrom, from, map, of, switchMap, ...globals });
}

const content = new Blob(["attachment body"], { type: "text/plain" });
function deferred() {
  let resolve;
  const promise = new Promise(complete => { resolve = complete; });
  return { promise, resolve };
}
const fileName = "attachment.txt";
const md5 = createHash("md5").update("attachment body").digest("hex");
function candidate(overrides = {}) {
  return { id: "existing", fileName, fileSize: content.size, md5, mimeType: "text/plain",
    storageType: "FileSystem", url: "/files?fileId=existing", expireAt: null, ...overrides };
}
function page(items, hasNextPage = false) {
  return { blobObjects: { items, pageInfo: { hasNextPage } } };
}
function storageWith(items = []) {
  const queries = [], uploads = [];
  const storage = {
    defaultStorageType: "FileSystem",
    list: async variables => { queries.push(variables); return page(items); },
    create: async (variables, context) => {
      uploads.push({ variables, context });
      return { createBlobObject: candidate({ id: `created-${uploads.length}`, url: "/new" }) };
    },
  };
  return { storage, queries, uploads };
}

const bundle = new URL("../dist/fesm2022/geexcode-geex-extensions-blob-storage.mjs", import.meta.url);
for (const [label, file] of [["source", new URL("./attach-blob.ts", import.meta.url)], ["built package", bundle]]) {
  const attachBlob = loadFunction(file, "attachBlob");
  describe(`attachment reuse (${label})`, () => {
    for (const storageType of ["Db", "FileSystem"]) {
      it(`reuses an identical ${storageType} Blob without uploading or changing its expiration`, async () => {
        const existing = candidate({ storageType, mimeType: " TEXT/PLAIN ", expireAt: new Date(Date.now() + 3600000).toISOString() });
        const probe = storageWith([existing]);
        const result = await firstValueFrom(attachBlob(probe.storage, content, fileName, storageType));
        assert.equal(result, existing);
        assert.equal(result.url, existing.url);
        assert.equal(probe.uploads.length, 0);
        assert.deepEqual(JSON.parse(JSON.stringify(probe.queries[0].filter)), {
          fileName: { eq: fileName }, md5: { eq: md5 }, fileSize: { eq: content.size }, storageType: { eq: storageType },
        });
      });
    }

    for (const [label, mismatch] of [
      ["name", { fileName: "other.txt" }], ["content", { md5: "different" }],
      ["size", { fileSize: content.size + 1 }], ["MIME", { mimeType: "image/png" }],
      ["storage type", { storageType: "Db" }], ["missing ID", { id: "" }],
      ["expired", { expireAt: new Date(Date.now() - 3600000).toISOString() }],
      ["invalid expiration", { expireAt: "invalid" }],
    ]) {
      it(`uploads when the candidate differs in ${label}`, async () => {
        const probe = storageWith([candidate(mismatch)]);
        const result = await firstValueFrom(attachBlob(probe.storage, content, fileName));
        assert.equal(result.id, "created-1");
        assert.equal(probe.uploads.length, 1);
      });
    }

    it("hashes and uploads the same transformed content with its original attachment name", async () => {
      const probe = storageWith();
      const transformed = new Blob(["resized image"], { type: "IMAGE/PNG" });
      await firstValueFrom(attachBlob(probe.storage, transformed, "original.png", "Db"));
      const { variables: { request }, context } = probe.uploads[0];
      assert.ok(request.file instanceof File);
      assert.equal(request.file.name, "original.png");
      assert.equal(request.file.type, "image/png");
      assert.equal(await request.file.text(), "resized image");
      assert.equal(request.md5, createHash("md5").update("resized image").digest("hex"));
      assert.equal(request.storageType, "Db");
      assert.equal(context.useMultipart, true);
    });

    it("uses the same explicit MIME fallback for matching and uploading untyped content", async () => {
      const untyped = new Blob(["attachment body"]);
      const existing = candidate({ mimeType: "application/octet-stream" });
      const reused = storageWith([existing]);
      assert.equal(await firstValueFrom(attachBlob(reused.storage, untyped, fileName)), existing);
      assert.equal(reused.uploads.length, 0);
      const created = storageWith();
      await firstValueFrom(attachBlob(created.storage, untyped, fileName));
      assert.equal(created.uploads[0].variables.request.file.type, "application/octet-stream");
    });

    it("checks later pages when earlier candidates have expired or different MIME", async () => {
      const existing = candidate();
      const probe = storageWith();
      probe.storage.list = async variables => {
        probe.queries.push(variables);
        return variables.skip === 0
          ? page(Array.from({ length: 20 }, () => candidate({ mimeType: "image/png" })), true)
          : page([existing]);
      };
      assert.equal(await firstValueFrom(attachBlob(probe.storage, content, fileName)), existing);
      assert.deepEqual(probe.queries.map(query => query.skip), [0, 20]);
      assert.equal(probe.uploads.length, 0);
    });

    it("creates independent Cache attachments without searching for reusable records", async () => {
      const probe = storageWith([candidate({ storageType: "Cache" })]);
      const first = await firstValueFrom(attachBlob(probe.storage, content, fileName, "Cache"));
      const second = await firstValueFrom(attachBlob(probe.storage, content, fileName, "Cache"));
      assert.notEqual(first.id, second.id);
      assert.equal(probe.queries.length, 0);
      assert.equal(probe.uploads.length, 2);
    });

    for (const [label, result] of [["absent result", undefined], ["absent items", { blobObjects: {} }],
      ["empty nonterminal page", page([], true)]]) {
      it(`does not upload after a lookup with ${label}`, async () => {
        const probe = storageWith();
        probe.storage.list = async () => result;
        await assert.rejects(firstValueFrom(attachBlob(probe.storage, content, fileName)), /Blob lookup/);
        assert.equal(probe.uploads.length, 0);
      });
    }

    it("propagates lookup errors without silently creating a new resource", async () => {
      const probe = storageWith();
      const failure = new Error("lookup failed");
      probe.storage.list = async () => { throw failure; };
      await assert.rejects(firstValueFrom(attachBlob(probe.storage, content, fileName)), error => error === failure);
      assert.equal(probe.uploads.length, 0);
    });

    it("propagates upload errors and rejects missing mutation results", async () => {
      const probe = storageWith();
      const failure = new Error("upload failed");
      probe.storage.create = async () => { throw failure; };
      await assert.rejects(firstValueFrom(attachBlob(probe.storage, content, fileName)), error => error === failure);
      probe.storage.create = async () => undefined;
      await assert.rejects(firstValueFrom(attachBlob(probe.storage, content, fileName)), /Blob upload returned no result/);
    });

    it("does not start an upload after the attachment was cancelled during lookup", async () => {
      const probe = storageWith();
      const pending = deferred();
      const entered = deferred();
      probe.storage.list = () => { entered.resolve(); return pending.promise; };
      const subscription = attachBlob(probe.storage, content, fileName).subscribe({ next: () => assert.fail("cancelled attachment completed") });
      await entered.promise;
      subscription.unsubscribe();
      pending.resolve(page([]));
      await new Promise(resolve => setImmediate(resolve));
      assert.equal(probe.uploads.length, 0);
    });
  });
}

for (const [label, file] of [["source", new URL("./blob-storage.module.ts", import.meta.url)], ["built package", bundle]]) {
  it(`keeps ordinary create separate and queries fresh Blob metadata (${label})`, async () => {
    const queries = [], mutations = [];
    const createModule = loadFunction(file, "createBlobStorageModule", {
      Apollo: {}, GQL_BLOB_OBJECTS: "list", GQL_CREATE_BLOB_OBJECT: "create", GQL_DELETE_BLOB_OBJECT: "delete",
    });
    const storage = createModule({ get: () => ({
      query: options => { queries.push(options); return of({ data: page([]) }); },
      mutate: options => { mutations.push(options); return of({ data: { createBlobObject: candidate() } }); },
    }) }, { defaultStorageType: "Db" });
    await storage.create({ request: {} }, { useMultipart: true });
    assert.equal(mutations.length, 1);
    assert.equal(queries.length, 0);
    assert.equal(mutations[0].errorPolicy, "none");
    await storage.list({ filter: {} });
    assert.equal(queries[0].fetchPolicy, "no-cache");
    assert.equal(queries[0].errorPolicy, "none");
  });
}
