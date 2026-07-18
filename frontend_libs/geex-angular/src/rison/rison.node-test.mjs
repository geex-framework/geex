import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { createRequire } from "node:module";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const require = createRequire(import.meta.url);
const rison = require("./rison.js");

describe("rison.decode_query_param", () => {
  it("strips rison quotes from digit-leading tokens (GeexRouter roundtrip)", () => {
    const token = "6a55fa9a714a8628249b4290";
    const encoded = rison.encode(token);
    assert.equal(encoded, `"${token}"`);
    assert.equal(rison.decode_query_param(encoded), token);
  });

  it("keeps plain identifier strings without adding quotes", () => {
    const displayName = "wechat";
    const encoded = rison.encode(displayName);
    assert.equal(encoded, displayName);
    assert.equal(rison.decode_query_param(encoded), displayName);
  });

  it("prevents nested quotes when value is later JSON5-stringified", () => {
    const token = "6a55fa9a714a8628249b4290";
    const encoded = rison.encode(token);
    const decoded = rison.decode_query_param(encoded);
    const json5Like = `'${decoded.replace(/'/g, "\\'")}'`;
    assert.equal(json5Like, `'${token}'`);
    assert.notEqual(json5Like, `'"${token}"'`);
  });

  it("returns empty string for nullish / empty input", () => {
    assert.equal(rison.decode_query_param(null), "");
    assert.equal(rison.decode_query_param(undefined), "");
    assert.equal(rison.decode_query_param(""), "");
  });

  it("falls back to raw value when input is not valid rison", () => {
    const raw = "not a valid ' rison !!";
    assert.equal(rison.decode_query_param(raw), raw);
  });
});
