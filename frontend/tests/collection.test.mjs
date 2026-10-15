import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync, readdirSync } from "node:fs";

import { formatCollected, toCurrencyCode } from "../app/lib/currency.ts";

import {
  PAYMENT_STATUS_FILTERS,
  PAYMENT_STATUS_LABELS,
  canCollect,
  canDownloadReceipt,
  currenciesOnPayments,
  decisiveDate,
  hasOutstanding,
  hasSettlement,
  isInFlight,
  isReversed,
  outstandingSummary,
  paymentMethodDescription,
  settlementRoute,
  statusQueryValue,
} from "../app/lib/payments.ts";

import {
  daysUntilExpiry,
  hasExpired,
  isAbsoluteHttpsUrl,
  isExpiringSoon,
  methodsInReadingOrder,
  needsAttention,
} from "../app/lib/paymentMethods.ts";

import {
  balanceMeaningKey,
  balancesInReadingOrder,
  entryDirection,
  entriesNewestFirst,
  formatSignedAmount,
  isUntouched,
  statementRows,
  NOTHING_HAS_MOVED,
  SQUARE_EXPLAINED,
  UNAPPLIED_CASH_EXPLAINED,
} from "../app/lib/account.ts";

import {
  DUNNING_STAGE_LABELS,
  arrearsInWorkingOrder,
  daysLate,
  decisionsWaiting,
  formatOutstanding,
  hasScheduledEvent,
  isResolved,
  needsDecision,
  stageIndex,
} from "../app/lib/dunning.ts";

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

const rendered = (language, key) =>
  CATALOGUES[language][key] ?? CATALOGUES.en[key] ?? key;

test("the collection side's currency spelling is normalised before it reaches Intl", () => {
  assert.equal(toCurrencyCode("Gbp"), "GBP");
  assert.equal(toCurrencyCode("Eur"), "EUR");
  assert.equal(toCurrencyCode("Usd"), "USD");
});

test("an unrecognised currency renders imperfectly rather than not at all", () => {
  assert.doesNotThrow(() => formatCollected(10, "Chf"));
});

test("formatting a collected amount does not throw on the wire spelling", () => {
  assert.equal(formatCollected(1200, "Gbp"), "£1,200.00");
  assert.equal(formatCollected(1200, "Eur"), "€1,200.00");
});

const payment = (overrides = {}) => ({
  paymentId: "pay",
  organizationId: "org",
  amount: 1200,
  currency: "Gbp",
  paymentStatus: "Settled",
  paymentMethodKind: "Card",
  paymentMethodLabel: "Visa 4242",
  remittanceReference: null,
  receiptNumber: "RCP-0001",
  raisedForInvoiceId: "inv",
  receivedAt: "2026-06-01T00:00:00Z",
  settledAt: "2026-06-04T00:00:00Z",
  returnedAt: null,
  failureReason: null,
  recordedByUserId: null,
  createdAt: "2026-06-01T00:00:00Z",
  updatedAt: "2026-06-04T00:00:00Z",
  ...overrides,
});

test("pending is in flight and is never one of the finished states", () => {
  const pending = payment({ paymentStatus: "Pending", settledAt: null, receiptNumber: null });

  assert.equal(isInFlight(pending), true);
  assert.equal(isReversed(pending), false);
  assert.equal(canDownloadReceipt(pending), false);
});

test("pending is not called pending, because the word means something else here", () => {
  assert.equal(rendered("en", PAYMENT_STATUS_LABELS.Pending), "On its way");
});

test("every status the badge can render has a word and a hint in the catalogue", () => {
  for (const [status, key] of Object.entries(PAYMENT_STATUS_LABELS)) {
    assert.ok(CATALOGUES.en[key], `${status} has no label in the catalogue`);
  }
});

test("a reversal keeps its receipt number and still offers no receipt", () => {
  const returned = payment({
    paymentStatus: "Returned",
    returnedAt: "2026-07-01T00:00:00Z",
  });

  assert.equal(returned.receiptNumber, "RCP-0001");
  assert.equal(canDownloadReceipt(returned), false);
  assert.equal(isReversed(returned), true);
});

test("a settled payment is the only one with a receipt to download", () => {
  assert.equal(canDownloadReceipt(payment()), true);
  assert.equal(canDownloadReceipt(payment({ paymentStatus: "Failed", receiptNumber: null })), false);
});

test("a settled payment with no number yet does not offer a download", () => {
  assert.equal(canDownloadReceipt(payment({ receiptNumber: null })), false);
});

test("every state is in the default list, including the two that are not success", () => {
  assert.deepEqual(PAYMENT_STATUS_FILTERS, [
    "all",
    "Pending",
    "Settled",
    "Failed",
    "Returned",
  ]);
});

