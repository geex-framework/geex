#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import fs from "node:fs";
import { createRequire } from "node:module";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const collection = path.resolve(__dirname, "../src/collection.json");
const require = createRequire(import.meta.url);

function resolveSchematicsCli() {
  return require.resolve("@angular-devkit/schematics-cli/bin/schematics.js");
}

export function isExecutedAsCli(entryPath = process.argv[1], metaUrl = import.meta.url) {
  if (!entryPath) {
    return false;
  }
  try {
    return fs.realpathSync.native(path.resolve(entryPath)) === fs.realpathSync.native(fileURLToPath(metaUrl));
  } catch {
    return path.resolve(entryPath) === fileURLToPath(metaUrl);
  }
}

export function buildInvocation(argv, resolvedCollection = collection, dependencies = {}) {
  const [command, ...rest] = argv;
  if (!command || command === "help" || command === "--help") {
    return {
      exitCode: command ? 0 : 1,
      message: `Usage:
  geex add <name> [--path=src/app/modules] [--force]
  geex sync [<name>|--all] [--path=src/app/modules] [--force]
`,
    };
  }

  const schematic = command === "add" ? "add-module" : command === "sync" ? "sync-module" : null;
  if (!schematic) {
    return { exitCode: 1, error: `Unknown command: ${command}` };
  }

  const schematicArgs = rest
    .map(argument => {
      if (argument === "--force" || argument === "--force=true") {
        return "--overwrite";
      }
      if (argument === "--force=false") {
        return null;
      }
      return argument;
    })
    .filter(Boolean);

  if (command === "sync" && !schematicArgs.some(argument => !argument.startsWith("-"))) {
    if (!schematicArgs.includes("--all")) {
      schematicArgs.unshift("--all");
    }
  }

  return {
    executable: process.execPath,
    args: [
      dependencies.schematicsCli ?? resolveSchematicsCli(),
      `${resolvedCollection}:${schematic}`,
      ...schematicArgs,
    ],
  };
}

export function runCli(argv, dependencies = {}) {
  const invocation = buildInvocation(argv, dependencies.collection ?? collection, dependencies);
  const output = dependencies.output ?? console;
  if (invocation.message) {
    output.log(invocation.message);
    return invocation.exitCode;
  }
  if (invocation.error) {
    output.error(invocation.error);
    return invocation.exitCode;
  }

  const spawn = dependencies.spawn ?? spawnSync;
  const result = spawn(invocation.executable, invocation.args, { stdio: "inherit", shell: false });
  return result.status ?? 1;
}

if (isExecutedAsCli()) {
  process.exitCode = runCli(process.argv.slice(2));
}
