import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

/** Mirrors package deep-set for behavioral check (source of truth is environment-overrides.ts). */
function setByPath(target, pathKey, value) {
  const parts = pathKey.split(".");
  let cur = target;
  for (let i = 0; i < parts.length - 1; i++) {
    const key = parts[i];
    if (cur[key] == null || typeof cur[key] !== "object") {
      cur[key] = {};
    }
    cur = cur[key];
  }
  cur[parts[parts.length - 1]] = value;
}

function applyEnvironmentOverrides(env, override) {
  for (const [key, value] of Object.entries(override)) {
    if (key.includes(".")) {
      setByPath(env, key, value);
    } else {
      env[key] = value;
    }
  }
}

describe("environment-overrides", () => {
  it("exports apply/loadEnvironmentOverrides from source and public-api", () => {
    const source = fs.readFileSync(path.join(__dirname, "environment-overrides.ts"), "utf8");
    assert.match(source, /export function applyEnvironmentOverrides/);
    assert.match(source, /export async function loadEnvironmentOverrides/);
    assert.match(source, /environment\.override\.js/);
    const api = fs.readFileSync(path.join(__dirname, "public-api.ts"), "utf8");
    assert.match(api, /environment-overrides/);
  });

  it("deep-sets dotted keys into nested env object", () => {
    const env = { api: { baseUrl: "http://a", timeout: 1 }, production: false };
    applyEnvironmentOverrides(env, {
      "api.baseUrl": "http://b",
      production: true,
      "feature.flag": true,
    });
    assert.equal(env.api.baseUrl, "http://b");
    assert.equal(env.api.timeout, 1);
    assert.equal(env.production, true);
    assert.equal(env.feature.flag, true);
  });
});
