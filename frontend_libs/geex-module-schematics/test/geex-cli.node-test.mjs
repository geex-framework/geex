import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { buildInvocation, isExecutedAsCli, runCli } from "../bin/geex.mjs";

describe("geex CLI", () => {
  it("dispatches add and forwards name, path and force", () => {
    const invocation = buildInvocation(
      ["add", "blob-storage", "--path=custom/modules", "--force"],
      "C:\\package\\collection.json",
      { schematicsCli: "C:\\schematics.js" },
    );
    assert.equal(invocation.executable, process.execPath);
    assert.deepEqual(invocation.args, [
      "C:\\schematics.js",
      "C:\\package\\collection.json:add-module",
      "blob-storage",
      "--path=custom/modules",
      "--overwrite",
    ]);
  });

  it("dispatches sync and forwards split path and force arguments", () => {
    const invocation = buildInvocation(
      ["sync", "identity", "--path", "src/custom/modules", "--force=true"],
      "collection.json",
      { schematicsCli: "schematics.js" },
    );
    assert.deepEqual(invocation.args, [
      "schematics.js",
      "collection.json:sync-module",
      "identity",
      "--path",
      "src/custom/modules",
      "--overwrite",
    ]);
  });

  it("defaults bare sync to --all", () => {
    const invocation = buildInvocation(["sync", "--force"], "collection.json", {
      schematicsCli: "schematics.js",
    });
    assert.deepEqual(invocation.args, [
      "schematics.js",
      "collection.json:sync-module",
      "--all",
      "--overwrite",
    ]);
  });

  it("compares CLI entry paths via realpath", () => {
    assert.equal(isExecutedAsCli(process.argv[1], import.meta.url), true);
    assert.equal(isExecutedAsCli(process.argv[1], "file:///W:/not-the-same-entry.mjs"), false);
  });

  it("rejects unknown commands without spawning a process", () => {
    let spawned = false;
    const errors = [];
    const exitCode = runCli(["remove", "identity"], {
      output: { log() {}, error(message) { errors.push(message); } },
      spawn() {
        spawned = true;
        return { status: 0 };
      },
    });
    assert.equal(exitCode, 1);
    assert.equal(spawned, false);
    assert.deepEqual(errors, ["Unknown command: remove"]);
  });

  it("returns the child process exit code", () => {
    const calls = [];
    const exitCode = runCli(["add", "identity"], {
      collection: "collection.json",
      schematicsCli: "schematics.js",
      output: { log() {}, error() {} },
      spawn(executable, args, options) {
        calls.push({ executable, args, options });
        return { status: 7 };
      },
    });
    assert.equal(exitCode, 7);
    assert.equal(calls.length, 1);
    assert.equal(calls[0].args[1], "collection.json:add-module");
    assert.equal(calls[0].options.shell, false);
  });
});
