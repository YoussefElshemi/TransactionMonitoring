import assert from "node:assert/strict";
import test from "node:test";

import {
  availabilityQuery,
  availabilitySentence,
  bandFor,
  bandsRefusal,
  bandsWorstFirst,
  commitmentBody,
  commitmentRefusal,
  formatUptime,
  isThinlyMeasured,
  latencySentence,
  percentileSentence,
  periodRefusal,
  shortfall,
  termState,
  verdictOf,
  verdictTone,
  VERDICT_LABELS,
} from "../app/lib/availability.ts";

const measurement = (overrides = {}) => ({
  organizationId: "org",
  periodStart: "2026-06-01T00:00:00Z",
  periodEnd: "2026-06-30T23:59:59Z",
  totalRequests: 128400,
  failedRequests: 12,
  uptimePercentage: 99.9906,
  meanLatencyMilliseconds: 38,
  percentileLatencyMilliseconds: 250,
  percentile: 99,
  maxLatencyMilliseconds: 2310,
  availabilityTarget: 99.9,
  latencyTargetMilliseconds: 500,
  metAvailabilityTarget: true,
  metLatencyTarget: true,
  ...overrides,
});

test("no target agreed is its own verdict, not a miss", () => {
  assert.equal(verdictOf(null), "none");
  assert.equal(verdictOf(false), "missed");
  assert.equal(verdictOf(true), "met");
});

test("the no-target verdict reads as no target and is styled inert", () => {
  assert.equal(VERDICT_LABELS.none, "No target agreed");
  assert.equal(verdictTone("none"), "inert");
  assert.equal(verdictTone("missed"), "critical");
  assert.equal(verdictTone("met"), "settled");
});

test("a measurement with no target is described as a measurement, not a verdict", () => {
  const sentence = availabilitySentence(
    measurement({
      availabilityTarget: null,
      metAvailabilityTarget: null,
    })
  );

  assert.match(sentence, /No target was agreed/);
  assert.match(sentence, /measurement rather than a verdict/);
  assert.doesNotMatch(sentence, /missed|short by/i);
});

test("a month that fell short says by how much", () => {
  const missed = measurement({
    uptimePercentage: 99.5,
    metAvailabilityTarget: false,
  });

  assert.equal(shortfall(missed), 99.9 - 99.5);
  assert.match(availabilitySentence(missed), /short by/);
});

test("a month with no target has no shortfall, rather than a shortfall of zero", () => {
  assert.equal(shortfall(measurement({ availabilityTarget: null })), null);
});

test("the percentile is stated as a bound, never as an exact figure", () => {
  const sentence = percentileSentence(measurement());

  assert.equal(sentence, "p99 under 250 ms");
  assert.doesNotMatch(sentence, /=/);
});

test("latency with no target agreed does not report a verdict", () => {
  const sentence = latencySentence(
    measurement({ latencyTargetMilliseconds: null, metLatencyTarget: null })
  );

  assert.match(sentence, /No latency target was agreed/);
  assert.doesNotMatch(sentence, /\bmet\b|\bmissed\b/);
});

test("an empty period is the default and is allowed", () => {
  assert.equal(periodRefusal({ fromDate: "", toDate: "" }), null);
  assert.equal(availabilityQuery({ fromDate: "", toDate: "" }), "");
});

test("half a period waits for its second DateOnly bound", () => {
  const refusal = periodRefusal({
    fromDate: "2026-06-01",
    toDate: "",
  });

  assert.match(refusal, /both ends or neither/i);
  assert.equal(
    availabilityQuery({ fromDate: "2026-06-01", toDate: "" }),
    ""
  );
});

test("a period that ends before it starts is refused", () => {
  assert.match(
    periodRefusal({ fromDate: "2026-06-30", toDate: "2026-06-01" }),
    /at or after the start date/
  );
});

test("availability bounds reject timestamps", () => {
  const period = {
    fromDate: "2026-06-01T00:00:00Z",
    toDate: "2026-06-30T23:59:59Z",
  };

  assert.match(periodRefusal(period), /YYYY-MM-DD/);
  assert.equal(availabilityQuery(period), "");
});

test("a whole period becomes DateOnly query parameters", () => {
  const query = availabilityQuery({
    fromDate: "2026-06-01",
    toDate: "2026-06-30",
  });

  assert.equal(query, "?fromDate=2026-06-01&toDate=2026-06-30");
  assert.doesNotMatch(query, /T00:00:00/);
});

const bands = [
  { belowUptimePercentage: 99.9, creditPercentage: 10 },
  { belowUptimePercentage: 99, creditPercentage: 25 },
  { belowUptimePercentage: 95, creditPercentage: 50 },
];

test("a month owes the deepest band it fell below, not the first one matched", () => {
  // 94 is below all three. The answer is 50, not 10.
  assert.equal(bandFor(bands, 94).creditPercentage, 50);
  assert.equal(bandFor(bands, 99.5).creditPercentage, 10);
});

