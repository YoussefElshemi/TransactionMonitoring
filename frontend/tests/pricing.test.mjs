import assert from "node:assert/strict";
import test from "node:test";

import {
  bandContaining,
  bandSpan,
  headlineRate,
  isSingleFlatRate,
  previewLines,
  previewTotal,
  priceRefusal,
  pricesRefusal,
  totalOf,
  totalsAgree,
  unitsInBand,
  unpricedResources,
} from "../app/lib/pricing.ts";

const band = (fromQuantity, toQuantity, unitAmount, flatAmount = 0) => ({
  fromQuantity,
  toQuantity,
  unitAmount,
  flatAmount,
});

const price = (pricingModel, tiers, freeAllowance = 0, billableResource = null) => ({
  billableResource,
  pricingModel,
  freeAllowance,
  tiers,
});

const version = (prices, platformFee = 0, minimumCharge = 0, currency = "GBP") => ({
  currency,
  platformFee,
  minimumCharge,
  prices,
});

test("a band is half-open over unit positions, so laid end to end none overlap", () => {
  const bottom = band(0, 10_000, 0.1);
  const top = band(10_000, null, 0.05);

  // The 10,000th unit is the bottom band's last, not the top band's first.
  assert.equal(unitsInBand(bottom, 10_000), 10_000);
  assert.equal(unitsInBand(top, 10_000), 0);

  assert.equal(unitsInBand(bottom, 12_000), 10_000);
  assert.equal(unitsInBand(top, 12_000), 2_000);
});

test("a band is read out in the terms a person counts in, not in unit positions", () => {
  assert.equal(bandSpan(band(0, 1_000, 0.1)), "1 to 1,000");
  assert.equal(bandSpan(band(1_000, null, 0.1)), "1,001 and above");
});

test("volume finds the band the last unit falls in", () => {
  const bands = [band(0, 10_000, 0.1), band(10_000, null, 0.05)];

  assert.equal(bandContaining(bands, 10_000).unitAmount, 0.1);
  assert.equal(bandContaining(bands, 10_001).unitAmount, 0.05);
});

test("graduated and volume reach different totals at the same quantity", () => {
  const bands = [band(0, 10_000, 0.1), band(10_000, null, 0.05)];
  const quantities = { RuleEvaluation: 12_000 };

  const graduated = previewTotal({
    version: version([price("Graduated", bands)]),
    quantities,
  });

  const volume = previewTotal({
    version: version([price("Volume", bands)]),
    quantities,
  });

  // Graduated: 10,000 at 0.10 plus 2,000 at 0.05.
  assert.equal(graduated, 1_100);
  // Volume: every one of the 12,000 at the band the 12,000th lands in.
  assert.equal(volume, 600);
});

test("graduated produces a line per band, so the customer sees where each edge was crossed", () => {
  const lines = previewLines({
    version: version([
      price("Graduated", [band(0, 10_000, 0.1), band(10_000, null, 0.05)]),
    ]),
    quantities: { RuleEvaluation: 12_000 },
  });

  assert.equal(lines.length, 2);
  assert.equal(lines[0].description, "Rule evaluations - 1 to 10000");
  assert.equal(lines[1].description, "Rule evaluations - 10001 and above");
  assert.deepEqual(
    lines.map((line) => line.amount),
    [1_000, 100]
  );
});

test("a band's flat amount rides on the band's own line rather than becoming a second", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 0.02, 500)])]),
    quantities: { RuleEvaluation: 1_000 },
  });

  assert.equal(lines.length, 1);
  assert.equal(lines[0].amount, 520);
});

test("the allowance comes off the quantity, and what is left enters the bands from the bottom", () => {
  const lines = previewLines({
    version: version([
      price(
        "Graduated",
        [band(0, 10_000, 0.1), band(10_000, null, 0.05)],
        1_000
      ),
    ]),
    quantities: { RuleEvaluation: 11_000 },
  });

  assert.equal(lines[0].description, "Rule evaluations - included allowance");
  assert.equal(lines[0].quantity, 1_000);
  assert.equal(lines[0].amount, 0);
  assert.equal(totalOf(lines), 1_000);
});

