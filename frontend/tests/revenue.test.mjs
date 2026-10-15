import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync, readdirSync } from "node:fs";

import {
  CONCENTRATION_READING_LABELS,
  HEALTH_BANDS,
  HEALTH_BAND_HINTS,
  HEALTH_BAND_LABELS,
  MOVEMENT_APPEARANCE,
  MOVEMENT_HINTS,
  MOVEMENT_LABELS,
  MOVEMENT_ORDER,
  NO_ANSWER,
  NO_ANSWER_REASONS,
  RESOURCE_LABELS,
  SIGNAL_LABELS,
  answered,
  clampTopCount,
  clampWindowDays,
  cohortIsStillMaturing,
  concentrationReading,
  concentrationTone,
  equivalentCustomers,
  formatRevenue,
  formatSignedRevenue,
  healthBandTone,
  impliedClosing,
  involuntaryShareOfChurn,
  isForwardRange,
  isValidUnitCost,
  marginIsOverstated,
  marginRateAnswer,
  marginTone,
  monthsInRange,
  movementExtent,
  movementReconciles,
  movementResidual,
  movementSteps,
  recognitionFileName,
  recoveryStageRows,
  resourceLabelKey,
  signalSentence,
  signalTone,
  signalsInReadingOrder,
  uncostedLines,
  uncostedResources,
  usageTrendAnswer,
} from "../app/lib/revenue.ts";

import { DUNNING_STAGES, DUNNING_STAGE_LABELS } from "../app/lib/dunning.ts";
import { BILLABLE_RESOURCE_ORDER } from "../app/lib/pricing.ts";

const NOW = new Date("2026-06-15T12:00:00Z");

const CATALOGUES = Object.fromEntries(
  readdirSync(new URL("../app/messages/", import.meta.url))
    .filter((name) => name.endsWith(".json"))
    .map((name) => [
      name.replace(".json", ""),
      JSON.parse(
        readFileSync(new URL("../app/messages/" + name, import.meta.url), "utf8")
      ),
    ])
);

const LANGUAGES = Object.keys(CATALOGUES);

const month = (overrides = {}) => ({
  month: 5,
  year: 2026,
  currency: "GBP",
  openingRevenue: 100_000,
  newRevenue: 12_000,
  reactivationRevenue: 3_000,
  expansionRevenue: 8_000,
  contractionRevenue: 5_000,
  churnedRevenue: 9_000,
  involuntaryChurnedRevenue: 4_000,
  closingRevenue: 109_000,
  openingCustomers: 210,
  newCustomers: 14,
  reactivatedCustomers: 3,
  churnedCustomers: 11,
  involuntarilyChurnedCustomers: 5,
  closingCustomers: 216,
  ...overrides,
});

test("the waterfall's own bars add up to the closing balance", () => {
  const steps = movementSteps(month());
  const movements = steps.filter((step) => !step.isTotal);

  const walked = movements.reduce(
    (running, step) => running + step.amount,
    steps[0].amount
  );

  assert.equal(walked, month().closingRevenue);
  assert.equal(movements[movements.length - 1].to, month().closingRevenue);
});

test("the running balance is continuous: each bar starts where the last one ended", () => {
  const movements = movementSteps(month()).filter((step) => !step.isTotal);

  for (let index = 1; index < movements.length; index++) {
    assert.equal(
      movements[index].from,
      movements[index - 1].to,
      `${movements[index].kind} does not start where ${movements[index - 1].kind} ended`
    );
  }
});

test("contraction and churn are negated exactly once", () => {
  const steps = movementSteps(month());
  const by = Object.fromEntries(steps.map((step) => [step.kind, step]));

  assert.equal(by.contraction.amount, -5_000);
  assert.equal(by.churn.amount, -9_000);
  assert.equal(by.contraction.direction, "down");
  assert.equal(by.churn.direction, "down");
  assert.equal(by.new.direction, "up");
});

test("the identity is reported as broken when the payload does not reconcile", () => {
  const broken = month({ closingRevenue: 111_000 });

  assert.equal(movementReconciles(broken), false);
  assert.equal(movementResidual(broken), 2_000);
  assert.equal(impliedClosing(broken), 109_000);
});

test("floating point noise is not reported as a book that does not balance", () => {
  const noisy = month({ closingRevenue: 109_000 + 1e-9 });

  assert.equal(movementReconciles(noisy), true);
});

