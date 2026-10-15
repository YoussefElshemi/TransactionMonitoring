import type {
  BillableResource,
  CurrencyCode,
  InvoiceLine,
  InvoiceLineType,
  PlanVersion,
  PricingModel,
} from "@/types";
import { documentMoney, formatMoney } from "@/lib/currency";
import { formatRate, rateSentence } from "@/lib/plans";

import { DOCUMENT_LOCALE, formatDecimal } from "@/lib/format";

export const BILLABLE_RESOURCE_LABELS: Record<BillableResource, string> = {
  Unclassified: "Metered usage",
  RuleEvaluation: "Rule evaluations",
  RuleSetEvaluation: "Rule set evaluations",
  RuleTest: "Rule tests",
  Backtest: "Backtested evaluations",
  SandboxWrite: "Sandbox writes",
};

export const BILLABLE_RESOURCE_HINTS: Record<BillableResource, string> = {
  Unclassified:
    "Metered before this was recorded. Not a data error and not something that can be corrected — a backtest and a rule evaluation both recorded only the rule's id, so there is nothing left to sort them by. These fall to the plan's catch-all price.",
  RuleEvaluation: "One rule evaluated against one transaction.",
  RuleSetEvaluation:
    "A whole rule set evaluated against one transaction — the rules inside it are not metered again individually.",
  RuleTest:
    "A rule run against a payload from the evaluate screen, without a transaction behind it.",
  Backtest:
    "A rule replayed over transactions already stored. Cheap per unit and enormous per run, which is why it is priced apart from live evaluation.",
  SandboxWrite:
    "A write against the isolated sandbox. Metered because it is served, even though nothing it produces is real.",
};

export const UNCLASSIFIED_HINT = "Metered before this was recorded";

export const BILLABLE_RESOURCE_ORDER: BillableResource[] = [
  "Unclassified",
  "RuleEvaluation",
  "RuleSetEvaluation",
  "RuleTest",
  "Backtest",
  "SandboxWrite",
];

export const CATCH_ALL_LABEL = "Everything else";

export const CATCH_ALL_HINT =
  "What the plan charges for any kind of work it does not price specifically. A version has at most one, and a version with only this behaves exactly like the single rate it replaced.";

export const describeBillableResource = (
  billableResource: BillableResource | null
): string =>
  billableResource === null
    ? CATCH_ALL_LABEL
    : BILLABLE_RESOURCE_LABELS[billableResource];

export const PRICING_MODEL_LABELS: Record<PricingModel, string> = {
  Flat: "Flat",
  Graduated: "Graduated",
  Volume: "Volume",
};

export const PRICING_MODEL_HINTS: Record<PricingModel, string> = {
  Flat: "One rate for every unit, however many there are. Exactly one band, because the arithmetic reads exactly one to find the rate.",
  Graduated:
    "Each unit is priced in the band it personally falls in. A quantity spanning three bands is charged three ways, and the customer can see where each edge was crossed.",
  Volume:
    "The whole quantity lands in one band and every unit is priced there, including the ones below the edge. Crossing an edge reprices what was already bought, not only what comes after it.",
};

export const GRADUATED_VERSUS_VOLUME =
  "Graduated prices each unit in the band it falls in; Volume finds the band the whole quantity lands in and prices every unit there. They are not variations of each other — at 12,000 units against bands breaking at 10,000 they reach different totals.";

export const FREE_ALLOWANCE_HINT =
  "Units given away before any band is reached. Consumed, not discounted: the allowance comes off the quantity, and what is left enters the bands from the bottom — so an allowance of 1,000 against a band breaking at 10,000 charges the 10,001st unit metered at the bottom rate, not the top one.";

export const PLATFORM_FEE_HINT =
  "Charged every month the plan is in force, whether anything was used or not. It is added to usage rather than replacing it.";

export const MINIMUM_CHARGE_HINT =
  "The floor. Usage counts towards it rather than being charged on top of it, so a customer who spends past it never sees it on their invoice at all — only the shortfall is ever added.";

export const FLAT_AMOUNT_HINT =
  "Charged once if any quantity reaches this band, on top of the per-unit amount. It rides on the band's own line rather than becoming a second one, because it is part of what that band costs.";

