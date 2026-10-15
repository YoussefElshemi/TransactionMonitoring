import assert from "node:assert/strict";
import test from "node:test";

import {
  canCancel,
  effectiveFromRefusal,
  inForceSubscription,
  invoiceSnapshotSentence,
  isCurrentVersion,
  isInForce,
  isScheduled,
  isSuperseded,
  isValidCostPerRequest,
  isValidEffectiveFrom,
  isValidPlanName,
  planOwnership,
  scheduledSubscriptions,
  successorOf,
  supersededSubscriptions,
  timelineSentence,
  versionLabel,
  versionsNewestFirst,
} from "../app/lib/plans.ts";

const NOW = new Date("2026-03-15T12:00:00Z");

const version = (versionNumber, costPerRequest) => ({
  planVersionId: `version-${versionNumber}`,
  planId: "plan",
  versionNumber,
  costPerRequest,
  publishedByUserId: null,
  createdAt: "2026-01-01T00:00:00Z",
});

const stretch = (subscriptionId, effectiveFrom, effectiveTo, versionNumber = 1) => ({
  subscriptionId,
  organizationId: "org",
  planVersionId: `version-${versionNumber}`,
  planVersion: version(versionNumber, 0.002),
  planName: "Growth",
  effectiveFrom,
  effectiveTo,
  createdByUserId: null,
  createdAt: "2026-01-01T00:00:00Z",
  updatedAt: "2026-01-01T00:00:00Z",
});

test("a stretch is scheduled, in force or superseded, and never two of them", () => {
  const past = stretch("past", "2026-01-01T00:00:00Z", "2026-02-01T00:00:00Z");
  const open = stretch("open", "2026-02-01T00:00:00Z", null);
  const future = stretch("future", "2026-04-01T00:00:00Z", null);

  assert.deepEqual(
    [isScheduled(past, NOW), isInForce(past, NOW), isSuperseded(past, NOW)],
    [false, false, true]
  );
  assert.deepEqual(
    [isScheduled(open, NOW), isInForce(open, NOW), isSuperseded(open, NOW)],
    [false, true, false]
  );
  assert.deepEqual(
    [isScheduled(future, NOW), isInForce(future, NOW), isSuperseded(future, NOW)],
    [true, false, false]
  );
});

test("a stretch with a scheduled successor is still in force, not over", () => {
  const closing = stretch("closing", "2026-02-01T00:00:00Z", "2026-04-01T00:00:00Z");

  assert.equal(isInForce(closing, NOW), true);
  assert.equal(isSuperseded(closing, NOW), false);
});

test("the boundary between two stretches belongs to the later one", () => {
  const closed = stretch("closed", "2026-01-01T00:00:00Z", NOW.toISOString());
  const opened = stretch("opened", NOW.toISOString(), null);

  assert.equal(isSuperseded(closed, NOW), true);
  assert.equal(isInForce(closed, NOW), false);
  assert.equal(isInForce(opened, NOW), true);
  assert.equal(isScheduled(opened, NOW), false);
});

test("the timeline is sorted into the order each zone reads in", () => {
  const past = stretch("past", "2026-01-01T00:00:00Z", "2026-02-01T00:00:00Z");
  const older = stretch("older", "2025-11-01T00:00:00Z", "2026-01-01T00:00:00Z");
  const open = stretch("open", "2026-02-01T00:00:00Z", "2026-04-01T00:00:00Z");
  const soon = stretch("soon", "2026-04-01T00:00:00Z", "2026-05-01T00:00:00Z");
  const later = stretch("later", "2026-05-01T00:00:00Z", null);

  const all = [later, past, open, older, soon];

  assert.equal(inForceSubscription(all, NOW)?.subscriptionId, "open");
  assert.deepEqual(
    scheduledSubscriptions(all, NOW).map((x) => x.subscriptionId),
    ["soon", "later"]
  );
  assert.deepEqual(
    supersededSubscriptions(all, NOW).map((x) => x.subscriptionId),
    ["past", "older"]
  );
});

test("an organisation on no plan reads as null rather than as an empty list", () => {
  const onlyPast = [
    stretch("past", "2026-01-01T00:00:00Z", "2026-02-01T00:00:00Z"),
  ];

  assert.equal(inForceSubscription(onlyPast, NOW), null);
  assert.equal(inForceSubscription([], NOW), null);
});

test("a subscription may not be backdated, and may not sit under a scheduled change", () => {
  const latestStart = "2026-04-01T00:00:00Z";

  assert.equal(isValidEffectiveFrom("2026-03-01T00:00:00Z", NOW), false);
  assert.equal(isValidEffectiveFrom(NOW.toISOString(), NOW), false);
  assert.equal(isValidEffectiveFrom("2026-03-15T12:00:01Z", NOW), true);

  assert.equal(
    isValidEffectiveFrom("2026-03-20T00:00:00Z", NOW, latestStart),
    false
  );
  assert.equal(isValidEffectiveFrom(latestStart, NOW, latestStart), false);
  assert.equal(
    isValidEffectiveFrom("2026-04-01T00:00:01Z", NOW, latestStart),
    true
  );

  assert.equal(isValidEffectiveFrom("not a date", NOW), false);
});