test("a month where expansion and contraction cancel still has both bars", () => {
  const flat = month({
    expansionRevenue: 4_000,
    contractionRevenue: 4_000,
    newRevenue: 0,
    reactivationRevenue: 0,
    churnedRevenue: 0,
    involuntaryChurnedRevenue: 0,
    closingRevenue: 100_000,
  });

  const kinds = movementSteps(flat).map((step) => step.kind);

  assert.deepEqual(kinds, MOVEMENT_ORDER);
  assert.equal(movementSteps(flat).length, 7);
});

test("a completely still month still renders all seven columns", () => {
  const still = month({
    newRevenue: 0,
    reactivationRevenue: 0,
    expansionRevenue: 0,
    contractionRevenue: 0,
    churnedRevenue: 0,
    involuntaryChurnedRevenue: 0,
    closingRevenue: 100_000,
  });

  assert.equal(movementSteps(still).length, 7);
  assert.equal(movementReconciles(still), true);
});

test("the value axis includes zero and the running balance's own peak", () => {
  const extent = movementExtent(movementSteps(month()));

  assert.equal(extent.min, 0);
  assert.equal(extent.max, 123_000);
});

test("new and reactivation are separate columns with separate marks", () => {
  assert.notEqual(MOVEMENT_LABELS.new, MOVEMENT_LABELS.reactivation);
  assert.notEqual(
    MOVEMENT_APPEARANCE.new.tone,
    MOVEMENT_APPEARANCE.reactivation.tone
  );
});

test("expansion is told from new business by a channel that is not colour", () => {
  assert.equal(MOVEMENT_APPEARANCE.expansion.fill, "hatch");
  assert.equal(MOVEMENT_APPEARANCE.new.fill, "solid");
});

test("involuntary churn is a proportion of churn rather than a sibling of it", () => {
  assert.equal(involuntaryShareOfChurn(month()), 4_000 / 9_000);
});

test("a month that lost nobody has no involuntary share, rather than a share of zero", () => {
  assert.equal(
    involuntaryShareOfChurn({
      churnedRevenue: 0,
      involuntaryChurnedRevenue: 0,
    }),
    null
  );
});

test("expansion and contraction carry no customer count, and null says so", () => {
  const by = Object.fromEntries(
    movementSteps(month()).map((step) => [step.kind, step])
  );

  assert.equal(by.expansion.customers, null);
  assert.equal(by.contraction.customers, null);
  assert.equal(by.new.customers, 14);
  assert.equal(by.churn.customers, 11);
});

test("a figure with no answer renders as a dash and never as a zero", () => {
  const answer = answered(null, (value) => `${value}%`, "nothingBilled");

  assert.equal(answer.known, false);
  assert.equal(answer.text, NO_ANSWER);
  assert.notEqual(answer.text, "0");
  assert.notEqual(answer.text, "0%");
});

test("a figure that genuinely is zero still renders as zero", () => {
  const answer = answered(0, (value) => `${value}%`, "nothingBilled");

  assert.equal(answer.known, true);
  assert.equal(answer.text, "0%");
  assert.equal(answer.reasonKey, "");
});

test("every absent figure carries a reason a reader can act on", () => {
  for (const reason of Object.keys(NO_ANSWER_REASONS)) {
    const answer = answered(null, String, reason);

    assert.ok(answer.reasonKey, `${reason} has no sentence`);
    assert.ok(
      CATALOGUES.en[answer.reasonKey],
      `${answer.reasonKey} is not in the catalogue`
    );
  }
});

test("a null margin rate is not zero and is not minus one hundred per cent", () => {
  const rate = marginRateAnswer({ marginRate: null }, (value) => `${value}`);

  assert.equal(rate.known, false);
  assert.equal(rate.text, NO_ANSWER);
  assert.equal(rate.reasonKey, NO_ANSWER_REASONS.nothingBilled);
});

test("a null usage trend is reported as no baseline, not as flat usage", () => {
  const trend = usageTrendAnswer({ usageTrend: null }, (value) => `${value}`);

  assert.equal(trend.text, NO_ANSWER);
  assert.equal(trend.reasonKey, NO_ANSWER_REASONS.noBaseline);
});

test("a month that billed nothing has no concentration reading at all", () => {
  assert.equal(concentrationReading(null), null);
  assert.equal(equivalentCustomers(null), null);
  assert.equal(equivalentCustomers(0), null);
});

test("a recovery window with no ladders has no rate, not a rate of zero", () => {
  const rate = answered(null, (value) => `${value}`, "noLaddersOpened");

  assert.equal(rate.text, NO_ANSWER);
});

test("a stage nothing resolved at has a null share rather than zero per cent", () => {
  const rows = recoveryStageRows({ resolvedByStage: [] });

  for (const row of rows) {
    assert.equal(row.resolved, 0);
    assert.equal(row.share, null, `${row.stage} reports a share of zero`);
  }
});