export const INVOICE_LINE_TYPE_LABELS: Record<InvoiceLineType, string> = {
  Usage: "Usage",
  PlatformFee: "Platform fee",
  MinimumTopUp: "Minimum top-up",
  Discount: "Discount",
  CommitmentDrawdown: "Commitment",
  Adjustment: "Adjustment",
  Tax: "Tax",
};

export const INVOICE_LINE_TYPE_HINTS: Record<InvoiceLineType, string> = {
  Usage: "Metered work, priced by the plan's bands.",
  PlatformFee: "The plan's standing monthly charge.",
  MinimumTopUp:
    "The shortfall between everything above and the plan's monthly minimum. It appears only when usage and fees together came to less than the floor.",
  Discount:
    "A standing reduction agreed with this customer, taken off what was owed at the point it appears. Discounts compound — each comes off what is left after the one before it.",
  CommitmentDrawdown:
    "Spent from money this customer promised up front. It reduces what they pay this month; the commitment itself was billed and collected elsewhere.",
  Adjustment:
    "A one-off agreed out of band, applied last so a goodwill credit reduces what the customer pays rather than quietly spending their prepaid balance.",
  Tax: "Charged on the net of everything above it — after the discounts, after the drawdown and after the adjustments — so nobody pays tax on money nobody asked them for. It is written down whatever it comes to: a reverse-charged or out-of-scope document still carries the line at zero, and its description is where it says why.",
};

export const ORDER_OF_OPERATIONS =
  "Usage and the platform fee are summed; the monthly minimum tops that sum up rather than being charged beside it; discounts come off what is left; a commitment draws down what survives the discounts; one-off adjustments land after that; and tax is charged last, on the net of all of it.";

export const PREVIEW_IS_NOT_AN_INVOICE =
  "A preview of the plan's own arithmetic, at the quantity above. The invoice is the authority: it is generated against what was actually metered, and against whatever discounts, commitments and adjustments this customer holds at the time.";

export interface Band {
  fromQuantity: number;
  toQuantity: number | null;
  unitAmount: number;
  flatAmount: number;
}

export interface Price {
  billableResource: BillableResource | null;
  pricingModel: PricingModel;
  freeAllowance: number;
  tiers: Band[];
}

export interface VersionShape {
  currency: CurrencyCode;
  platformFee: number;
  minimumCharge: number;
  prices: Price[];
}

export const BANDS_START_AT_ZERO =
  "Bands start at zero, or the first units metered are unpriced.";

export const BANDS_MEET_EXACTLY =
  "Each band ends where the next begins. A gap is a quantity nobody agreed a price for; an overlap is two prices for the same unit.";

export const TOP_BAND_IS_UNBOUNDED =
  "The last band never ends. A bounded top band is a quantity the plan stops pricing the moment a customer grows into it, and an unbounded band below the top makes every band above it unreachable.";

export const FLAT_HAS_ONE_BAND =
  "A flat price charges one rate for every unit, and the arithmetic reads exactly one band to find it. A second band would be a term that was written down, published, and then never charged.";

export const ONE_PRICE_PER_RESOURCE =
  "A version prices each kind of work once, and has at most one catch-all. Two prices for the same work would leave the arithmetic picking between them.";

export const FLAT_DISCARDS_BANDS =
  "Flat charges one rate for every unit, so only the first band would be kept. The rest would never be charged.";

export const orderedBands = (bands: readonly Band[]): Band[] =>
  [...bands].sort((a, b) => a.fromQuantity - b.fromQuantity);

