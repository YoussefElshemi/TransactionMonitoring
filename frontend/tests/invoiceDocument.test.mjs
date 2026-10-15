import assert from "node:assert/strict";
import test from "node:test";

import {
  canCredit,
  canDelete,
  canIssue,
  currenciesPresent,
  daysOverdue,
  exportFileName,
  invoiceLabel,
  invoicesInWorkingOrder,
  isOverdue,
  isTaxLine,
  matchesStatusFilter,
  paymentTermsSentence,
  predatesParties,
  totalsBalance,
} from "../app/lib/invoiceDocument.ts";
import {
  conversionSentence,
  formatAgreed,
  formatMoney,
  wasConverted,
} from "../app/lib/currency.ts";
import { formatAmount, formatPercentage } from "../app/lib/pricing.ts";
import { formatRate, rateSentence } from "../app/lib/plans.ts";
import { predictRate, predictTreatment } from "../app/lib/tax.ts";
import {
  isFullyCredited,
  creditRefusal,
  uncreditedAmount,
} from "../app/lib/creditNotes.ts";
import {
  entityRefusal,
  prefixesRefusal,
} from "../app/lib/billingEntities.ts";
import {
  registrationState,
  wouldClearVerification,
} from "../app/lib/billingProfiles.ts";
import {
  rateForMonth,
  rateInForce,
  ratesByPair,
} from "../app/lib/exchangeRates.ts";

const invoice = (over = {}) => ({
  invoiceId: "invoice-1",
  invoiceNumber: "INV-2026-000042",
  invoiceStatus: "Issued",
  billingEntityId: "entity-1",
  supplier: {
    legalName: "Platform Ltd",
    address: "1 Example Street\nLondon",
    countryCode: "GB",
    taxIdentifier: "GB123456789",
    email: null,
  },
  customer: {
    legalName: "Customer BV",
    address: "2 Voorbeeldstraat\nAmsterdam",
    countryCode: "NL",
    taxIdentifier: "NL123456789B01",
    email: "ap@customer.example",
  },
  currency: "EUR",
  planCurrency: "GBP",
  conversionRate: 1.2,
  taxTreatment: "ReverseCharge",
  taxRate: 0,
  taxAmount: 0,
  netAmount: 1_200,
  amount: 1_200,
  issuedAt: "2026-02-01T00:00:00Z",
  dueAt: "2026-03-03T00:00:00Z",
  paymentTerms: 30,
  month: 1,
  year: 2026,
  isProcessed: false,
  lines: [],
  ...over,
});

const draft = (over = {}) =>
  invoice({
    invoiceNumber: null,
    invoiceStatus: "Draft",
    issuedAt: null,
    dueAt: null,
    ...over,
  });

test("a draft can be issued and withdrawn, and cannot be credited", () => {
  const it = draft();

  assert.equal(canIssue(it), true);
  assert.equal(canDelete(it), true);
  assert.equal(canCredit(it), false);
});

test("an issued invoice can be credited, and cannot be issued again or deleted", () => {
  const it = invoice();

  assert.equal(canIssue(it), false);
  assert.equal(canDelete(it), false);
  assert.equal(canCredit(it), true);
});

test("the two sets of affordances are never both offered", () => {
  for (const it of [draft(), invoice()]) {
    assert.notEqual(canDelete(it), canCredit(it));
    assert.equal(canIssue(it), canDelete(it));
  }
});

test("a draft is named rather than left blank where its number would go", () => {
  assert.equal(invoiceLabel(invoice()), "INV-2026-000042");
  assert.match(invoiceLabel(draft()), /Draft/);

  assert.equal(exportFileName(invoice()), "INV-2026-000042.pdf");
  assert.equal(exportFileName(draft()), "invoice-1.pdf");
});

const at = (iso) => new Date(iso);

test("overdue is a comparison against the due date", () => {
  const it = invoice();

  assert.equal(isOverdue(it, at("2026-03-02T00:00:00Z")), false);
  assert.equal(isOverdue(it, at("2026-03-03T00:00:00Z")), false);
  assert.equal(isOverdue(it, at("2026-03-04T00:00:00Z")), true);
});

test("a draft is never overdue, however long it has been sitting there", () => {
  assert.equal(isOverdue(draft(), at("2030-01-01T00:00:00Z")), false);
  assert.equal(daysOverdue(draft(), at("2030-01-01T00:00:00Z")), 0);
});

