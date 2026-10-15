"use client";

import type { PlanVersion } from "@/types";
import { formatRate } from "@/lib/plans";
import {
  CATCH_ALL_HINT,
  FREE_ALLOWANCE_HINT,
  MINIMUM_CHARGE_HINT,
  PLATFORM_FEE_HINT,
  PRICING_MODEL_HINTS,
  PRICING_MODEL_LABELS,
  bandRate,
  bandSpan,
  describeBillableResource,
  formatAmount,
  formatQuantity,
  hasPriceStructure,
  orderedBands,
  pricesInReadingOrder,
} from "@/lib/pricing";
import { useTranslation } from "@/context/LocalizationContext";

export function PriceStructure({
  version,
  hideHeadlineRate = false,
}: {
  version: Pick<
    PlanVersion,
    "currency" | "costPerRequest" | "platformFee" | "minimumCharge" | "prices"
  >;
  hideHeadlineRate?: boolean;
}) {
  const t = useTranslation();
  if (!hasPriceStructure(version)) {
    return (
      <p className="text-sm text-slate-500">
        One flat rate for everything metered:{" "}
        <span className="font-mono text-slate-700">
          {formatRate(version.costPerRequest, version.currency)} per request
        </span>
        . No platform fee, no monthly minimum, no allowance and no bands.
      </p>
    );
  }

  const prices = pricesInReadingOrder(version.prices);

  return (
    <div className="space-y-3">
      {(version.platformFee > 0 || version.minimumCharge > 0) && (
        <div className="grid gap-2 sm:grid-cols-2">
          {version.platformFee > 0 && (
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
              <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.platformFee")}
              </p>
              <p className="mt-1 font-mono text-sm text-slate-900">
                {formatAmount(version.platformFee, version.currency)} a month
              </p>
              <p className="mt-1 text-xs text-slate-500">{PLATFORM_FEE_HINT}</p>
            </div>
          )}

          {version.minimumCharge > 0 && (
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
              <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.monthlyMinimum")}
              </p>
              <p className="mt-1 font-mono text-sm text-slate-900">
                {formatAmount(version.minimumCharge, version.currency)}
              </p>
              <p className="mt-1 text-xs text-slate-500">
                {MINIMUM_CHARGE_HINT}
              </p>
            </div>
          )}
        </div>
      )}

      {prices.map((price) => {
        const bands = orderedBands(price.tiers);

        return (
          <div
            key={price.planPriceId}
            className="rounded-lg border border-slate-200 bg-card p-3"
          >
            <div className="flex flex-wrap items-baseline justify-between gap-2">
              <p className="text-sm font-medium text-slate-900">
                {describeBillableResource(price.billableResource)}
              </p>
              <span className="rounded-full border border-slate-200 bg-slate-50 px-2 py-0.5 font-mono text-xs text-slate-600">
                {PRICING_MODEL_LABELS[price.pricingModel]}
              </span>
            </div>

            <p className="mt-1 text-xs text-slate-500">
              {price.billableResource === null
                ? CATCH_ALL_HINT
                : PRICING_MODEL_HINTS[price.pricingModel]}
            </p>

            {price.freeAllowance > 0 && (
              <p className="mt-2 rounded border border-elevated-200 bg-elevated-50 p-2 text-xs text-elevated-700">
                First {formatQuantity(price.freeAllowance)} included.{" "}
                {FREE_ALLOWANCE_HINT}
              </p>
            )}

            <ul className="mt-2 divide-y divide-slate-100 border-t border-slate-100">
              {bands.map((band, index) => (
                <li
                  key={index}
                  className="flex flex-wrap items-baseline justify-between gap-x-4 py-1.5 font-mono text-xs"
                >
                  <span className="text-slate-500">{bandSpan(band)}</span>
                  <span className="text-slate-800">
                    {bandRate(band, version.currency)}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        );
      })}

      {!hideHeadlineRate && (
        <p className="text-xs text-slate-400">
          Quoted as {formatRate(version.costPerRequest, version.currency)} per
          request — the
          catch-all price&apos;s bottom band, restated so the plan can be given
          in one figure. The invoice is computed from the structure above.
        </p>
      )}
    </div>
  );
}