export const priceRefusal = (price: Price): string | null => {
  const name = describeBillableResource(price.billableResource);
  const bands = orderedBands(price.tiers);

  if (bands.length === 0) return `${name} has no bands.`;

  if (!Number.isInteger(price.freeAllowance) || price.freeAllowance < 0) {
    return `${name} has a free allowance that is not a whole number of units, or is negative.`;
  }

  for (const band of bands) {
    if (!Number.isInteger(band.fromQuantity) || band.fromQuantity < 0) {
      return `${name} has a band starting at something that is not a whole number of units.`;
    }
    if (
      band.toQuantity !== null &&
      (!Number.isInteger(band.toQuantity) ||
        band.toQuantity <= band.fromQuantity)
    ) {
      return `${name} has a band that ends at or before it starts.`;
    }
    if (!Number.isFinite(band.unitAmount) || band.unitAmount < 0) {
      return `${name} has a band with a negative or missing rate.`;
    }
    if (!Number.isFinite(band.flatAmount) || band.flatAmount < 0) {
      return `${name} has a band with a negative or missing flat amount.`;
    }
  }

  if (bands[0].fromQuantity !== 0) {
    return `${name} does not start at zero, so the first units it meters are unpriced.`;
  }

  for (let index = 0; index < bands.length - 1; index++) {
    const band = bands[index];

    if (band.toQuantity === null) {
      return `${name} has an unbounded band below its top one, so the bands above it are unreachable.`;
    }

    if (band.toQuantity !== bands[index + 1].fromQuantity) {
      return `${name} has a break between ${band.toQuantity} and ${bands[index + 1].fromQuantity}, so quantities in between are unpriced.`;
    }
  }

  if (bands[bands.length - 1].toQuantity !== null) {
    return `${name} has a bounded top band, so any quantity above ${bands[bands.length - 1].toQuantity} is unpriced.`;
  }

  if (price.pricingModel === "Flat" && bands.length > 1) {
    return `${name} is flat but has ${bands.length} bands. A flat price charges one rate, so bands beyond the first would never be charged.`;
  }

  return null;
};

export const isValidPrice = (price: Price): boolean =>
  priceRefusal(price) === null;

export const pricesRefusal = (prices: readonly Price[]): string | null => {
  const named = prices.filter((price) => price.billableResource !== null);

  const duplicated = named.find(
    (price, index) =>
      named.findIndex(
        (other) => other.billableResource === price.billableResource
      ) !== index
  );

  if (duplicated) {
    return `${describeBillableResource(duplicated.billableResource)} is priced twice. A version prices each kind of work once.`;
  }

  if (prices.filter((price) => price.billableResource === null).length > 1) {
    return "There is more than one catch-all price. A version has at most one.";
  }

  for (const price of prices) {
    const refusal = priceRefusal(price);
    if (refusal !== null) return refusal;
  }

  return null;
};

export const areValidPrices = (prices: readonly Price[]): boolean =>
  pricesRefusal(prices) === null;

export const isValidMoney = (value: number): boolean =>
  Number.isFinite(value) && value >= 0;

export const unpricedResources = (
  prices: readonly Price[]
): BillableResource[] =>
  BILLABLE_RESOURCE_ORDER.filter(
    (resource) => !prices.some((price) => price.billableResource === resource)
  );

export const formatQuantity = (quantity: number): string =>
  formatDecimal(quantity, DOCUMENT_LOCALE, { maximumFractionDigits: 0 });

export const formatPercentage = (value: number): string =>
  `${value.toLocaleString("en-GB", {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  })}%`;

export const bandSpan = (band: Band): string =>
  band.toQuantity === null
    ? `${formatQuantity(band.fromQuantity + 1)} and above`
    : `${formatQuantity(band.fromQuantity + 1)} to ${formatQuantity(band.toQuantity)}`;

export const bandRate = (band: Band, currency: CurrencyCode): string =>
  band.flatAmount > 0
    ? `${formatRate(band.unitAmount, currency)} each, plus ${formatRate(band.flatAmount, currency)} for the band`
    : `${formatRate(band.unitAmount, currency)} each`;

export const bandSentence = (band: Band, currency: CurrencyCode): string =>
  `${bandSpan(band)}: ${bandRate(band, currency)}`;

// Rounded per line, half away from zero like the API. toFixed settles the float halfway case first.
export const roundMoney = (value: number): number => {
  const scaled = Number((Math.abs(value) * 100).toFixed(6));
  const rounded = Math.round(scaled) / 100;

  // `|| 0` avoids -0, which formats as "-0.00".
  return value < 0 ? -rounded || 0 : rounded;
};

export interface PreviewLine {
  invoiceLineType: InvoiceLineType;
  billableResource: BillableResource | null;
  description: string;
  quantity: number;
  unitAmount: number;
  amount: number;
  sortOrder: number;
}