const margin = (overrides = {}) => ({
  organizationId: "org",
  organizationName: "Northwind",
  month: 5,
  year: 2026,
  currency: "GBP",
  revenue: 1_000,
  cost: 400,
  margin: 600,
  marginRate: 0.6,
  costsComplete: true,
  lines: [
    { billableResource: "RuleEvaluation", count: 1_000, unitCost: 0.4, cost: 400 },
  ],
  ...overrides,
});

test("an incomplete row is always flagged, whatever its margin", () => {
  assert.equal(marginIsOverstated(margin({ costsComplete: false })), true);
  assert.equal(marginIsOverstated(margin()), false);
});

test("a hundred per cent margin from uncosted usage never reads as good news", () => {
  const uncosted = margin({
    cost: 0,
    margin: 1_000,
    marginRate: 1,
    costsComplete: false,
    lines: [
      { billableResource: "RuleEvaluation", count: 1_000, unitCost: null, cost: 0 },
    ],
  });

  assert.notEqual(marginTone(uncosted), "settled");
  assert.equal(marginTone(uncosted), "attention");
});

test("no incomplete row takes the settled treatment at any margin rate", () => {
  for (const rate of [null, -0.5, 0, 0.1, 0.35, 0.9, 1]) {
    assert.notEqual(
      marginTone(margin({ marginRate: rate, costsComplete: false })),
      "settled",
      `a rate of ${rate} with incomplete costs was drawn as good news`
    );
  }
});

test("a complete row is still read on its merits", () => {
  assert.equal(marginTone(margin({ marginRate: 0.6 })), "settled");
  assert.equal(marginTone(margin({ marginRate: 0.1 })), "elevated");
  assert.equal(marginTone(margin({ marginRate: -0.2 })), "critical");
  assert.equal(marginTone(margin({ marginRate: null })), "inert");
});

test("the row detail can name which resource nobody has costed", () => {
  const mixed = margin({
    costsComplete: false,
    lines: [
      { billableResource: "RuleEvaluation", count: 10, unitCost: 0.4, cost: 4 },
      { billableResource: "Backtest", count: 2, unitCost: null, cost: 0 },
    ],
  });

  assert.deepEqual(
    uncostedLines(mixed).map((line) => line.billableResource),
    ["Backtest"]
  );
});

test("a unit cost of zero is a real rate and does not read as missing", () => {
  const free = margin({
    lines: [
      { billableResource: "SandboxWrite", count: 10, unitCost: 0, cost: 0 },
    ],
  });

  assert.deepEqual(uncostedLines(free), []);
  assert.equal(isValidUnitCost("0"), true);
  assert.equal(isValidUnitCost("-1"), false);
  assert.equal(isValidUnitCost(""), false);
  assert.equal(isValidUnitCost("free"), false);
});

test("the rates panel can name the resources nobody has costed at all", () => {
  const rates = [{ billableResource: "RuleEvaluation" }];

  assert.deepEqual(
    uncostedResources(rates, BILLABLE_RESOURCE_ORDER),
    BILLABLE_RESOURCE_ORDER.filter((r) => r !== "RuleEvaluation")
  );
});

const health = (overrides = {}) => ({
  organizationId: "org",
  organizationName: "Northwind",
  healthScore: 52,
  healthBand: "Watch",
  periodStart: "2026-05-16T00:00:00Z",
  periodEnd: "2026-06-15T00:00:00Z",
  recentUsage: 400,
  previousUsage: 1_000,
  usageTrend: -0.6,
  recentErrorRate: 0.08,
  previousErrorRate: 0.01,
  daysSinceLastUsage: 4,
  arrearsStage: null,
  signals: [],
  ...overrides,
});

const signal = (kind, penalty, detail = "") => ({ kind, penalty, detail });

test("the signals lead with whatever moved the score most", () => {
  const ordered = signalsInReadingOrder([
    signal("Arrears", 10),
    signal("UsageDecline", 30),
    signal("ErrorRateRise", 15),
  ]);

  assert.deepEqual(
    ordered.map((s) => s.kind),
    ["UsageDecline", "ErrorRateRise", "Arrears"]
  );
});

test("an absence of evidence sorts last however the penalties fall", () => {
  const ordered = signalsInReadingOrder([
    signal("NoBaseline", 0),
    signal("Silence", 5),
  ]);

  assert.deepEqual(ordered.map((s) => s.kind), ["Silence", "NoBaseline"]);
});