test("a threshold is exclusive, so a month exactly on it owes nothing", () => {
  assert.equal(bandFor(bands, 99.9), null);
  assert.equal(bandFor(bands, 99.8999).creditPercentage, 10);
});

test("a month that met every band owes nothing", () => {
  assert.equal(bandFor(bands, 100), null);
});

test("bands are read as a ladder, worst first, whatever order they arrive in", () => {
  const sorted = bandsWorstFirst(bands);

  assert.deepEqual(
    sorted.map((band) => band.belowUptimePercentage),
    [95, 99, 99.9]
  );
});

test("an empty credit schedule is legal — a target with no money attached", () => {
  assert.equal(bandsRefusal([]), null);
});

test("a band missing either half is refused", () => {
  assert.match(
    bandsRefusal([{ belowUptimePercentage: "", creditPercentage: "10" }]),
    /needs an uptime threshold/
  );
  assert.match(
    bandsRefusal([{ belowUptimePercentage: "99", creditPercentage: "" }]),
    /needs a credit percentage/
  );
});

test("two bands at the same threshold are refused, since only one could apply", () => {
  assert.match(
    bandsRefusal([
      { belowUptimePercentage: "99.9", creditPercentage: "10" },
      { belowUptimePercentage: "99.9", creditPercentage: "25" },
    ]),
    /Only one of them could ever decide/
  );
});

test("a band crediting nothing is allowed, and is not read as an empty box", () => {
  assert.equal(
    bandsRefusal([{ belowUptimePercentage: "99.9", creditPercentage: "0" }]),
    null
  );
});

const draft = (overrides = {}) => ({
  availabilityTarget: "99.9",
  latencyTargetMilliseconds: "500",
  latencyPercentile: "99",
  effectiveFrom: "",
  effectiveTo: "",
  creditBands: [],
  ...overrides,
});

test("a valid commitment is accepted", () => {
  assert.equal(commitmentRefusal(draft()), null);
});

test("a target given as a fraction is caught, with the mistake named", () => {
  const refusal = commitmentRefusal(draft({ availabilityTarget: "120" }));

  assert.match(refusal, /percentage and not a fraction/);
});

test("a latency target typed in seconds is caught by the upper bound", () => {
  const refusal = commitmentRefusal(
    draft({ latencyTargetMilliseconds: "900000" })
  );

  assert.match(refusal, /this field is in milliseconds/);
});

test("the mean is not a percentile", () => {
  assert.match(
    commitmentRefusal(draft({ latencyPercentile: "0" })),
    /between 1 and 100/
  );
});

test("a term that ends before it starts is refused", () => {
  assert.match(
    commitmentRefusal(
      draft({
        effectiveFrom: "2026-06-01T00:00",
        effectiveTo: "2026-05-01T00:00",
      })
    ),
    /at or before it starts/
  );
});

test("omitted dates are omitted from the body rather than sent as null", () => {
  const body = commitmentBody(draft());

  assert.equal("effectiveFrom" in body, false);
  assert.equal("effectiveTo" in body, false);
  assert.equal(body.availabilityTarget, 99.9);
  assert.equal(body.latencyPercentile, 99);
});

test("the body sorts the bands into ladder order", () => {
  const body = commitmentBody(
    draft({
      creditBands: [
        { belowUptimePercentage: "99.9", creditPercentage: "10" },
        { belowUptimePercentage: "95", creditPercentage: "50" },
      ],
    })
  );

  assert.deepEqual(
    body.creditBands.map((band) => band.belowUptimePercentage),
    [95, 99.9]
  );
});

const NOW = new Date("2026-06-15T12:00:00Z");

test("a term is scheduled, current or superseded", () => {
  assert.equal(
    termState(
      { effectiveFrom: "2026-07-01T00:00:00Z", effectiveTo: null },
      NOW
    ),
    "scheduled"
  );
  assert.equal(
    termState(
      { effectiveFrom: "2026-01-01T00:00:00Z", effectiveTo: null },
      NOW
    ),
    "current"
  );
  assert.equal(
    termState(
      {
        effectiveFrom: "2026-01-01T00:00:00Z",
        effectiveTo: "2026-05-01T00:00:00Z",
      },
      NOW
    ),
    "superseded"
  );
});

test("a month with almost no traffic is flagged as indicative", () => {
  assert.equal(isThinlyMeasured(measurement({ totalRequests: 11 })), true);
  assert.equal(isThinlyMeasured(measurement({ totalRequests: 128400 })), false);
  assert.equal(isThinlyMeasured(measurement({ totalRequests: 0 })), false);
});

test("uptime keeps four decimals here too, because money depends on it", () => {
  assert.equal(formatUptime(99.9906), "99.9906%");
  assert.equal(formatUptime(100), "100%");
});