export interface PreviewRequest {
  version: VersionShape;
  quantities: Partial<Record<BillableResource, number>>;
  billing?: { currency: CurrencyCode; conversionRate: number };
  tax?: { taxRate: number; description: string };
  discounts?: readonly {
    discountType: "Percentage" | "FixedAmount";
    value: number;
    description: string;
  }[];
  commitment?: { amount: number; drawn: number } | null;
  adjustments?: readonly { description: string; amount: number }[];
}

export const totalOf = (lines: readonly { amount: number }[]): number =>
  roundMoney(lines.reduce((sum, line) => sum + line.amount, 0));

export const findPrice = (
  version: VersionShape,
  billableResource: BillableResource
): Price | null =>
  version.prices.find((price) => price.billableResource === billableResource) ??
  version.prices.find((price) => price.billableResource === null) ??
  null;

export const unitsInBand = (band: Band, chargeable: number): number => {
  const upperEdge =
    band.toQuantity === null ? chargeable : Math.min(chargeable, band.toQuantity);

  return Math.max(0, upperEdge - band.fromQuantity);
};

export const bandContaining = (
  bands: readonly Band[],
  chargeable: number
): Band | null => {
  const lastUnit = chargeable - 1;

  return (
    bands.find(
      (band) =>
        band.fromQuantity <= lastUnit &&
        (band.toQuantity === null || lastUnit < band.toQuantity)
    ) ?? null
  );
};

export const previewLines = (request: PreviewRequest): PreviewLine[] => {
  const lines: PreviewLine[] = [];

  const currency = request.billing?.currency ?? request.version.currency;
  const conversionRate = request.billing?.conversionRate ?? 1;

  const push = (line: Omit<PreviewLine, "sortOrder">) => {
    lines.push({
      ...line,
      amount: roundMoney(line.amount),
      sortOrder: lines.length,
    });
  };

  for (const billableResource of BILLABLE_RESOURCE_ORDER) {
    const quantity = request.quantities[billableResource] ?? 0;
    if (quantity <= 0) continue;

    const price = findPrice(request.version, billableResource);
    const name = BILLABLE_RESOURCE_LABELS[billableResource];

    if (price === null) {
      push({
        invoiceLineType: "Usage",
        billableResource,
        description: `${name} (not priced by this plan)`,
        quantity,
        unitAmount: 0,
        amount: 0,
      });
      continue;
    }

    if (price.freeAllowance > 0) {
      push({
        invoiceLineType: "Usage",
        billableResource,
        description: `${name} - included allowance`,
        quantity: Math.min(quantity, price.freeAllowance),
        unitAmount: 0,
        amount: 0,
      });
    }

    const chargeable = Math.max(0, quantity - price.freeAllowance);
    if (chargeable === 0) continue;

    const bands = orderedBands(price.tiers);
    if (bands.length === 0) continue;

    const priced = (band: Band, units: number, description: string) => {
      const unitAmount = band.unitAmount * conversionRate;

      push({
        invoiceLineType: "Usage",
        billableResource,
        description,
        quantity: units,
        unitAmount,
        amount: units * unitAmount + band.flatAmount * conversionRate,
      });
    };

    if (price.pricingModel === "Flat") {
      priced(bands[0], chargeable, name);
      continue;
    }

    if (price.pricingModel === "Graduated") {
      for (const band of bands) {
        const units = unitsInBand(band, chargeable);
        if (units === 0) continue;

        priced(
          band,
          units,
          band.toQuantity === null
            ? `${name} - ${band.fromQuantity + 1} and above`
            : `${name} - ${band.fromQuantity + 1} to ${band.toQuantity}`
        );
      }
      continue;
    }

    const band = bandContaining(bands, chargeable);
    if (band !== null) priced(band, chargeable, name);
  }

  const platformFee = request.version.platformFee * conversionRate;

  if (platformFee > 0) {
    push({
      invoiceLineType: "PlatformFee",
      billableResource: null,
      description: "Platform fee",
      quantity: 1,
      unitAmount: platformFee,
      amount: platformFee,
    });
  }

  const minimumCharge = roundMoney(request.version.minimumCharge * conversionRate);

  if (minimumCharge > 0) {
    const shortfall = roundMoney(minimumCharge - totalOf(lines));

    if (shortfall > 0) {
      push({
        invoiceLineType: "MinimumTopUp",
        billableResource: null,
        description: `Monthly minimum top-up (minimum ${documentMoney(minimumCharge, currency)})`,
        quantity: 1,
        unitAmount: shortfall,
        amount: shortfall,
      });
    }
  }

  for (const discount of request.discounts ?? []) {
    const running = totalOf(lines);
    if (running <= 0) continue;

    const reduction =
      discount.discountType === "Percentage"
        ? roundMoney((running * discount.value) / 100)
        :
          Math.min(discount.value, running);

    if (reduction <= 0) continue;

    push({
      invoiceLineType: "Discount",
      billableResource: null,
      description: discount.description,
      quantity: 1,
      unitAmount: reduction,
      amount: -reduction,
    });
  }

  if (request.commitment) {
    const payable = totalOf(lines);
    const remaining = request.commitment.amount - request.commitment.drawn;
    const drawn = Math.min(remaining, payable);

    if (payable > 0 && drawn > 0) {
      push({
        invoiceLineType: "CommitmentDrawdown",
        billableResource: null,
        description: `Drawn from commitment (${documentMoney(remaining - drawn, currency)} of ${documentMoney(request.commitment.amount, currency)} remaining)`,
        quantity: 1,
        unitAmount: drawn,
        amount: -drawn,
      });
    }
  }

  for (const adjustment of request.adjustments ?? []) {
    push({
      invoiceLineType: "Adjustment",
      billableResource: null,
      description: adjustment.description,
      quantity: 1,
      unitAmount: Math.abs(adjustment.amount),
      amount: adjustment.amount,
    });
  }

  if (request.tax && lines.length > 0) {
    const tax = roundMoney((totalOf(lines) * request.tax.taxRate) / 100);

    push({
      invoiceLineType: "Tax",
      billableResource: null,
      description: request.tax.description,
      quantity: 1,
      unitAmount: tax,
      amount: tax,
    });
  }

  return lines;
};

