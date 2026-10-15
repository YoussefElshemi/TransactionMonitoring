import assert from "node:assert/strict";
import test from "node:test";

import {
  canDeleteAdjustment,
  canDeleteCommitment,
  commitmentWindowRefusal,
  deleteDiscountEffect,
  directionOf,
  discountValueRefusal,
  discountWindowRefusal,
  drawnFraction,
  isValidYear,
  overlapsExisting,
  periodLabel,
  signedAmount,
  termState,
} from "../app/lib/contractTerms.ts";

/** Fixed, so a suite run at midnight cannot disagree with one run at noon. */
const NOW = new Date("2026-06-15T12:00:00Z");

const discount = (effectiveFrom, effectiveTo) => ({
  effectiveFrom,
  effectiveTo,
});

test("a percentage discount cannot exceed a hundred, and a fixed one can", () => {
  assert.match(
    discountValueRefusal("Percentage", 120),
    /cannot exceed 100/
  );
  assert.equal(discountValueRefusal("FixedAmount", 120), null);
});

test("a discount of nothing is refused either way", () => {
  assert.match(discountValueRefusal("Percentage", 0), /not a discount/);
  assert.match(discountValueRefusal("FixedAmount", -5), /not a discount/);
});

test("a window that closes before it opens is refused", () => {
  assert.match(
    discountWindowRefusal("2026-06-01T00:00", "2026-05-01T00:00"),
    /at or before it starts/
  );

  assert.equal(discountWindowRefusal("2026-06-01T00:00", ""), null);
});

test("delete removes a discount that has not started, and ends one that has", () => {
  const scheduled = deleteDiscountEffect(
    discount("2026-07-01T00:00:00Z", null),
    NOW
  );
  assert.equal(scheduled.removes, true);

  const live = deleteDiscountEffect(
    discount("2026-01-01T00:00:00Z", null),
    NOW
  );
  assert.equal(live.removes, false);
  assert.match(live.message, /ended rather than deleted/);
});

test("ending a discount that already expired does not extend it to today", () => {
  const expired = deleteDiscountEffect(
    discount("2026-01-01T00:00:00Z", "2026-03-01T00:00:00Z"),
    NOW
  );

  assert.equal(expired.removes, false);
  assert.match(expired.message, /not moved to today/);
});

test("a dated term is scheduled, in force or expired, and never two of them", () => {
  assert.equal(termState(discount("2026-07-01T00:00:00Z", null), NOW), "scheduled");
  assert.equal(termState(discount("2026-01-01T00:00:00Z", null), NOW), "inForce");
  assert.equal(
    termState(discount("2026-01-01T00:00:00Z", "2026-03-01T00:00:00Z"), NOW),
    "expired"
  );
});

test("the sign comes from the direction chosen, not from what was typed", () => {
  assert.equal(signedAmount("credit", 50), -50);
  assert.equal(signedAmount("credit", -50), -50);
  assert.equal(signedAmount("charge", 50), 50);
  assert.equal(signedAmount("charge", -50), 50);
});

test("an existing adjustment reads back the way it was raised", () => {
  assert.equal(directionOf({ amount: -50 }), "credit");
  assert.equal(directionOf({ amount: 50 }), "charge");
});

test("an adjustment already on an invoice cannot be deleted", () => {
  assert.equal(canDeleteAdjustment({ appliedToInvoiceId: null }), true);
  assert.equal(canDeleteAdjustment({ appliedToInvoiceId: "invoice" }), false);
});

test("a year is bounded at next year, because that is what the API takes", () => {
  assert.equal(isValidYear(2026, NOW), true);
  assert.equal(isValidYear(2027, NOW), true);
  assert.equal(isValidYear(2028, NOW), false);
  assert.equal(isValidYear(1999, NOW), false);
});

test("a period reads as a month and a year", () => {
  assert.equal(periodLabel({ month: 3, year: 2026 }), "March 2026");
});

test("a commitment needs an end date, because the term is what it is spent over", () => {
  assert.match(commitmentWindowRefusal("2026-01-01T00:00", ""), /needs an end date/);
});

test("terms that meet exactly do not overlap, but terms that share a month do", () => {
  const existing = [
    {
      effectiveFrom: "2026-01-01T00:00:00Z",
      effectiveTo: "2026-07-01T00:00:00Z",
    },
  ];

  assert.equal(
    overlapsExisting(
      {
        effectiveFrom: "2026-07-01T00:00:00Z",
        effectiveTo: "2027-01-01T00:00:00Z",
      },
      existing
    ),
    false
  );

  assert.equal(
    overlapsExisting(
      {
        effectiveFrom: "2026-06-01T00:00:00Z",
        effectiveTo: "2027-01-01T00:00:00Z",
      },
      existing
    ),
    true
  );
});

test("a commitment anything has drawn against cannot be deleted", () => {
  assert.equal(canDeleteCommitment({ drawn: 0 }), true);
  assert.equal(canDeleteCommitment({ drawn: 0.01 }), false);
});

test("the drawn fraction is bounded, so the bar cannot run past its track", () => {
  assert.equal(drawnFraction({ amount: 1000, drawn: 250 }), 0.25);
  assert.equal(drawnFraction({ amount: 1000, drawn: 4000 }), 1);
  assert.equal(drawnFraction({ amount: 0, drawn: 0 }), 0);
});
