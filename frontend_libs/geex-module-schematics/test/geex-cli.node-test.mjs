import assert from "node:assert/strict";
import { describe, it } from "node:test";

import { buildInvocation, runCli } from "../bin/geex.mjs";

describe("geex CLI", () => {
  it("dispatches add and forwards name, path and force", () => {
    const invocation = buildInvocation(
      ["add", "blob-storage", "--path=custom/modules", "--force"],
      "C:\\package\\collection.json",
    );
    assert.equal(invocation.executable, "npx");
    assert.deepEqual(invocation.args, [
      "@angular-devkit/schematics-cli",
      "C:\\package\\collection.json:add-module",
      "blob-storage",
      "--path=custom/modules",
      "--overwrite",
    ]);
    assert.equal(invocation.args.includes("schematic"), false);
  });

  it("dispatches sync and forwards split path and force arguments", () => {
    const invocation = buildInvocation(
      ["sync", "identity", "--path", "src/custom/modules", "--force=true"],
      "collection.json",
    );
    assert.deepEqual(invocation.args, [
      "@angular-devkit/schematics-cli",
      "collection.json:sync-module",
      "identity",
      "--path",
      "src/custom/modules",
      "--overwrite",
    ]);
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
      output: { log() {}, error() {} },
      spawn(executable, args, options) {
        calls.push({ executable, args, options });
        return { status: 7 };
      },
    });
    assert.equal(exitCode, 7);
    assert.equal(calls.length, 1);
    assert.equal(calls[0].args[1], "collection.json:add-module");
  });
});