export const previewTotal = (request: PreviewRequest): number =>
  totalOf(previewLines(request));

export const linesInOrder = (lines: readonly InvoiceLine[]): InvoiceLine[] =>
  [...lines].sort((a, b) => a.sortOrder - b.sortOrder);

export const totalsAgree = (invoice: {
  amount: number;
  lines: readonly InvoiceLine[];
}): boolean => Math.abs(totalOf(invoice.lines) - invoice.amount) < 0.005;

export const TOTAL_DISAGREES =
  "This invoice's total is not the sum of its lines. The lines are the working the total was reached by, so one of the two has changed since it was issued — quote the invoice id rather than either figure.";

export const NO_LINES_HEADLINE = "This invoice has no working";

export const NO_LINES_DETAIL =
  "It was issued before the arithmetic was recorded line by line, when a total was a rate times a count. The total is what the customer was billed; there is nothing further behind it to show.";

export const showsQuantity = (line: {
  invoiceLineType: InvoiceLineType;
}): boolean => line.invoiceLineType === "Usage";

export const isCredit = (line: { amount: number }): boolean => line.amount < 0;

export const ZERO_LINES_ARE_DELIBERATE =
  "Lines at zero are kept. They are either an included allowance or work this plan prices at nothing — a quantity that reached the meter and not the invoice is what a customer disputes, so it is written down rather than left out.";

export const formatAmount = (amount: number, currency: CurrencyCode): string =>
  formatMoney(amount, currency);