test("a processed invoice is not overdue however long it sat", () => {
  const settled = invoice({ isProcessed: true });

  assert.equal(isOverdue(settled, at("2027-01-01T00:00:00Z")), false);
});

test("how late is counted in whole days", () => {
  assert.equal(daysOverdue(invoice(), at("2026-03-13T00:00:00Z")), 10);
  assert.equal(daysOverdue(invoice(), at("2026-03-03T12:00:00Z")), 0);
});

test("terms of zero days read as due on issue rather than as 0 days", () => {
  assert.equal(paymentTermsSentence(0), "Due on issue");
  assert.equal(paymentTermsSentence(1), "Due 1 day from issue");
  assert.equal(paymentTermsSentence(30), "Due 30 days from issue");
});

test("the conversion is stated only where the two currencies differ", () => {
  assert.equal(wasConverted(invoice()), true);

  assert.equal(
    wasConverted(
      invoice({ currency: "GBP", planCurrency: "GBP", conversionRate: null })
    ),
    false
  );
});

test("a null rate is never rendered as a conversion, whatever the currencies say", () => {
  assert.equal(wasConverted(invoice({ conversionRate: null })), false);
});

test("the conversion note names both currencies and the rate between them", () => {
  const sentence = conversionSentence("GBP", "EUR", 1.2);

  assert.match(sentence, /GBP/);
  assert.match(sentence, /1\.20 EUR\/GBP/);
});

test("a conversion rate keeps its precision rather than rounding to two", () => {
  assert.match(conversionSentence("GBP", "EUR", 1.1834), /1\.1834/);
});

const line = (over = {}) => ({
  invoiceLineId: "line-1",
  invoiceLineType: "Usage",
  billableResource: "RuleEvaluation",
  description: "Rule evaluations",
  quantity: 1_000,
  unitAmount: 1.2,
  amount: 1_200,
  invoiceAdjustmentId: null,
  sortOrder: 0,
  ...over,
});

test("the tax line is told apart from the working it is charged on", () => {
  const usage = line();
  const tax = line({
    invoiceLineId: "line-2",
    invoiceLineType: "Tax",
    billableResource: null,
    description:
      "Reverse charge - VAT to be accounted for by the recipient under Article 196 of Council Directive 2006/112/EC",
    quantity: 1,
    unitAmount: 0,
    amount: 0,
    sortOrder: 1,
  });

  assert.equal(isTaxLine(usage), false);
  assert.equal(isTaxLine(tax), true);
});

test("a zero tax line survives with its description, because the description is the reason", () => {
  const tax = line({
    invoiceLineType: "Tax",
    amount: 0,
    description: "Reverse charge - VAT to be accounted for by the recipient",
  });

  const lines = [line(), tax];
  const body = lines.filter((entry) => !isTaxLine(entry));
  const found = lines.find(isTaxLine);

  assert.equal(body.length, 1);
  assert.equal(found.amount, 0);
  assert.match(found.description, /Reverse charge/);
});

test("the document's own three figures are checked against each other", () => {
  assert.equal(totalsBalance(invoice()), true);
  assert.equal(
    totalsBalance(invoice({ netAmount: 1_000, taxAmount: 200, amount: 1_200 })),
    true
  );

  assert.equal(
    totalsBalance(invoice({ netAmount: 1_000, taxAmount: 200, amount: 1_201 })),
    false
  );
});

test("an invoice raised before parties were recorded renders without crashing", () => {
  const old = invoice({
    invoiceNumber: null,
    billingEntityId: null,
    supplier: null,
    customer: null,
  });

  assert.equal(predatesParties(old), true);
  assert.equal(canCredit(old), true);
  assert.equal(canDelete(old), false);
  assert.match(invoiceLabel(old), /Draft/);
});

test("parties are both present or both absent, and half of one is not treated as absent", () => {
  assert.equal(predatesParties(invoice()), false);
  assert.equal(predatesParties(invoice({ customer: null })), false);
  assert.equal(
    predatesParties(invoice({ supplier: null, customer: null })),
    true
  );
});

test("drafts sort above issued documents, and each group runs newest first", () => {
  const rows = [
    invoice({ invoiceId: "a", month: 1, year: 2026 }),
    draft({ invoiceId: "b", month: 2, year: 2026 }),
    invoice({ invoiceId: "c", month: 3, year: 2026 }),
    draft({ invoiceId: "d", month: 4, year: 2026 }),
  ];

  assert.deepEqual(
    invoicesInWorkingOrder(rows).map((row) => row.invoiceId),
    ["d", "b", "c", "a"]
  );
});

