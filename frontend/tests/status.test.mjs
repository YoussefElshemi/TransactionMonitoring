import assert from "node:assert/strict";
import test from "node:test";

import {
  componentsSentence,
  formatDowntime,
  formatLatency,
  formatUptime,
  hasNoProbeData,
  isFailing,
  isPlausibleEmail,
  overallDetail,
  overallHeadline,
  probedDayCount,
  readUptimeDay,
  uptimeCoverageNote,
  uptimeDayTitle,
  uptimeWindow,
} from "../app/lib/status.ts";

const day = (overrides = {}) => ({
  date: "2026-06-10",
  totalProbes: 1440,
  healthyProbes: 1440,
  downtimeSeconds: 0,
  uptimePercentage: 100,
  ...overrides,
});

test("a day with no probes reads as unknown, not as a perfect day", () => {
  const unprobed = day({
    totalProbes: 0,
    healthyProbes: 0,
    uptimePercentage: 100,
  });

  assert.equal(hasNoProbeData(unprobed), true);
  assert.equal(readUptimeDay(unprobed), "unknown");

  assert.equal(readUptimeDay(day()), "up");
});

test("the tooltip for an unprobed day says it is not being reported as up", () => {
  const title = uptimeDayTitle(day({ totalProbes: 0, healthyProbes: 0 }));

  assert.match(title, /no data/i);
  assert.match(title, /not being reported as up/i);
  assert.doesNotMatch(title, /100%/);
});

test("a probed day quotes its figures", () => {
  const title = uptimeDayTitle(
    day({ totalProbes: 1440, healthyProbes: 1400, uptimePercentage: 97.2222, downtimeSeconds: 2400 })
  );

  assert.match(title, /97\.2222%/);
  assert.match(title, /1,400 of 1,440 probes healthy/);
  assert.match(title, /40 minutes down/);
});

test("readings are banded by severity", () => {
  assert.equal(readUptimeDay(day({ uptimePercentage: 99.5 })), "up");
  assert.equal(readUptimeDay(day({ uptimePercentage: 98 })), "degraded");
  assert.equal(readUptimeDay(day({ uptimePercentage: 80 })), "down");
});

test("a short history is padded at the front, not stretched", () => {
  const history = [day({ date: "2026-06-09" }), day({ date: "2026-06-10" })];
  const window = uptimeWindow(history, 90);

  assert.equal(window.length, 90);
  assert.equal(window[0], null);
  assert.equal(window[87], null);
  assert.equal(window[88].date, "2026-06-09");
  assert.equal(window[89].date, "2026-06-10");
});

test("a long history is trimmed to the most recent days", () => {
  const history = Array.from({ length: 120 }, (_, index) =>
    day({ date: `day-${index}` })
  );

  const window = uptimeWindow(history, 90);

  assert.equal(window.length, 90);
  assert.equal(window[0].date, "day-30");
  assert.equal(window[89].date, "day-119");
});

test("coverage counts only the days that were actually probed", () => {
  const history = [
    day(),
    day({ totalProbes: 0, healthyProbes: 0 }),
    day({ totalProbes: 0, healthyProbes: 0 }),
  ];

  assert.equal(probedDayCount(history), 1);
});

test("a full window of probed days carries no coverage caveat", () => {
  const history = Array.from({ length: 90 }, () => day());

  assert.equal(uptimeCoverageNote(history, 90), null);
});

test("a gappy window says the gaps are gaps rather than outages", () => {
  const history = [
    ...Array.from({ length: 10 }, () => day()),
    ...Array.from({ length: 5 }, () => day({ totalProbes: 0, healthyProbes: 0 })),
  ];

  const note = uptimeCoverageNote(history, 90);

  assert.match(note, /10 of the last 90 days were probed/);
  assert.match(note, /not outages/);
});

test("a window with nothing probed says the bar is empty because nothing is known", () => {
  const history = Array.from({ length: 90 }, () =>
    day({ totalProbes: 0, healthyProbes: 0 })
  );

  assert.match(uptimeCoverageNote(history, 90), /nothing is known/);
});

const health = (component, status) => ({
  component,
  status,
  lastProbedAt: null,
  latencyMilliseconds: null,
  uptimePercentage: 100,
  history: [],
});

test("the banner names the affected components rather than repeating the status", () => {
  const detail = overallDetail({
    status: "PartialOutage",
    observedAt: "2026-06-10T12:00:00Z",
    components: [
      health("Api", "Operational"),
      health("Webhooks", "MajorOutage"),
      health("RuleEvaluation", "Degraded"),
    ],
    activeIncidents: [],
    upcomingMaintenance: [],
  });

  assert.match(detail, /Webhooks and Rule evaluation are affected/);
  assert.match(detail, /Everything else is answering normally/);
});

test("maintenance alone is not described as components being affected", () => {
  const detail = overallDetail({
    status: "UnderMaintenance",
    observedAt: "2026-06-10T12:00:00Z",
    components: [
      health("Api", "Operational"),
      health("Reporting", "UnderMaintenance"),
    ],
    activeIncidents: [],
    upcomingMaintenance: [],
  });

  assert.match(detail, /Reporting is in announced maintenance/);
  assert.doesNotMatch(detail, /Reporting is affected/);
});

test("an overall status no component agrees with is admitted rather than papered over", () => {
  const detail = overallDetail({
    status: "MajorOutage",
    observedAt: "2026-06-10T12:00:00Z",
    components: [health("Api", "Operational")],
    activeIncidents: [],
    upcomingMaintenance: [],
  });

  assert.match(detail, /no individual component is showing/);
});

test("headlines are sentences rather than enum names", () => {
  assert.equal(overallHeadline("PartialOutage"), "Partial outage");
  assert.equal(overallHeadline("Operational"), "All systems operational");
});

test("announced maintenance is not a failure", () => {
  assert.equal(isFailing("UnderMaintenance"), false);
  assert.equal(isFailing("Degraded"), true);
  assert.equal(isFailing("MajorOutage"), true);
});

test("uptime keeps four decimals, because the third nine is the point", () => {
  assert.equal(formatUptime(99.9), "99.9%");
  assert.equal(formatUptime(99.99), "99.99%");
  assert.equal(formatUptime(99.9812), "99.9812%");
});

test("an exact hundred reads as a hundred rather than as 100.0000", () => {
  assert.equal(formatUptime(100), "100%");
});

test("downtime coarsens as it grows", () => {
  assert.equal(formatDowntime(1), "1 second");
  assert.equal(formatDowntime(45), "45 seconds");
  assert.equal(formatDowntime(600), "10 minutes");
  assert.equal(formatDowntime(7200), "2 hours");
});

test("a component that has never been probed says so rather than showing a dash", () => {
  assert.equal(formatLatency(null), "never probed");
  assert.equal(formatLatency(12), "12 ms");
});

test("component lists read as English", () => {
  assert.equal(componentsSentence([]), "no components");
  assert.equal(componentsSentence(["Api"]), "API");
  assert.equal(
    componentsSentence(["Api", "Webhooks", "Reporting"]),
    "API, Webhooks and Reporting"
  );
});

test("the subscribe box catches typos without inventing rules the API does not have", () => {
  assert.equal(isPlausibleEmail("someone@example.com"), true);
  assert.equal(isPlausibleEmail("someone"), false);
  assert.equal(isPlausibleEmail("someone@example"), false);
  assert.equal(isPlausibleEmail("  spaced out@example.com "), false);
});