test("usage inside the allowance is still written down, at zero", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 0.1)], 1_000)]),
    quantities: { RuleEvaluation: 400 },
  });

  assert.equal(lines.length, 1);
  assert.equal(lines[0].quantity, 400);
  assert.equal(lines[0].amount, 0);
});

test("work the plan prices by nothing is written down at zero rather than dropped", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 0.1)], 0, "RuleEvaluation")]),
    quantities: { Backtest: 50 },
  });

  assert.equal(lines.length, 1);
  assert.equal(lines[0].description, "Backtested evaluations (not priced by this plan)");
  assert.equal(lines[0].amount, 0);
});

test("a catch-all prices whatever is not named", () => {
  const lines = previewLines({
    version: version([
      price("Flat", [band(0, null, 1)], 0, "Backtest"),
      price("Flat", [band(0, null, 0.1)]),
    ]),
    quantities: { Backtest: 10, RuleEvaluation: 10 },
  });

  assert.deepEqual(
    lines.map((line) => [line.billableResource, line.amount]),
    [
      ["RuleEvaluation", 1],
      ["Backtest", 10],
    ]
  );
});

test("the monthly minimum tops the sum up rather than being charged beside it", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 0.1)])], 0, 50),
    quantities: { RuleEvaluation: 100 },
  });

  // 10 of usage against a floor of 50 is a top-up of 40, not a charge of 60.
  assert.deepEqual(
    lines.map((line) => line.amount),
    [10, 40]
  );
  assert.equal(totalOf(lines), 50);
});

test("a customer who spends past the minimum never sees it", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 0.1)])], 0, 50),
    quantities: { RuleEvaluation: 1_000 },
  });

  assert.equal(lines.length, 1);
  assert.equal(totalOf(lines), 100);
});

test("discounts compound: two of ten per cent are nineteen, not twenty", () => {
  const discount = (value) => ({
    discountType: "Percentage",
    value,
    description: `${value}%`,
  });

  const total = previewTotal({
    version: version([price("Flat", [band(0, null, 1)])]),
    quantities: { RuleEvaluation: 100 },
    discounts: [discount(10), discount(10)],
  });

  assert.equal(total, 81);
});

test("a fixed discount larger than the bill floors it at nothing rather than banking a credit", () => {
  const total = previewTotal({
    version: version([price("Flat", [band(0, null, 1)])]),
    quantities: { RuleEvaluation: 100 },
    discounts: [
      { discountType: "FixedAmount", value: 500, description: "Goodwill" },
    ],
  });

  assert.equal(total, 0);
});

test("a commitment draws down what survives the discounts, and only what is left of it", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 1)])]),
    quantities: { RuleEvaluation: 250 },
    commitment: { amount: 1_000, drawn: 900 },
  });

  const drawdown = lines.find(
    (line) => line.invoiceLineType === "CommitmentDrawdown"
  );

  // 100 left of the balance against 250 owed: the customer pays the overage.
  assert.equal(drawdown.amount, -100);
  assert.equal(
    drawdown.description,
    "Drawn from commitment (£0.00 of £1,000.00 remaining)"
  );
  assert.equal(totalOf(lines), 150);
});

test("an adjustment lands after the drawdown, so goodwill is not paid for out of prepaid balance", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 1)])]),
    quantities: { RuleEvaluation: 250 },
    commitment: { amount: 1_000, drawn: 0 },
    adjustments: [{ description: "Outage credit", amount: -50 }],
  });

  const [drawdown, adjustment] = lines.slice(-2);

  assert.equal(drawdown.amount, -250);
  assert.equal(adjustment.invoiceLineType, "Adjustment");
  assert.equal(totalOf(lines), -50);
});

test("the platform fee is charged whether anything was used or not", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 0.1)])], 25),
    quantities: {},
  });

  assert.equal(lines.length, 1);
  assert.equal(lines[0].invoiceLineType, "PlatformFee");
  assert.equal(totalOf(lines), 25);
});