test("months are ordered as months rather than by when they were raised", () => {
  const rows = [
    invoice({ invoiceId: "march", month: 3, year: 2026 }),
    invoice({ invoiceId: "january", month: 1, year: 2026 }),
    invoice({ invoiceId: "december", month: 12, year: 2025 }),
  ];

  assert.deepEqual(
    invoicesInWorkingOrder(rows).map((row) => row.invoiceId),
    ["march", "january", "december"]
  );
});

test("the status filter narrows to one kind of document or neither", () => {
  assert.equal(matchesStatusFilter(draft(), "all"), true);
  assert.equal(matchesStatusFilter(draft(), "Draft"), true);
  assert.equal(matchesStatusFilter(draft(), "Issued"), false);
  assert.equal(matchesStatusFilter(invoice(), "Issued"), true);
});

test("a list reports every currency it is holding", () => {
  const rows = [
    invoice({ currency: "GBP" }),
    invoice({ currency: "EUR" }),
    invoice({ currency: "GBP" }),
  ];

  assert.deepEqual(currenciesPresent(rows), ["GBP", "EUR"]);
});

test("an amount is formatted in the currency it is handed, not a hardcoded symbol", () => {
  assert.equal(formatMoney(1_200, "GBP"), "£1,200.00");
  assert.equal(formatMoney(1_200, "EUR"), "€1,200.00");

  assert.notEqual(formatMoney(1_200, "GBP"), formatMoney(1_200, "USD"));
});

test("an invoice amount is formatted from the invoice, not from anywhere else", () => {
  const document = invoice();

  assert.equal(
    formatAmount(document.amount, document.currency),
    "€1,200.00"
  );
});

test("amounts are fixed at two decimals and rates keep their significant digits", () => {
  assert.equal(formatAmount(1_200.005, "GBP"), "£1,200.01");
  assert.match(formatRate(0.0025, "GBP"), /0\.0025/);
  assert.match(rateSentence(0.0025, "GBP"), /per request$/);
});

test("a percentage never picks up a currency symbol", () => {
  assert.equal(formatPercentage(20), "20%");
  assert.equal(formatPercentage(25.5), "25.5%");
});

test("an amount with no profile to state a currency says so rather than inventing one", () => {
  assert.equal(formatAgreed(1_200, "GBP"), "£1,200.00");
  assert.equal(formatAgreed(1_200, null), "1,200.00");
});

test("what is left uncredited is the net less what has been credited off it", () => {
  const document = invoice({ netAmount: 1_000 });

  assert.equal(uncreditedAmount(document, []), 1_000);
  assert.equal(uncreditedAmount(document, [{ netAmount: 400 }]), 600);
  assert.equal(
    uncreditedAmount(document, [{ netAmount: 400 }, { netAmount: 600 }]),
    0
  );
  assert.equal(isFullyCredited(document, [{ netAmount: 1_000 }]), true);
});

test("a credit is capped at what is left, and blank means the remainder", () => {
  assert.equal(creditRefusal("Overcharged", "", 600), null);
  assert.equal(creditRefusal("Overcharged", "600", 600), null);
  assert.match(creditRefusal("Overcharged", "601", 600), /more than is left/);
  assert.match(creditRefusal("", "100", 600), /why this credit/);
  assert.match(creditRefusal("Overcharged", "0", 600), /not a credit/);
});

test("a verified registration elsewhere in the EU moves the tax, and an unverified one does not", () => {
  assert.equal(predictTreatment("GB", "NL", true), "ReverseCharge");
  assert.equal(predictTreatment("GB", "NL", false), "OutOfScope");

  assert.equal(predictTreatment("IE", "NL", false), "Standard");
  assert.equal(predictRate("IE", "NL", false), 21);
});

test("a domestic supply is charged at the supplier's own rate", () => {
  assert.equal(predictTreatment("GB", "GB", false), "Standard");
  assert.equal(predictRate("GB", "GB", false), 20);
});

test("a customer outside the jurisdictions is outside the scope", () => {
  assert.equal(predictTreatment("GB", "US", true), "OutOfScope");
  assert.equal(predictRate("GB", "US", true), 0);
});

