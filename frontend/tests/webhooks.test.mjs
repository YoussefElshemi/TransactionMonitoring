import assert from "node:assert/strict";
import test from "node:test";

import {
  DELIVERY_OUTCOME_LABELS,
  EVENT_CATEGORY_ORDER,
  deliveryOutcome,
  deliveryOutcomeTone,
  disableState,
  disableStateTone,
  eventsPatchValue,
  formatLatency,
  groupEventsByCategory,
  healthTone,
  notSubscribableReason,
  parsePayload,
  replayBatchSentence,
  replayWindowRefusal,
  responseStatusLabel,
  responseStatusTone,
  subscribesToEverything,
  subscriptionSummary,
  wideningWarning,
  windowLength,
} from "../app/lib/webhooks.ts";

const definition = (event, category, isSubscribable = true) => ({
  event,
  category,
  description: `${event} happened`,
  isSubscribable,
  introducedInVersion: 1,
});

test("the catalogue is grouped in reading order rather than alphabetically", () => {
  const groups = groupEventsByCategory([
    definition("alert.raised", "Alert"),
    definition("case.created", "Case"),
    definition("approval.requested", "Approval"),
  ]);

  assert.deepEqual(
    groups.map((group) => group.category),
    ["Case", "Alert", "Approval"]
  );
});

test("a category this build has not heard of is appended, never dropped", () => {
  const groups = groupEventsByCategory([
    definition("case.created", "Case"),
    definition("sanction.matched", "Sanction"),
  ]);

  assert.deepEqual(
    groups.map((group) => group.category),
    ["Case", "Sanction"]
  );
  assert.equal(groups[1].label, "Sanction");
  assert.equal(groups[1].events.length, 1);
});

test("every published category has a place in the order", () => {
  assert.equal(new Set(EVENT_CATEGORY_ORDER).size, EVENT_CATEGORY_ORDER.length);
});

test("the one entry that cannot be subscribed to says why rather than vanishing", () => {
  assert.equal(notSubscribableReason(definition("case.created", "Case")), null);

  const refusal = notSubscribableReason(
    definition("test", "Diagnostic", false)
  );
  assert.ok(refusal);
  assert.match(refusal, /Test/i);
});

test("an empty subscription is every event, not no events", () => {
  assert.equal(subscribesToEverything({ events: [] }), true);
  assert.equal(subscriptionSummary({ events: [] }), "All events");
  assert.equal(subscriptionSummary({ events: [] }, 20), "All events");
});

test("a narrowed subscription is counted against the catalogue", () => {
  assert.equal(
    subscriptionSummary({ events: ["case.created", "case.resolved"] }, 20),
    "2 of 20 events"
  );
  assert.equal(subscriptionSummary({ events: ["case.created"] }), "1 event");
});

test("clearing the last event is warned about, because it widens rather than empties", () => {
  assert.equal(wideningWarning(["case.created"], []) !== null, true);
  assert.equal(wideningWarning([], []), null);
  assert.equal(wideningWarning(["case.created", "case.resolved"], ["case.created"]), null);
});

test("an untouched picker sends no events key at all", () => {
  assert.equal(eventsPatchValue(["case.created"], ["case.created"]), undefined);
  assert.equal(eventsPatchValue([], []), undefined);
});

test("reordering the same events is not an edit", () => {
  assert.equal(
    eventsPatchValue(["a", "b"], ["b", "a"]),
    undefined
  );
});

test("clearing the selection sends an empty array, which is a real request", () => {
  assert.deepEqual(eventsPatchValue(["case.created"], []), []);
});

test("adding or removing an event sends the whole new list", () => {
  assert.deepEqual(eventsPatchValue(["a"], ["a", "b"]), ["a", "b"]);
  assert.deepEqual(eventsPatchValue(["a", "b"], ["b"]), ["b"]);
});

test("the patch value is a copy, so a later edit cannot mutate a sent body", () => {
  const next = ["a"];
  const value = eventsPatchValue([], next);
  next.push("b");
  assert.deepEqual(value, ["a"]);
});

test("a paused endpoint and one the platform switched off are different states", () => {
  assert.equal(disableState({ isEnabled: true, disabledAt: null }), "enabled");
  assert.equal(disableState({ isEnabled: false, disabledAt: null }), "paused");
  assert.equal(
    disableState({ isEnabled: false, disabledAt: "2026-09-01T00:00:00Z" }),
    "auto-disabled"
  );
});

test("only the automatic switch-off takes the failure colour", () => {
  assert.equal(disableStateTone("enabled"), "settled");
  assert.equal(disableStateTone("paused"), "inert");
  assert.equal(disableStateTone("auto-disabled"), "critical");
});