test("every step in order, and the total is the sum of the lines", () => {
  const lines = previewLines({
    version: version([price("Graduated", [band(0, 100, 1), band(100, null, 0.5)])], 20, 200),
    quantities: { RuleEvaluation: 150 },
    discounts: [{ discountType: "Percentage", value: 10, description: "Agreed" }],
    commitment: { amount: 100, drawn: 0 },
    adjustments: [{ description: "Migration fee", amount: 30 }],
  });

  assert.deepEqual(
    lines.map((line) => line.invoiceLineType),
    [
      "Usage",
      "Usage",
      "PlatformFee",
      "MinimumTopUp",
      "Discount",
      "CommitmentDrawdown",
      "Adjustment",
    ]
  );

  // 100 + 25 usage, 20 fee = 145; topped up to 200; 10% off = 180; 100 drawn
  // from the commitment = 80; plus a 30 migration fee.
  assert.equal(totalOf(lines), 110);
});

test("bands must start at zero, or the first units metered are unpriced", () => {
  const refusal = priceRefusal(price("Graduated", [band(100, null, 0.1)]));

  assert.match(refusal, /does not start at zero/);
});

test("a gap between bands is refused, and the refusal names the gap", () => {
  const refusal = priceRefusal(
    price("Graduated", [band(0, 100, 0.1), band(200, null, 0.05)])
  );

  assert.match(refusal, /break between 100 and 200/);
});

test("the top band never ends, and no band below it is unbounded", () => {
  assert.match(
    priceRefusal(price("Graduated", [band(0, 100, 0.1)])),
    /bounded top band/
  );

  assert.match(
    priceRefusal(price("Graduated", [band(0, null, 0.1), band(100, null, 0.05)])),
    /unbounded band below its top one/
  );
});

test("a flat price has exactly one band", () => {
  assert.match(
    priceRefusal(price("Flat", [band(0, 100, 0.1), band(100, null, 0.05)])),
    /flat but has 2 bands/
  );

  assert.equal(priceRefusal(price("Flat", [band(0, null, 0.1)])), null);
});

test("bands typed out of order are still read in order", () => {
  const refusal = priceRefusal(
    price("Graduated", [band(100, null, 0.05), band(0, 100, 0.1)])
  );

  assert.equal(refusal, null);
});

test("a version prices each kind of work once and has at most one catch-all", () => {
  const flat = [band(0, null, 0.1)];

  assert.match(
    pricesRefusal([
      price("Flat", flat, 0, "Backtest"),
      price("Flat", flat, 0, "Backtest"),
    ]),
    /priced twice/
  );

  assert.match(
    pricesRefusal([price("Flat", flat), price("Flat", flat)]),
    /more than one catch-all/
  );

  assert.equal(
    pricesRefusal([price("Flat", flat), price("Flat", flat, 0, "Backtest")]),
    null
  );
});

test("what a version does not price by name is offered to the builder", () => {
  const unpriced = unpricedResources([
    price([band(0, null, 1)], 0, "Backtest"),
  ]);

  assert.ok(unpriced.includes("RuleEvaluation"));
});

test("only a genuinely single flat unbounded band is quotable as one rate", () => {
  assert.equal(isSingleFlatRate(version([])), true);
  assert.equal(isSingleFlatRate(version([price("Flat", [band(0, null, 0.1)])])), true);

  assert.equal(isSingleFlatRate(version([], 10)), false);
  assert.equal(isSingleFlatRate(version([], 0, 10)), false);
  assert.equal(
    isSingleFlatRate(version([price("Flat", [band(0, null, 0.1)], 500)])),
    false
  );
  assert.equal(
    isSingleFlatRate(
      version([price("Graduated", [band(0, 100, 0.1), band(100, null, 0.05)])])
    ),
    false
  );
  assert.equal(
    isSingleFlatRate(version([price("Flat", [band(0, null, 0.1)], 0, "Backtest")])),
    false
  );
});

test("a total is asserted against its lines rather than recomputed", () => {
  const lines = [
    { amount: 100, sortOrder: 0 },
    { amount: -10, sortOrder: 1 },
  ];

  assert.equal(totalsAgree({ amount: 90, lines }), true);
  assert.equal(totalsAgree({ amount: 100, lines }), false);
});

test("money rounds away from zero, a line at a time", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 0.005)])]),
    quantities: { RuleEvaluation: 1 },
  });

  assert.equal(lines[0].amount, 0.01);
});