test("the two backdating refusals say different things, because they want different fixes", () => {
  const past = effectiveFromRefusal("2026-01-01T00:00:00Z", NOW);
  const under = effectiveFromRefusal(
    "2026-03-20T00:00:00Z",
    NOW,
    "2026-04-01T00:00:00Z"
  );

  assert.match(past, /backdated/i);
  assert.match(under, /cancel the scheduled change/i);
  assert.notEqual(past, under);

  assert.equal(effectiveFromRefusal("2026-05-01T00:00:00Z", NOW), null);
});

test("only a stretch that has not started can be cancelled", () => {
  const open = stretch("open", "2026-02-01T00:00:00Z", null);
  const future = stretch("future", "2026-04-01T00:00:00Z", null);
  const past = stretch("past", "2026-01-01T00:00:00Z", "2026-02-01T00:00:00Z");

  assert.equal(canCancel(future, NOW), true);
  assert.equal(canCancel(open, NOW), false);
  assert.equal(canCancel(past, NOW), false);
});

test("a stretch reads as prose, and names what takes over from it", () => {
  const open = stretch("open", "2026-02-01T00:00:00Z", null, 2);
  const closing = stretch("closing", "2026-02-01T00:00:00Z", "2026-04-01T00:00:00Z", 2);
  const successor = stretch("successor", "2026-04-01T00:00:00Z", null, 3);

  assert.equal(timelineSentence(open), "Growth v2 from 1 Feb 2026, ongoing");
  assert.equal(
    timelineSentence(closing),
    "Growth v2 from 1 Feb 2026, until 1 Apr 2026"
  );
  assert.equal(
    timelineSentence(closing, successor),
    "Growth v2 from 1 Feb 2026, until 1 Apr 2026, when Growth v3 starts"
  );
});

test("the successor is the stretch that opens at the instant this one closes", () => {
  const closing = stretch("closing", "2026-02-01T00:00:00Z", "2026-04-01T00:00:00Z");
  const successor = stretch("successor", "2026-04-01T00:00:00Z", null);
  const unrelated = stretch("unrelated", "2026-06-01T00:00:00Z", null);

  assert.equal(
    successorOf(closing, [closing, successor, unrelated])?.subscriptionId,
    "successor"
  );
  assert.equal(successorOf(closing, [closing, unrelated]), null);
  assert.equal(successorOf(successor, [closing, successor]), null);
});

test("a plan is catalogue or bespoke, and the field that decides is nullable", () => {
  assert.equal(planOwnership({ organizationId: null }), "catalogue");
  assert.equal(planOwnership({ organizationId: "org" }), "bespoke");
});

test("versions accumulate newest first, and only the highest is current", () => {
  const plan = { currentVersionNumber: 3 };
  const versions = [version(1, 0.005), version(3, 0.003), version(2, 0.004)];

  assert.deepEqual(
    versionsNewestFirst(versions).map((x) => x.versionNumber),
    [3, 2, 1]
  );
  assert.equal(isCurrentVersion(plan, version(3, 0.003)), true);
  assert.equal(isCurrentVersion(plan, version(2, 0.004)), false);

  assert.deepEqual(
    versions.map((x) => x.versionNumber),
    [1, 3, 2]
  );
});

test("a name and a rate are checked against the bounds the API enforces", () => {
  assert.equal(isValidPlanName("Growth"), true);
  assert.equal(isValidPlanName("   "), false);
  assert.equal(isValidPlanName("x".repeat(201)), false);

  assert.equal(isValidCostPerRequest(0), true);
  assert.equal(isValidCostPerRequest(-0.001), false);
  assert.equal(isValidCostPerRequest(Number.NaN), false);
});

test("a version label survives either half being missing", () => {
  assert.equal(versionLabel("Growth", 2), "Growth v2");
  assert.equal(versionLabel("Growth", null), "Growth");
  assert.equal(versionLabel(null, 2), "Unknown plan v2");
  assert.equal(versionLabel(null, null), "Unknown plan");
});

test("an invoice predating plans says so instead of showing an empty version", () => {
  const priced = invoiceSnapshotSentence({
    planVersionId: "version-2",
    planVersionNumber: 2,
    costPerRequest: 0.002,
    planCurrency: "GBP",
  });
  const legacy = invoiceSnapshotSentence({
    planVersionId: null,
    planVersionNumber: null,
    costPerRequest: 0.002,
    planCurrency: "GBP",
  });

  assert.match(priced, /plan version 2/);
  assert.match(legacy, /predates plans/);
  assert.match(priced, /per request/);
  assert.match(legacy, /per request/);
});