const health = (overrides = {}) => ({
  isEnabled: true,
  consecutiveFailures: 0,
  lastSucceededAt: "2026-09-01T00:00:00Z",
  lastFailedAt: null,
  disabledAt: null,
  disabledReason: null,
  from: "2026-09-01T00:00:00Z",
  to: "2026-09-08T00:00:00Z",
  delivered: 10,
  failed: 0,
  pending: 0,
  ...overrides,
});

test("an endpoint still on but failing is flagged before it is switched off", () => {
  assert.equal(healthTone(health()), "settled");
  assert.equal(healthTone(health({ consecutiveFailures: 4 })), "attention");
  assert.equal(healthTone(health({ isEnabled: false })), "inert");
  assert.equal(
    healthTone(
      health({ isEnabled: false, disabledAt: "2026-09-02T00:00:00Z" })
    ),
    "critical"
  );
});

test("the tally window is named rather than assumed", () => {
  assert.equal(windowLength({ from: "2026-09-01T00:00:00Z", to: "2026-09-08T00:00:00Z" }), "7 days");
  assert.equal(windowLength({ from: "2026-09-08T00:00:00Z", to: "2026-09-08T06:00:00Z" }), "6 hours");
  assert.equal(windowLength({ from: "2026-09-08T00:00:00Z", to: "2026-09-01T00:00:00Z" }), "the window");
});

test("a suppressed row is not read as a failure", () => {
  assert.equal(
    deliveryOutcome({ isDelivered: false, suppressedAt: "2026-09-01T00:00:00Z" }),
    "suppressed"
  );
  assert.equal(
    deliveryOutcome({ isDelivered: true, suppressedAt: "2026-09-01T00:00:00Z" }),
    "suppressed"
  );
});

test("undelivered is not claimed to be failed", () => {
  assert.equal(deliveryOutcome({ isDelivered: false, suppressedAt: null }), "undelivered");
  assert.equal(DELIVERY_OUTCOME_LABELS.undelivered, "Not delivered");
  assert.equal(deliveryOutcomeTone("undelivered"), "attention");
  assert.equal(deliveryOutcomeTone("delivered"), "settled");
  assert.equal(deliveryOutcomeTone("suppressed"), "inert");
});

test("a request that never got an answer is never rendered as 0 or as a success", () => {
  assert.equal(responseStatusLabel({ responseStatusCode: null }), "No answer");
  assert.equal(responseStatusTone({ responseStatusCode: null }), "critical");

  assert.equal(responseStatusLabel({ responseStatusCode: 204 }), "204");
  assert.equal(responseStatusTone({ responseStatusCode: 204 }), "settled");
  assert.equal(responseStatusTone({ responseStatusCode: 404 }), "attention");
  assert.equal(responseStatusTone({ responseStatusCode: 503 }), "critical");
});

test("latency is said in the unit that reads, and absence is not zero", () => {
  assert.equal(formatLatency(412), "412 ms");
  assert.equal(formatLatency(1400), "1.4 s");
  assert.equal(formatLatency(30000), "30 s");
  assert.equal(formatLatency(null), "—");
  assert.equal(formatLatency(undefined), "—");
});

test("a payload that will not parse is handed back rather than thrown away", () => {
  assert.deepEqual(parsePayload('{"event":"case.created"}'), {
    event: "case.created",
  });
  assert.equal(parsePayload("not json at all"), null);
});

test("an unbounded replay window is refused", () => {
  assert.ok(replayWindowRefusal({ from: "", to: "2026-09-08T00:00" }));
  assert.ok(replayWindowRefusal({ from: "2026-09-08T00:00", to: "" }));
  assert.ok(
    replayWindowRefusal({ from: "2026-09-08T00:00", to: "2026-09-01T00:00" })
  );
  assert.equal(
    replayWindowRefusal({ from: "2026-09-01T00:00", to: "2026-09-08T00:00" }),
    null
  );
});

test("a batch cut short by the ceiling says so", () => {
  const sentence = replayBatchSentence({
    matched: 500,
    queued: 500,
    hasMore: true,
  });

  assert.match(sentence, /500/);
  assert.match(sentence, /again/i);
});

test("a batch that matched nothing does not report a success", () => {
  const sentence = replayBatchSentence({ matched: 0, queued: 0, hasMore: false });
  assert.match(sentence, /Nothing/i);
});

test("a whole window queued says the window is queued", () => {
  const sentence = replayBatchSentence({ matched: 12, queued: 12, hasMore: false });
  assert.match(sentence, /12 deliveries matched, 12 queued/);
  assert.match(sentence, /whole window/i);
});
