import { describe, it } from "node:test";
import assert from "node:assert/strict";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

describe("provideGeexPayments", () => {
  it("registers payments through core module contributions", () => {
    const source = fs.readFileSync(path.join(__dirname, "provide-geex-payments.ts"), "utf8");
    assert.match(source, /payments:/);
    assert.match(source, /provideGeexModuleContribution/);
  });

  it("covers payment and refund gql operations", () => {
    const gql = fs.readFileSync(path.join(__dirname, "graphql.ts"), "utf8");
    assert.match(gql, /payments\(/);
    assert.match(gql, /paymentRefunds\(/);
    assert.match(gql, /closePayment/);
    assert.match(gql, /revokePayment/);
    assert.match(gql, /createPayment\(/);
    assert.match(gql, /createPaymentRefund/);
    assert.match(gql, /syncPaymentRefund/);
  });

  it("exposes createPayment on payments module API", () => {
    const types = fs.readFileSync(path.join(__dirname, "payments.types.ts"), "utf8");
    assert.match(types, /createPayment\(/);
    assert.match(types, /createPaymentRefund\(/);
    const moduleSource = fs.readFileSync(path.join(__dirname, "payments.module.ts"), "utf8");
    assert.match(moduleSource, /async createPayment/);
    assert.match(moduleSource, /GQL_CREATE_PAYMENT/);
  });
});