test("no baseline is drawn as a different kind of thing, not a milder problem", () => {
  assert.equal(signalTone(signal("NoBaseline", 0)), "inert");

  for (const kind of ["UsageDecline", "ErrorRateRise", "Silence", "Arrears"]) {
    assert.notEqual(
      signalTone(signal(kind, 0)),
      "inert",
      `${kind} is drawn as an absence of evidence`
    );
  }
});

test("a computed score never takes the loudest chip in the vocabulary", () => {
  for (const band of HEALTH_BANDS) {
    assert.notEqual(
      healthBandTone(band),
      "critical",
      `${band} is drawn as loudly as a decision that is actually waiting`
    );
  }

  assert.equal(healthBandTone("AtRisk"), "elevated");
  assert.equal(healthBandTone("Healthy"), "settled");
});

test("the signal sentence is composed from the numbers rather than taken from the API", () => {
  const numbers = {
    percent: (fraction) => `${Math.round(fraction * 100)}%`,
    count: (value) => String(value),
  };

  const decline = signalSentence(
    health(),
    signal("UsageDecline", 30, "Usage fell by 60% against the previous window."),
    numbers
  );

  assert.equal(decline.key, "revenue.signalSentence.usageDecline");
  assert.deepEqual(decline.args, ["60%"]);
});

test("never used and not used recently are different sentences", () => {
  const numbers = { percent: String, count: String };

  assert.equal(
    signalSentence(health({ daysSinceLastUsage: null }), signal("Silence", 5), numbers)
      .key,
    "revenue.signalSentence.silenceNeverUsed"
  );
  assert.equal(
    signalSentence(health({ daysSinceLastUsage: 40 }), signal("Silence", 5), numbers)
      .key,
    "revenue.signalSentence.silence"
  );
});

test("a decline with no baseline to measure it against says so", () => {
  const numbers = { percent: String, count: String };

  assert.equal(
    signalSentence(
      health({ usageTrend: null }),
      signal("UsageDecline", 30),
      numbers
    ).key,
    "revenue.signalSentence.usageDeclineUnmeasured"
  );
});

test("the recovery breakdown reuses the arrears ladder rather than re-mapping it", () => {
  const rows = recoveryStageRows({
    resolvedByStage: [{ stage: "FinalNotice", resolved: 3 }],
  });

  assert.deepEqual(rows.map((row) => row.stage), DUNNING_STAGES);

  for (const row of rows) {
    assert.equal(
      row.labelKey,
      DUNNING_STAGE_LABELS[row.stage],
      `${row.stage} has a second label on this screen`
    );
  }
});

test("every rung is listed, including the ones nothing reached", () => {
  const rows = recoveryStageRows({
    resolvedByStage: [{ stage: "Grace", resolved: 8 }],
  });

  assert.equal(rows.length, DUNNING_STAGES.length);
  assert.equal(rows.find((row) => row.stage === "FinalNotice").resolved, 0);
  assert.equal(rows.find((row) => row.stage === "Grace").share, 1);
});

test("the top rung is still a decision waiting for a person, in every language", () => {
  const FORBIDDEN =
    /suspend|restrict|block|disabled|terminated|gesperrt|suspendu|bloque|bloqueado|suspendido|geblokkeerd|opgeschort/i;

  const rows = recoveryStageRows({ resolvedByStage: [] });
  const top = rows.find((row) => row.stage === "RestrictionProposed");

  for (const language of LANGUAGES) {
    const label = CATALOGUES[language][top.labelKey] ?? CATALOGUES.en[top.labelKey];
    assert.doesNotMatch(
      label,
      FORBIDDEN,
      `${language}: the top rung reads as "${label}" on the revenue board`
    );
  }
});

test("a window that ended recently is reported as still maturing", () => {
  assert.equal(
    cohortIsStillMaturing({ periodEnd: "2026-06-14T00:00:00Z" }, NOW),
    true
  );
});

test("a window old enough for its ladders to have finished is not caveated", () => {
  assert.equal(
    cohortIsStillMaturing({ periodEnd: "2026-01-01T00:00:00Z" }, NOW),
    false
  );
});

test("the cohort caption is a caption rather than a tooltip, and exists everywhere", () => {
  for (const language of LANGUAGES) {
    const caption = CATALOGUES[language]["revenue.recoveryIsACohort"];
    assert.ok(caption, `${language} has no cohort caption`);
    assert.ok(
      caption.length > 80,
      `${language}'s cohort caption is too short to make the argument`
    );
  }
});

test("the index is offered as a number of equally sized customers", () => {
  assert.equal(equivalentCustomers(0.2), 5);
  assert.equal(equivalentCustomers(1), 1);
});