test("the filter sends the enum name rather than the console's own word for it", () => {
  assert.equal(statusQueryValue("Returned"), "Returned");
  assert.equal(statusQueryValue("all"), null);
});

test("a row is dated by the event that ended it, and says which", () => {
  assert.deepEqual(decisiveDate(payment()), {
    labelKey: "payments.dated.settled",
    value: "2026-06-04T00:00:00Z",
  });

  assert.deepEqual(
    decisiveDate(payment({ paymentStatus: "Returned", returnedAt: "2026-07-01T00:00:00Z" })),
    { labelKey: "payments.dated.reversed", value: "2026-07-01T00:00:00Z" }
  );

  assert.deepEqual(
    decisiveDate(payment({ paymentStatus: "Pending", settledAt: null })),
    { labelKey: "payments.dated.requested", value: "2026-06-01T00:00:00Z" }
  );
});

test("a settled payment missing its settled date falls back rather than rendering nothing", () => {
  assert.deepEqual(decisiveDate(payment({ settledAt: null })), {
    labelKey: "payments.dated.requested",
    value: "2026-06-01T00:00:00Z",
  });
});

test("a bank transfer with nothing stored to charge still describes itself", () => {
  assert.deepEqual(
    paymentMethodDescription(
      payment({ paymentMethodKind: "BankTransfer", paymentMethodLabel: null })
    ),
    { labelKey: "payments.kind.bankTransfer" }
  );
});

test("a provider's own label is handed back as text, never as a key to translate", () => {
  assert.deepEqual(paymentMethodDescription(payment()), { label: "Visa 4242" });
});

test("a list holding two currencies can say so", () => {
  assert.deepEqual(
    currenciesOnPayments([payment(), payment({ currency: "Eur" })]),
    ["EUR", "GBP"]
  );
});

const settlement = (overrides = {}) => ({
  invoiceId: "inv",
  amount: 1200,
  allocatedAmount: 1200,
  creditedAmount: 0,
  outstandingAmount: 0,
  isSettled: true,
  ...overrides,
});

test("a draft has no settlement, and absent is not zero", () => {
  assert.equal(hasSettlement({ invoice: {} }), false);
  assert.equal(hasSettlement({ invoice: {}, settlement: settlement() }), true);
});

test("settled by credit note is not settled by payment, and is not called paid", () => {
  assert.equal(
    settlementRoute(settlement({ allocatedAmount: 0, creditedAmount: 1200 })),
    "credited"
  );
  assert.equal(settlementRoute(settlement()), "paid");
  assert.equal(
    settlementRoute(settlement({ allocatedAmount: 700, creditedAmount: 500 })),
    "both"
  );
});

test("a zero-total invoice is settled without being either paid or credited", () => {
  assert.equal(
    settlementRoute(settlement({ amount: 0, allocatedAmount: 0, creditedAmount: 0 })),
    "neither"
  );
});

test("a settled invoice is summarised as having nothing outstanding rather than as paid", () => {
  assert.deepEqual(outstandingSummary(settlement(), "GBP"), {
    labelKey: "billing.nothingOutstanding",
  });
  assert.equal(
    rendered("en", "billing.nothingOutstanding"),
    "Nothing outstanding"
  );
  assert.deepEqual(
    outstandingSummary(settlement({ isSettled: false, outstandingAmount: 500 }), "GBP"),
    { amount: "£500.00" }
  );
});

test("pay now is offered only on an issued invoice with something left on it", () => {
  const issued = { invoiceStatus: "Issued" };
  const draft = { invoiceStatus: "Draft" };
  const owing = settlement({ isSettled: false, outstandingAmount: 500, allocatedAmount: 700 });

  assert.equal(canCollect(issued, owing), true);
  assert.equal(canCollect(issued, settlement()), false);
  assert.equal(canCollect(draft, undefined), false);
  assert.equal(hasOutstanding(owing), true);
});

const method = (overrides = {}) => ({
  paymentMethodReference: "pm_1",
  paymentMethodKind: "Card",
  label: "Visa 4242",
  mandateReference: null,
  isDefault: false,
  expiresAt: "2026-12-31T00:00:00Z",
  ...overrides,
});

test("a mandate has no expiry, and is not reported as expiring today", () => {
  const mandate = method({ paymentMethodKind: "DirectDebit", expiresAt: null });

  assert.equal(daysUntilExpiry(mandate, NOW), null);
  assert.equal(isExpiringSoon(mandate, NOW), false);
  assert.equal(hasExpired(mandate, NOW), false);
});

test("a card is warned about roughly two months out, not one", () => {
  assert.equal(isExpiringSoon(method({ expiresAt: "2026-07-20T00:00:00Z" }), NOW), true);
  assert.equal(isExpiringSoon(method({ expiresAt: "2026-12-31T00:00:00Z" }), NOW), false);
});