test("verification is the pair, so changing either half clears it", () => {
  const profile = {
    taxIdentifier: "NL123456789B01",
    countryCode: "NL",
    taxIdentifierVerifiedAt: "2026-01-01T00:00:00Z",
  };

  assert.equal(
    wouldClearVerification(profile, {
      taxIdentifier: "nl 123456789 b01",
      countryCode: "NL",
    }),
    false
  );

  assert.equal(
    wouldClearVerification(profile, {
      taxIdentifier: "NL999999999B01",
      countryCode: "NL",
    }),
    true
  );

  assert.equal(
    wouldClearVerification(profile, {
      taxIdentifier: "NL123456789B01",
      countryCode: "DE",
    }),
    true
  );

  assert.equal(
    wouldClearVerification(
      { ...profile, taxIdentifierVerifiedAt: null },
      { taxIdentifier: "NL999999999B01", countryCode: "NL" }
    ),
    false
  );
});

test("a registration on file and unconfirmed is a state of its own, not a weaker verified", () => {
  assert.equal(
    registrationState({ taxIdentifier: null, taxIdentifierVerifiedAt: null }),
    "none"
  );
  assert.equal(
    registrationState({
      taxIdentifier: "NL123456789B01",
      taxIdentifierVerifiedAt: null,
    }),
    "unverified"
  );
  assert.equal(
    registrationState({
      taxIdentifier: "NL123456789B01",
      taxIdentifierVerifiedAt: "2026-01-01T00:00:00Z",
    }),
    "verified"
  );
});

test("the two prefixes must differ, and both are upper-case and short", () => {
  assert.equal(prefixesRefusal("INV", "CRN"), null);
  assert.match(prefixesRefusal("INV", "INV"), /own prefix/);
  assert.match(prefixesRefusal("inv", "CRN"), /upper-case/);
  assert.match(prefixesRefusal("INVOICES1", "CRN"), /at most 8/);
  assert.match(prefixesRefusal("", "CRN"), /required/);
});

test("an entity in a jurisdiction the platform cannot decide tax for is refused on the form", () => {
  const entity = {
    legalName: "Platform Ltd",
    address: "1 Example Street\nLondon",
    countryCode: "GB",
    taxIdentifier: "GB123456789",
    invoiceNumberPrefix: "INV",
    creditNoteNumberPrefix: "CRN",
  };

  assert.equal(entityRefusal(entity), null);
  assert.match(
    entityRefusal({ ...entity, countryCode: "US" }),
    /decide tax for/
  );
});

const exchangeRate = (effectiveFrom, value, createdByUserId = null) => ({
  exchangeRateId: `rate-${effectiveFrom}`,
  fromCurrency: "GBP",
  toCurrency: "EUR",
  rate: value,
  effectiveFrom,
  createdByUserId,
  createdAt: effectiveFrom,
});

test("the rate in force is the latest one effective at or before the instant", () => {
  const rates = [
    exchangeRate("2026-01-01T00:00:00Z", 1.1),
    exchangeRate("2026-02-01T00:00:00Z", 1.2),
    exchangeRate("2026-03-01T00:00:00Z", 1.3),
  ];

  assert.equal(rateInForce(rates, at("2026-02-15T00:00:00Z")).rate, 1.2);
  assert.equal(rateInForce(rates, at("2026-02-01T00:00:00Z")).rate, 1.2);
  assert.equal(rateInForce(rates, at("2025-12-31T00:00:00Z")), null);
});

test("a month is converted at the rate in force at the end of it", () => {
  const rates = [
    exchangeRate("2026-01-01T00:00:00Z", 1.1),
    exchangeRate("2026-01-20T00:00:00Z", 1.2),
    exchangeRate("2026-02-05T00:00:00Z", 1.3),
  ];

  // January's invoice takes the 20th's rate, not February's and not the 1st's.
  assert.equal(rateForMonth(rates, 1, 2026).rate, 1.2);
  assert.equal(rateForMonth(rates, 2, 2026).rate, 1.3);
});

test("a pair's history reads newest first, and a fed rate is told from a typed one", () => {
  const rates = [
    exchangeRate("2026-01-01T00:00:00Z", 1.1),
    exchangeRate("2026-02-01T00:00:00Z", 1.2, "user-1"),
  ];

  const [pair] = ratesByPair(rates);

  assert.equal(pair.rates[0].rate, 1.2);
  assert.equal(pair.rates[0].createdByUserId, "user-1");
  assert.equal(pair.rates[1].createdByUserId, null);
});