test("the concentration reading is a description rather than an alarm", () => {
  assert.equal(concentrationReading(0.05), "spread");
  assert.equal(concentrationReading(0.18), "concentrated");
  assert.equal(concentrationReading(0.4), "dependent");

  for (const reading of Object.keys(CONCENTRATION_READING_LABELS)) {
    assert.notEqual(concentrationTone(reading), "critical");
  }
});

test("the list length stays inside what the endpoint accepts", () => {
  assert.equal(clampTopCount(900), 500);
  assert.equal(clampTopCount(0), 20);
  assert.equal(clampTopCount(50), 50);
});

test("the health window stays inside what the endpoint accepts", () => {
  assert.equal(clampWindowDays(1000), 365);
  assert.equal(clampWindowDays(0), 30);
});

test("both of the API's currency spellings format without throwing", () => {
  assert.equal(formatRevenue(1_200, "GBP"), "£1,200.00");
  assert.equal(formatRevenue(1_200, "Gbp"), "£1,200.00");
  assert.doesNotThrow(() => formatRevenue(10, "Chf"));
});

test("every movement figure carries its sign, including the plus", () => {
  assert.equal(formatSignedRevenue(1_200, "GBP"), "+£1,200.00");
  assert.equal(formatSignedRevenue(-1_200, "GBP"), "−£1,200.00");
});

test("a backwards range is caught before it reaches the API", () => {
  assert.equal(
    isForwardRange({ month: 5, year: 2026 }, { month: 2, year: 2026 }),
    false
  );
  assert.equal(
    isForwardRange({ month: 11, year: 2025 }, { month: 2, year: 2026 }),
    true
  );
  assert.equal(
    monthsInRange({ month: 11, year: 2025 }, { month: 2, year: 2026 }),
    4
  );
});

test("the export is named after the range rather than after now", () => {
  assert.equal(
    recognitionFileName({ month: 1, year: 2026 }, { month: 3, year: 2026 }),
    "revenue-recognition-2026-01-to-2026-03.csv"
  );
});

const VOCABULARIES = {
  "movement label": MOVEMENT_LABELS,
  "movement hint": MOVEMENT_HINTS,
  "health band": HEALTH_BAND_LABELS,
  "health band hint": HEALTH_BAND_HINTS,
  "signal": SIGNAL_LABELS,
  "billable resource": RESOURCE_LABELS,
  "concentration reading": CONCENTRATION_READING_LABELS,
  "no-answer reason": NO_ANSWER_REASONS,
};

test("every word this board can render exists in English", () => {
  for (const [what, vocabulary] of Object.entries(VOCABULARIES)) {
    for (const [value, key] of Object.entries(vocabulary)) {
      assert.ok(
        CATALOGUES.en[key],
        `${what} ${value} (${key}) is not in the catalogue`
      );
    }
  }
});

test("every revenue key is translated in all five languages, with no fallbacks", () => {
  const keys = Object.keys(CATALOGUES.en).filter(
    (key) => key.startsWith("revenue.") || key === "nav.admin.revenue"
  );

  assert.ok(keys.length > 100, "the revenue catalogue did not load");

  for (const language of LANGUAGES) {
    const missing = keys.filter((key) => !CATALOGUES[language][key]);

    assert.deepEqual(
      missing,
      [],
      `${language} is missing ${missing.length} revenue key(s)`
    );
  }
});

test("no translation quietly reuses the English string where the two should differ", () => {
  for (const language of LANGUAGES.filter((code) => code !== "en")) {
    const copied = Object.keys(CATALOGUES.en)
      .filter((key) => key.startsWith("revenue."))
      .filter((key) => CATALOGUES.en[key].length > 60)
      .filter((key) => CATALOGUES[language][key] === CATALOGUES.en[key]);

    assert.deepEqual(
      copied,
      [],
      `${language} holds ${copied.length} untranslated English sentence(s)`
    );
  }
});

test("the placeholders survive translation", () => {
  for (const key of Object.keys(CATALOGUES.en).filter((k) =>
    k.startsWith("revenue.")
  )) {
    const expected = (CATALOGUES.en[key].match(/\{\d+\}/g) ?? []).sort();

    for (const language of LANGUAGES) {
      const actual = (CATALOGUES[language][key].match(/\{\d+\}/g) ?? []).sort();

      assert.deepEqual(
        actual,
        expected,
        `${language}: ${key} has placeholders ${actual} against English's ${expected}`
      );
    }
  }
});

test("a resource the API adds later is named rather than swallowed", () => {
  assert.equal(resourceLabelKey("QuantumEvaluation"), "QuantumEvaluation");
  assert.equal(resourceLabelKey("Backtest"), RESOURCE_LABELS.Backtest);
});
