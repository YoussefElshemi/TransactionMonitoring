import assert from "node:assert/strict";
import test from "node:test";

import {
  MODE_STORAGE_KEY,
  effectiveModeMatches,
  isTestCredential,
  modeRequestHeader,
  registerModeScopedCache,
  setEnvironmentMode,
} from "../app/lib/mode.ts";
import {
  SANDBOX_RESET_CONFIRMATION,
  isSandboxExhausted,
  isSandboxResetConfirmation,
  notifySandboxCreditSpent,
  readSandboxCreditProblem,
  sandboxCreditPercentRemaining,
  sandboxExhaustedDescription,
  sandboxRateLimitDescription,
  sandboxResetDescription,
  spendsSandboxCredit,
  subscribeToSandboxCredit,
} from "../app/lib/sandbox.ts";

const values = new Map();
globalThis.window = {
  localStorage: {
    getItem: (key) => values.get(key) ?? null,
    setItem: (key, value) => values.set(key, value),
  },
};

test("Test mode persists, and every request states its environment", () => {
  setEnvironmentMode("live", { persist: false });
  setEnvironmentMode("test");
  assert.equal(values.get(MODE_STORAGE_KEY), "test");
  assert.equal(modeRequestHeader(), "test");

  setEnvironmentMode("live");
  assert.equal(modeRequestHeader(), "live");
});

test("switching environments invalidates registered mode-scoped caches", () => {
  let invalidations = 0;
  const unregister = registerModeScopedCache(() => { invalidations += 1; });
  setEnvironmentMode("test", { persist: false });
  setEnvironmentMode("live", { persist: false });
  unregister();
  assert.equal(invalidations, 2);
});

test("test credential prefixes are never classified as Live", () => {
  assert.equal(isTestCredential("tmc_test_client"), true);
  assert.equal(isTestCredential("tmsec_test_secret"), true);
  assert.equal(isTestCredential("tmc_live_client"), false);
});

test("an effective response mode cannot cross the requested boundary", () => {
  assert.equal(effectiveModeMatches("test", "test"), true);
  assert.equal(effectiveModeMatches("live", "test"), false);
  assert.equal(effectiveModeMatches("test", "live"), false);
  assert.equal(effectiveModeMatches(null, "live"), true);
});

test("Sandbox credit presentation distinguishes available and exhausted balances", () => {
  assert.equal(isSandboxExhausted({ remaining: 1 }), false);
  assert.equal(isSandboxExhausted({ remaining: 0 }), true);
  assert.equal(sandboxCreditPercentRemaining({ allowance: 200, remaining: 50 }), 25);
  assert.match(sandboxRateLimitDescription(2_000), /120 requests\/minute/);
  assert.match(sandboxRateLimitDescription(2_000), /2 seconds/);
});

test("Sandbox reset requires the deliberate confirmation and describes its boundary", () => {
  assert.equal(isSandboxResetConfirmation("reset"), false);
  assert.equal(isSandboxResetConfirmation(SANDBOX_RESET_CONFIRMATION), true);
  const description = sandboxResetDescription("Northwind");
  assert.match(description, /Test API keys are kept/);
  assert.doesNotMatch(description, /deletes[^.]*Test API keys/);
  assert.match(description, /Monthly credit usage is not reset/);
  assert.match(description, /Live data and Live credentials are not changed/);
});

test("every Test mutation spends a credit, except the way out of an empty one", () => {
  assert.equal(spendsSandboxCredit("POST", "/queues"), true);
  assert.equal(spendsSandboxCredit("PATCH", "/rules/r1"), true);
  assert.equal(spendsSandboxCredit("DELETE", "/webhooks/w1"), true);
  assert.equal(spendsSandboxCredit("POST", "/rules/r1/evaluate"), true);

  assert.equal(spendsSandboxCredit("GET", "/queues"), false);
  assert.equal(spendsSandboxCredit("POST", "/organizations/org-1/sandbox/reset"), false);
  assert.equal(spendsSandboxCredit("POST", "/organizations/my/sandbox/reset"), false);
  assert.equal(spendsSandboxCredit("POST", "/auth/login"), false);
});

test("an exhausted allowance is explained as unretryable, with the balance and the reset", () => {
  const credit = readSandboxCreditProblem({
    title: "Sandbox credit exhausted",
    allowance: 250,
    used: 250,
    remaining: 0,
    requested: 1,
    resetsAt: "2026-10-01T00:00:00Z",
    mode: "Test",
  });

  assert.deepEqual(credit, {
    allowance: 250,
    used: 250,
    remaining: 0,
    requested: 1,
    resetsAt: "2026-10-01T00:00:00Z",
    mode: "Test",
  });

  const description = sandboxExhaustedDescription(credit, () => "1 October");
  assert.match(description, /0 of 250 Test credits remain/);
  assert.match(description, /resets on 1 October/);
  assert.doesNotMatch(description, /try again/i);
  assert.match(description, /Resetting the Sandbox still works/);

  assert.equal(readSandboxCreditProblem({ title: "Payment Required" }), null);
  assert.match(sandboxExhaustedDescription(null), /allowance resets or an admin raises it/);
  assert.doesNotMatch(
    sandboxExhaustedDescription({ ...credit, resetsAt: "whenever" }),
    /whenever|Invalid Date/
  );
});

test("a Test mutation tells every credit meter the balance moved", () => {
  let refreshes = 0;
  const unsubscribe = subscribeToSandboxCredit(() => { refreshes += 1; });
  notifySandboxCreditSpent();
  notifySandboxCreditSpent();
  unsubscribe();
  notifySandboxCreditSpent();
  assert.equal(refreshes, 2);
});