test("an expired card is a louder state than an expiring one, and not both", () => {
  const expired = method({ expiresAt: "2026-01-01T00:00:00Z" });

  assert.equal(hasExpired(expired, NOW), true);
  assert.equal(isExpiringSoon(expired, NOW), false);
  assert.equal(needsAttention(expired, NOW), true);
});

test("the default leads the list, then whatever needs acting on", () => {
  const ordered = methodsInReadingOrder(
    [
      method({ paymentMethodReference: "a", label: "Amex 1111" }),
      method({ paymentMethodReference: "b", label: "Expiring", expiresAt: "2026-07-01T00:00:00Z" }),
      method({ paymentMethodReference: "c", label: "Default", isDefault: true }),
    ],
    NOW
  );

  assert.deepEqual(
    ordered.map((m) => m.paymentMethodReference),
    ["c", "b", "a"]
  );
});

test("a session return address must be absolute https, because a card was just typed into it", () => {
  assert.equal(isAbsoluteHttpsUrl("https://console.example.com/organization"), true);
  assert.equal(isAbsoluteHttpsUrl("http://console.example.com/organization"), false);
  assert.equal(isAbsoluteHttpsUrl("/organization#Billing"), false);
  assert.equal(isAbsoluteHttpsUrl("http://localhost:3000/organization"), false);
});

const entry = (overrides = {}) => ({
  accountEntryId: "ae",
  organizationId: "org",
  currency: "Gbp",
  amount: 1200,
  reason: "Receipt",
  paymentId: "pay",
  invoiceId: null,
  creditNoteId: null,
  description: "Payment received",
  createdByUserId: null,
  createdAt: "2026-06-01T00:00:00Z",
  ...overrides,
});

test("direction is read off the sign the ledger sent, never off the reason", () => {
  assert.equal(entryDirection(entry({ amount: 1200, reason: "Refund" })), "in");
  assert.equal(entryDirection(entry({ amount: -1200, reason: "Receipt" })), "out");
});

test("a statement line always shows its sign, including the plus", () => {
  assert.equal(formatSignedAmount(entry({ amount: 1200 })), "+£1,200.00");
  assert.equal(formatSignedAmount(entry({ amount: -1200 })), "−£1,200.00");
});

test("the running balance walks backwards from the real closing balance", () => {
  const rows = statementRows(
    [
      entry({ accountEntryId: "newest", amount: -500, createdAt: "2026-06-03T00:00:00Z" }),
      entry({ accountEntryId: "oldest", amount: 1200, createdAt: "2026-06-01T00:00:00Z" }),
    ],
    [{ organizationId: "org", currency: "Gbp", amount: 700, entryCount: 2 }]
  );

  assert.deepEqual(
    rows.map((row) => [row.entry.accountEntryId, row.balanceAfter]),
    [
      ["newest", 700],
      ["oldest", 1200],
    ]
  );
});

test("two currencies on one page get two independent runnings, never one", () => {
  const rows = statementRows(
    [
      entry({ accountEntryId: "eur", currency: "Eur", amount: 300, createdAt: "2026-06-03T00:00:00Z" }),
      entry({ accountEntryId: "gbp", currency: "Gbp", amount: 1200, createdAt: "2026-06-01T00:00:00Z" }),
    ],
    [
      { organizationId: "org", currency: "Gbp", amount: 1200, entryCount: 1 },
      { organizationId: "org", currency: "Eur", amount: 300, entryCount: 1 },
    ]
  );

  assert.deepEqual(
    rows.map((row) => [row.entry.accountEntryId, row.balanceAfter]),
    [
      ["eur", 300],
      ["gbp", 1200],
    ]
  );
});

test("entries are ordered newest first regardless of what order they arrived in", () => {
  const ordered = entriesNewestFirst([
    entry({ accountEntryId: "old", createdAt: "2026-06-01T00:00:00Z" }),
    entry({ accountEntryId: "new", createdAt: "2026-06-09T00:00:00Z" }),
  ]);

  assert.deepEqual(ordered.map((e) => e.accountEntryId), ["new", "old"]);
});

test("a positive balance is explained as unapplied cash rather than left to read as a fault", () => {
  const credit = { amount: 500, entryCount: 3 };
  assert.equal(balanceMeaningKey(credit), UNAPPLIED_CASH_EXPLAINED);
  assert.match(rendered("en", UNAPPLIED_CASH_EXPLAINED), /in credit/i);
});

test("an account that has never moved is told apart from one that is square", () => {
  assert.equal(isUntouched({ amount: 0, entryCount: 0 }), true);
  assert.equal(isUntouched({ amount: 0, entryCount: 4 }), false);
  assert.equal(balanceMeaningKey({ amount: 0, entryCount: 0 }), NOTHING_HAS_MOVED);
  assert.equal(balanceMeaningKey({ amount: 0, entryCount: 4 }), SQUARE_EXPLAINED);
});