test("the headline rate is restated from the catch-all's bottom band", () => {
  const bands = [band(0, 10_000, 0.1), band(10_000, null, 0.05)];

  assert.equal(headlineRate(0.99, [price("Graduated", bands)]), 0.1);

  assert.equal(
    headlineRate(0.99, [price("Flat", [band(0, null, 0.2)], 0, "Backtest")]),
    0.99
  );
});

test("a conversion touches what came off the plan and leaves what was negotiated alone", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 1)])], 100, 0, "GBP"),
    quantities: { RuleEvaluation: 100 },
    billing: { currency: "EUR", conversionRate: 2 },
    adjustments: [{ description: "Goodwill", amount: -50 }],
  });

  const usage = lines.find((line) => line.invoiceLineType === "Usage");
  const fee = lines.find((line) => line.invoiceLineType === "PlatformFee");
  const adjustment = lines.find((line) => line.invoiceLineType === "Adjustment");

  assert.equal(usage.amount, 200);
  assert.equal(fee.amount, 200);
  assert.equal(adjustment.amount, -50);
});

test("a converted minimum names the customer's currency in its own description", () => {
  const lines = previewLines({
    version: version([], 0, 100, "GBP"),
    quantities: {},
    billing: { currency: "EUR", conversionRate: 1.5 },
  });

  const topUp = lines.find((line) => line.invoiceLineType === "MinimumTopUp");

  assert.equal(topUp.amount, 150);
  assert.equal(
    topUp.description,
    "Monthly minimum top-up (minimum €150.00)"
  );
});

test("tax is charged on the net of everything, so a discount reduces the tax with it", () => {
  const withoutDiscount = previewLines({
    version: version([price("Flat", [band(0, null, 1)])]),
    quantities: { RuleEvaluation: 100 },
    tax: { taxRate: 20, description: "United Kingdom VAT at 20%" },
  });

  const withDiscount = previewLines({
    version: version([price("Flat", [band(0, null, 1)])]),
    quantities: { RuleEvaluation: 100 },
    discounts: [
      { discountType: "Percentage", value: 50, description: "Half price" },
    ],
    tax: { taxRate: 20, description: "United Kingdom VAT at 20%" },
  });

  const taxOf = (lines) =>
    lines.find((line) => line.invoiceLineType === "Tax").amount;

  assert.equal(taxOf(withoutDiscount), 20);
  assert.equal(taxOf(withDiscount), 10);
  assert.equal(totalOf(withDiscount), 60);
});

test("the tax line comes last, after the adjustments", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 1)])]),
    quantities: { RuleEvaluation: 100 },
    adjustments: [{ description: "Migration fee", amount: 25 }],
    tax: { taxRate: 20, description: "United Kingdom VAT at 20%" },
  });

  assert.equal(lines[lines.length - 1].invoiceLineType, "Tax");
  assert.equal(lines[lines.length - 1].amount, 25);
});

test("a reverse-charged month still carries its tax line, at zero, with the reason", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 1)])]),
    quantities: { RuleEvaluation: 100 },
    tax: {
      taxRate: 0,
      description:
        "Reverse charge - VAT to be accounted for by the recipient under Article 196 of Council Directive 2006/112/EC",
    },
  });

  const tax = lines.find((line) => line.invoiceLineType === "Tax");

  assert.equal(tax.amount, 0);
  assert.match(tax.description, /Reverse charge/);
});

test("a month with nothing in it gets no tax line, because there is nothing to explain", () => {
  const lines = previewLines({
    version: version([]),
    quantities: {},
    tax: { taxRate: 20, description: "United Kingdom VAT at 20%" },
  });

  assert.equal(lines.length, 0);
});

test("a plan previewed on its own is previewed in its own currency and without tax", () => {
  const lines = previewLines({
    version: version([price("Flat", [band(0, null, 1)])], 0, 0, "EUR"),
    quantities: { RuleEvaluation: 100 },
  });

  assert.equal(lines.some((line) => line.invoiceLineType === "Tax"), false);
  assert.equal(totalOf(lines), 100);
});
