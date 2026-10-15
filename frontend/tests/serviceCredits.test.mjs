import assert from "node:assert/strict";
import test from "node:test";

import {
  creditSentence,
  creditTone,
  formatCreditAmount,
  hasCreditNote,
  issueRefusal,
  metTheTarget,
  monthLabel,
  newestMonthFirst,
  paperworkSentence,
  toCurrencyCode,
} from "../app/lib/serviceCredits.ts";

import {
  formatDuration,
  incidentDurationMs,
  incidentRefusal,
  latestUpdate,
  reviewContentRefusal,
  reviewRefusal,
  timelineNewestFirst,
  updateRefusal,
} from "../app/lib/incidents.ts";

const NOW = new Date("2026-06-15T12:00:00Z");

const credit = (overrides = {}) => ({
  serviceCreditId: "sc",
  organizationId: "org",
  availabilityCommitmentId: "ac",
  month: 3,
  year: 2026,
  availabilityTarget: 99.9,
  measuredUptimePercentage: 99.95,
  totalRequests: 100000,
  failedRequests: 50,
  creditPercentage: 0,
  amount: 0,
  currency: "Gbp",
  invoiceId: "inv",
  creditNoteId: null,
  createdAt: "2026-04-01T00:00:00Z",
  updatedAt: "2026-04-01T00:00:00Z",
  ...overrides,
});

test("this endpoint's currency spelling is normalised before it reaches Intl", () => {
  assert.equal(toCurrencyCode("Gbp"), "GBP");
  assert.equal(toCurrencyCode("Eur"), "EUR");
  assert.equal(toCurrencyCode("Usd"), "USD");
});

test("an unrecognised currency degrades rather than throwing", () => {
  assert.equal(toCurrencyCode("Jpy"), "GBP");
});

test("formatting an amount does not throw on the wire spelling", () => {
  assert.doesNotThrow(() =>
    formatCreditAmount(credit({ amount: 120.5, currency: "Gbp" }))
  );
  assert.match(formatCreditAmount(credit({ amount: 120.5 })), /120\.50/);
});

test("zero credit is a month that met its target, not an empty row", () => {
  assert.equal(metTheTarget(credit({ creditPercentage: 0 })), true);
  assert.equal(metTheTarget(credit({ creditPercentage: 10 })), false);
});

test("a met month gets a sentence of its own", () => {
  const sentence = creditSentence(credit());

  assert.match(sentence, /Met — nothing is owed/);
});

test("a missed month states the percentage and the money", () => {
  const sentence = creditSentence(
    credit({
      creditPercentage: 10,
      measuredUptimePercentage: 99.5,
      amount: 120,
    })
  );

  assert.match(sentence, /10% of the month's net charge credited/);
  assert.match(sentence, /£120\.00/);
});

test("the missed month is the one that takes the loud chip", () => {
  assert.equal(creditTone(credit()), "settled");
  assert.equal(creditTone(credit({ creditPercentage: 25 })), "critical");
});

test("months are named rather than numbered", () => {
  assert.equal(monthLabel(credit()), "March 2026");
  assert.equal(monthLabel(credit({ month: 13 })), "13/2026");
});

test("months are listed newest first", () => {
  const rows = [
    credit({ month: 1, year: 2026 }),
    credit({ month: 12, year: 2025 }),
    credit({ month: 3, year: 2026 }),
  ];

  assert.deepEqual(
    newestMonthFirst(rows).map((row) => monthLabel(row)),
    ["March 2026", "January 2026", "December 2025"]
  );
});

test("the paperwork on a row is never a blank cell", () => {
  assert.match(paperworkSentence(credit()), /no credit note was raised/);
  assert.match(
    paperworkSentence(credit({ creditPercentage: 10, creditNoteId: "cn" })),
    /money has been given back/
  );
  assert.match(
    paperworkSentence(credit({ creditPercentage: 10 })),
    /has not been given back/
  );
  assert.match(
    paperworkSentence(credit({ creditPercentage: 10, invoiceId: null })),
    /nothing to work a credit off/
  );
});

test("a credit note is only claimed where one exists", () => {
  assert.equal(hasCreditNote(credit()), false);
  assert.equal(hasCreditNote(credit({ creditNoteId: "cn" })), true);
});

test("a month still running cannot be measured", () => {
  assert.match(
    issueRefusal({ month: "6", year: "2026" }, NOW),
    /not over yet/
  );
  assert.equal(issueRefusal({ month: "5", year: "2026" }, NOW), null);
});

test("a month outside 1-12 is refused", () => {
  assert.match(issueRefusal({ month: "0", year: "2026" }, NOW), /1 to 12/);
  assert.match(issueRefusal({ month: "13", year: "2026" }, NOW), /1 to 12/);
});

const incident = (overrides = {}) => ({
  incidentId: "inc",
  incidentNumber: "INC0000000012",
  title: "Elevated error rates",
  summary: "Some requests are failing.",
  status: "Investigating",
  impact: "Major",
  components: ["Api"],
  startedAt: "2026-06-15T10:00:00Z",
  resolvedAt: null,
  rootCause: null,
  remediation: null,
  reviewPublishedAt: null,
  updates: [],
  createdAt: "2026-06-15T10:05:00Z",
  updatedAt: "2026-06-15T10:05:00Z",
  ...overrides,
});

test("duration is measured from when customers were affected, not from creation", () => {
  // startedAt 10:00, now 12:00. Creation at 10:05 is not the answer.
  assert.equal(incidentDurationMs(incident(), NOW), 2 * 3600 * 1000);
  assert.equal(formatDuration(incidentDurationMs(incident(), NOW)), "2h");
});

test("a resolved incident stops counting at its resolution", () => {
  const resolved = incident({ resolvedAt: "2026-06-15T10:30:00Z" });

  assert.equal(formatDuration(incidentDurationMs(resolved, NOW)), "30m");
});

test("the timeline reads newest first", () => {
  const withUpdates = incident({
    updates: [
      { incidentUpdateId: "1", status: "Investigating", body: "a", postedAt: "2026-06-15T10:05:00Z" },
      { incidentUpdateId: "2", status: "Identified", body: "b", postedAt: "2026-06-15T11:00:00Z" },
    ],
  });

  assert.equal(timelineNewestFirst(withUpdates.updates)[0].incidentUpdateId, "2");
  assert.equal(latestUpdate(withUpdates).incidentUpdateId, "2");
});

test("an incident against no component would be invisible, so it is refused", () => {
  assert.match(
    incidentRefusal({
      title: "Something",
      summary: "Something happened.",
      components: [],
    }),
    /an incident against nothing changes nothing on the public page/
  );
});

test("an empty update is refused, because it mails every subscriber", () => {
  assert.match(updateRefusal("  "), /tells them nothing/);
  assert.equal(updateRefusal("We have identified the cause."), null);
});

test("a review needs both halves, and each refusal says why", () => {
  assert.match(
    reviewContentRefusal({ rootCause: "", remediation: "x" }),
    /is an apology/
  );
  assert.match(
    reviewContentRefusal({ rootCause: "x", remediation: "" }),
    /promise that nothing will change/
  );
  assert.equal(
    reviewContentRefusal({ rootCause: "x", remediation: "y" }),
    null
  );
});

test("a review of an open incident is refused as a guess", () => {
  assert.match(reviewRefusal(incident()), /is a guess/);
  assert.equal(reviewRefusal(incident({ status: "Resolved" })), null);
});
