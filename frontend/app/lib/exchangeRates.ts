import type { CurrencyCode, ExchangeRate } from "@/types";

export const RATES_ARE_APPENDED =
  "Rates are appended, never edited. Correcting one records the next from the instant it takes effect; every invoice already converted at the old one stays exactly as it was issued. There is no endpoint that changes a rate and there is not going to be one.";

export const EFFECTIVE_FROM_HINT =
  "The instant this rate takes over. Left blank it starts now. Dating it earlier is how a correction is made — the rate in force at any moment is the latest one effective at or before it, so a row dated before an existing one changes nothing that came after.";

export const MISSING_RATE_REFUSES_THE_INVOICE =
  "An invoice is refused when no rate was in force at the end of the month it covers, rather than being converted at whatever the rate happens to be today. A total reached from a number nobody recorded cannot be reproduced next year, and reproducing a closed month is the whole point of recording any of this.";

export const SAME_CURRENCY_MESSAGE =
  "A currency does not convert to itself. Pick two different ones — an invoice whose plan and customer share a currency is not converted at all, and carries no rate.";

export const isFedRate = (
  rate: Pick<ExchangeRate, "createdByUserId">
): boolean => rate.createdByUserId === null;

export const FED_RATE_LABEL = "Daily feed";

export const FED_RATE_HINT =
  "Appended overnight from the European Central Bank's published reference rates. Nobody typed it, and there is nobody to ask about it — a rate that looks wrong is corrected the same way any other is, by recording the next one.";

export const MANUAL_RATE_LABEL = "Recorded by hand";

export const MANUAL_RATE_HINT =
  "Entered by a person, and therefore deliberate — it was recorded either before the feed had a rate for that instant or to override what the feed had.";

export const pairLabel = (
  fromCurrency: CurrencyCode,
  toCurrency: CurrencyCode
): string => `${fromCurrency} → ${toCurrency}`;

export const pairKey = (
  rate: Pick<ExchangeRate, "fromCurrency" | "toCurrency">
): string => `${rate.fromCurrency}:${rate.toCurrency}`;

export const rateSentence = (rate: {
  fromCurrency: CurrencyCode;
  toCurrency: CurrencyCode;
  rate: number;
}): string =>
  `1 ${rate.fromCurrency} buys ${rate.rate.toLocaleString("en-GB", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 6,
  })} ${rate.toCurrency}`;

export const formatExchangeRate = (rate: number): string =>
  rate.toLocaleString("en-GB", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 6,
  });

export const ratesNewestFirst = (
  rates: readonly ExchangeRate[]
): ExchangeRate[] =>
  [...rates].sort(
    (a, b) => Date.parse(b.effectiveFrom) - Date.parse(a.effectiveFrom)
  );

export const ratesByPair = (
  rates: readonly ExchangeRate[]
): { key: string; fromCurrency: CurrencyCode; toCurrency: CurrencyCode; rates: ExchangeRate[] }[] => {
  const pairs = new Map<string, ExchangeRate[]>();

  for (const rate of rates) {
    const key = pairKey(rate);
    const existing = pairs.get(key);

    if (existing) existing.push(rate);
    else pairs.set(key, [rate]);
  }

  return [...pairs.entries()]
    .map(([key, group]) => ({
      key,
      fromCurrency: group[0].fromCurrency,
      toCurrency: group[0].toCurrency,
      rates: ratesNewestFirst(group),
    }))
    .sort((a, b) => a.key.localeCompare(b.key));
};

export const rateInForce = (
  rates: readonly ExchangeRate[],
  at: Date
): ExchangeRate | null => {
  const instant = at.getTime();

  return (
    ratesNewestFirst(rates).find(
      (rate) => Date.parse(rate.effectiveFrom) <= instant
    ) ?? null
  );
};

export const rateForMonth = (
  rates: readonly ExchangeRate[],
  month: number,
  year: number
): ExchangeRate | null =>
  // The last instant of the month, in UTC — `Date.UTC` with a day of zero on
  // the following month is the last day of this one.
  rateInForce(rates, new Date(Date.UTC(year, month, 0, 23, 59, 59, 999)));

export const isValidRate = (rate: number): boolean =>
  Number.isFinite(rate) && rate > 0;

export const RATE_REFUSAL =
  "A rate is a positive number — how many units of the second currency one unit of the first buys.";