export const invoicePeriod = (invoice: {
  month: number;
  year: number;
}): string =>
  new Intl.DateTimeFormat(DOCUMENT_LOCALE, {
    month: "long",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(Date.UTC(invoice.year, invoice.month - 1, 1)));

export const isSingleFlatRate = (version: {
  platformFee: number;
  minimumCharge: number;
  prices: readonly Price[];
}): boolean => {
  if (version.platformFee > 0 || version.minimumCharge > 0) return false;
  if (version.prices.length === 0) return true;
  if (version.prices.length > 1) return false;

  const [price] = version.prices;

  return (
    price.billableResource === null &&
    price.freeAllowance === 0 &&
    price.tiers.length === 1 &&
    price.tiers[0].fromQuantity === 0 &&
    price.tiers[0].toQuantity === null &&
    price.tiers[0].flatAmount === 0
  );
};

export const bandCount = (version: { prices: readonly Price[] }): number =>
  version.prices.reduce((count, price) => count + price.tiers.length, 0);

export const pricesInReadingOrder = <
  T extends { billableResource: BillableResource | null },
>(
  prices: readonly T[]
): T[] =>
  [...prices].sort((a, b) => {
    if (a.billableResource === null) return 1;
    if (b.billableResource === null) return -1;

    return (
      BILLABLE_RESOURCE_ORDER.indexOf(a.billableResource) -
      BILLABLE_RESOURCE_ORDER.indexOf(b.billableResource)
    );
  });

export const bandsFromVersion = (version: PlanVersion): Price[] =>
  version.prices.map((price) => ({
    billableResource: price.billableResource,
    pricingModel: price.pricingModel,
    freeAllowance: price.freeAllowance,
    tiers: orderedBands(price.tiers).map((tier) => ({
      fromQuantity: tier.fromQuantity,
      toQuantity: tier.toQuantity,
      unitAmount: tier.unitAmount,
      flatAmount: tier.flatAmount,
    })),
  }));

export const priceSummary = (price: Price, currency: CurrencyCode): string => {
  const bands = orderedBands(price.tiers);
  const name = describeBillableResource(price.billableResource);

  const shape =
    price.pricingModel === "Flat" || bands.length === 1
      ? bands.length > 0
        ? bandRate(bands[0], currency)
        : "no bands"
      : `${PRICING_MODEL_LABELS[price.pricingModel].toLowerCase()} over ${bands.length} bands`;

  const allowance =
    price.freeAllowance > 0
      ? `, first ${formatQuantity(price.freeAllowance)} included`
      : "";

  return `${name}: ${shape}${allowance}`;
};

export const priceShapeSentence = (version: {
  currency: CurrencyCode;
  costPerRequest: number;
  platformFee: number;
  minimumCharge: number;
  prices: readonly Price[];
}): string => {
  if (isSingleFlatRate(version)) {
    return rateSentence(version.costPerRequest, version.currency);
  }

  const clauses: string[] = [];

  if (version.prices.length === 1) {
    clauses.push(
      priceSummary(pricesInReadingOrder(version.prices)[0], version.currency)
    );
  } else if (version.prices.length > 1) {
    clauses.push(
      `${version.prices.length} prices over ${bandCount(version)} bands`
    );
  }

  if (version.platformFee > 0) {
    clauses.push(`${formatAmount(version.platformFee, version.currency)} a month`);
  }

  if (version.minimumCharge > 0) {
    clauses.push(
      `minimum ${formatAmount(version.minimumCharge, version.currency)}`
    );
  }

  if (clauses.length === 0) return "Nothing is charged under this version";

  return clauses.join(", ");
};

export const priceShapeLabel = (version: {
  currency: CurrencyCode;
  costPerRequest: number;
  platformFee: number;
  minimumCharge: number;
  prices: readonly Price[];
}): string => {
  if (isSingleFlatRate(version)) {
    return rateSentence(version.costPerRequest, version.currency);
  }

  const parts: string[] = [];

  if (version.prices.length === 1) {
    const [price] = version.prices;
    const bands = orderedBands(price.tiers).length;

    parts.push(
      bands > 1
        ? `${PRICING_MODEL_LABELS[price.pricingModel]}, ${bands} bands`
        : PRICING_MODEL_LABELS[price.pricingModel]
    );
  } else if (version.prices.length > 1) {
    parts.push(`${version.prices.length} prices`);
  }

  if (version.platformFee > 0) parts.push("fee");
  if (version.minimumCharge > 0) parts.push("minimum");

  return parts.length === 0 ? "Free" : parts.join(" · ");
};

export const hasPriceStructure = (version: {
  platformFee: number;
  minimumCharge: number;
  prices: readonly Price[];
}): boolean =>
  version.prices.length > 0 ||
  version.platformFee > 0 ||
  version.minimumCharge > 0;

export interface BandDraft {
  fromQuantity: string;
  toQuantity: string;
  unitAmount: string;
  flatAmount: string;
}

export interface PriceDraft {
  billableResource: BillableResource | null;
  pricingModel: PricingModel;
  freeAllowance: string;
  tiers: BandDraft[];
}

export interface StructureDraft {
  currency: CurrencyCode;
  platformFee: string;
  minimumCharge: string;
  prices: PriceDraft[];
}

const draftNumber = (value: string): number => {
  const trimmed = value.trim();
  if (trimmed.length === 0) return 0;

  return Number(trimmed);
};

export const draftBand = (band: BandDraft): Band => ({
  fromQuantity: draftNumber(band.fromQuantity),
  toQuantity: band.toQuantity.trim().length === 0 ? null : Number(band.toQuantity),
  unitAmount: draftNumber(band.unitAmount),
  flatAmount: draftNumber(band.flatAmount),
});

export const draftPrice = (price: PriceDraft): Price => ({
  billableResource: price.billableResource,
  pricingModel: price.pricingModel,
  freeAllowance: draftNumber(price.freeAllowance),
  tiers: price.tiers.map(draftBand),
});

export const draftVersion = (draft: StructureDraft): VersionShape => ({
  currency: draft.currency,
  platformFee: draftNumber(draft.platformFee),
  minimumCharge: draftNumber(draft.minimumCharge),
  prices: draft.prices.map(draftPrice),
});

export const nextBand = (bands: readonly BandDraft[]): BandDraft => {
  const previous = bands[bands.length - 1];

  return {
    fromQuantity: previous?.toQuantity.trim().length
      ? previous.toQuantity
      : String(draftNumber(previous?.fromQuantity ?? "0") + 1000),
    toQuantity: "",
    unitAmount: "",
    flatAmount: "",
  };
};

export const newPriceDraft = (
  billableResource: BillableResource | null
): PriceDraft => ({
  billableResource,
  pricingModel: "Flat",
  freeAllowance: "",
  tiers: [
    { fromQuantity: "0", toQuantity: "", unitAmount: "", flatAmount: "" },
  ],
});

export const draftFromVersion = (version: PlanVersion): StructureDraft => ({
  currency: version.currency,
  platformFee: version.platformFee > 0 ? String(version.platformFee) : "",
  minimumCharge: version.minimumCharge > 0 ? String(version.minimumCharge) : "",
  prices: pricesInReadingOrder(version.prices).map((price) => ({
    billableResource: price.billableResource,
    pricingModel: price.pricingModel,
    freeAllowance: price.freeAllowance > 0 ? String(price.freeAllowance) : "",
    tiers: orderedBands(price.tiers).map((tier) => ({
      fromQuantity: String(tier.fromQuantity),
      toQuantity: tier.toQuantity === null ? "" : String(tier.toQuantity),
      unitAmount: String(tier.unitAmount),
      flatAmount: tier.flatAmount > 0 ? String(tier.flatAmount) : "",
    })),
  })),
});

export const structureRefusal = (draft: StructureDraft): string | null => {
  const version = draftVersion(draft);

  if (!isValidMoney(version.platformFee)) {
    return "The platform fee must be a number, and not negative.";
  }

  if (!isValidMoney(version.minimumCharge)) {
    return "The monthly minimum must be a number, and not negative.";
  }

  return pricesRefusal(version.prices);
};

export const pricesRequestBody = (draft: StructureDraft) =>
  draft.prices.map((price) => {
    const converted = draftPrice(price);

    return {
      billableResource: converted.billableResource,
      pricingModel: converted.pricingModel,
      freeAllowance: converted.freeAllowance,
      tiers: orderedBands(converted.tiers).map((band) => ({
        fromQuantity: band.fromQuantity,
        toQuantity: band.toQuantity,
        unitAmount: band.unitAmount,
        flatAmount: band.flatAmount,
      })),
    };
  });

// Mirrors PlanPriceFactory.HeadlineRate: with a catch-all, the API restates the headline
// from its bottom band, overriding the typed rate.
export const headlineRate = (
  costPerRequest: number,
  prices: readonly Price[]
): number => {
  const catchAll = prices.find((price) => price.billableResource === null);
  if (!catchAll) return costPerRequest;

  const [bottom] = orderedBands(catchAll.tiers);

  return bottom === undefined ? costPerRequest : bottom.unitAmount;
};

export const draftHeadlineRate = (
  costPerRequest: number,
  draft: StructureDraft
): number => headlineRate(costPerRequest, draftVersion(draft).prices);
