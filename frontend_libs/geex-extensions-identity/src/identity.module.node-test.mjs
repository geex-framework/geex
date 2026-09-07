import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("identity.module cache warmup", () => {
  it("queries dedicated cache fields instead of users/orgs", () => {
    const source = fs.readFileSync(path.join(__dirname, "identity.module.ts"), "utf8");
    assert.match(source, /query orgsCache \{\s*orgsCache \{/);
    assert.match(source, /query usersCache \{\s*usersCache \{/);
    assert.doesNotMatch(source, /query orgsCache \{ orgs\(/);
    assert.doesNotMatch(source, /query usersCache \{\s*users\(/);
    assert.match(source, /data: \{ usersCache: \[\] \}/);
    assert.match(source, /data: \{ orgsCache: \[\] \}/);
  });
});