test("balances sit in a fixed order so a reader finds one in the same place twice", () => {
  const ordered = balancesInReadingOrder([
    { organizationId: "o", currency: "Usd", amount: 1, entryCount: 1 },
    { organizationId: "o", currency: "Eur", amount: 900, entryCount: 1 },
  ]);

  assert.deepEqual(ordered.map((b) => b.currency), ["Eur", "Usd"]);
});

const dunning = (overrides = {}) => ({
  dunningStateId: "ds",
  invoiceId: "inv",
  invoiceNumber: "INV-0001",
  organizationId: "org",
  stage: "FirstNotice",
  outstandingAmount: 1200,
  currency: "Gbp",
  dueAt: "2026-06-01T00:00:00Z",
  nextAttemptAt: "2026-06-20T00:00:00Z",
  lastNoticedAt: "2026-06-05T00:00:00Z",
  collectionAttempts: 1,
  lastAttemptedAt: "2026-06-05T00:00:00Z",
  restrictionProposedAt: null,
  resolvedAt: null,
  createdAt: "2026-06-02T00:00:00Z",
  ...overrides,
});

test("the last stage is never named as something that has been done to anybody", () => {
  assert.equal(
    rendered("en", DUNNING_STAGE_LABELS.RestrictionProposed),
    "Decision needed"
  );
});

test("no stage is named as something done to the customer, in any language", () => {
  const FORBIDDEN =
    /suspend|restrict|block|disabled|terminated|gesperrt|suspendu|bloque|bloqueado|suspendido|geblokkeerd|opgeschort/i;

  for (const language of Object.keys(CATALOGUES)) {
    for (const [stage, key] of Object.entries(DUNNING_STAGE_LABELS)) {
      const label = rendered(language, key);
      assert.doesNotMatch(
        label,
        FORBIDDEN,
        `${language}: ${stage} is rendered as "${label}", which claims something was done to the customer`
      );
    }
  }
});

test("every ladder stage has a word and an explanation in the catalogue", () => {
  for (const [stage, key] of Object.entries(DUNNING_STAGE_LABELS)) {
    assert.ok(CATALOGUES.en[key], `${stage} has no label in the catalogue`);
  }
});

test("the top of the ladder needs a person and schedules nothing", () => {
  const proposed = dunning({
    stage: "RestrictionProposed",
    restrictionProposedAt: "2026-06-10T00:00:00Z",
  });

  assert.equal(needsDecision(proposed), true);
  assert.equal(hasScheduledEvent(proposed), false);
  assert.equal(hasScheduledEvent(dunning()), true);
});

test("a resolved row schedules nothing either", () => {
  assert.equal(hasScheduledEvent(dunning({ resolvedAt: "2026-06-11T00:00:00Z" })), false);
  assert.equal(isResolved(dunning({ resolvedAt: "2026-06-11T00:00:00Z" })), true);
});

test("the board leads with the rows waiting on a person", () => {
  const ordered = arrearsInWorkingOrder([
    dunning({ dunningStateId: "grace", stage: "Grace" }),
    dunning({ dunningStateId: "decide", stage: "RestrictionProposed" }),
    dunning({ dunningStateId: "final", stage: "FinalNotice" }),
  ]);

  assert.deepEqual(ordered.map((d) => d.dunningStateId), ["decide", "final", "grace"]);
});

test("a resolved final notice sinks below an open grace period", () => {
  const ordered = arrearsInWorkingOrder([
    dunning({ dunningStateId: "resolved", stage: "RestrictionProposed", resolvedAt: "2026-06-11T00:00:00Z" }),
    dunning({ dunningStateId: "open", stage: "Grace" }),
  ]);

  assert.deepEqual(ordered.map((d) => d.dunningStateId), ["open", "resolved"]);
});

test("only open rows count as decisions waiting", () => {
  assert.equal(
    decisionsWaiting([
      dunning({ stage: "RestrictionProposed" }),
      dunning({ stage: "RestrictionProposed", resolvedAt: "2026-06-11T00:00:00Z" }),
      dunning({ stage: "Grace" }),
    ]),
    1
  );
});

test("an unrecognised stage sorts to the bottom rather than above a final notice", () => {
  assert.equal(stageIndex("Quarantined"), 0);
});

test("an amount whose currency did not travel is not stated in the platform's own", () => {
  assert.equal(formatOutstanding(dunning({ currency: null })), "1,200.00");
  assert.equal(formatOutstanding(dunning()), "£1,200.00");
});

test("a row whose due date did not travel has an unknown age, not an age of zero", () => {
  assert.equal(daysLate(dunning({ dueAt: null }), NOW), null);
  assert.equal(daysLate(dunning(), NOW), 14);
});
