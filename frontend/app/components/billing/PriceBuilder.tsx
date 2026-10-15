"use client";

import { useState } from "react";
import { Plus, TriangleAlert, X } from "lucide-react";

import type { BillableResource, CurrencyCode, PricingModel } from "@/types";
import {
  CURRENCY_CODES,
  CURRENCY_IS_PART_OF_THE_PRICE,
  CURRENCY_LABELS,
} from "@/lib/currency";
import {
  BILLABLE_RESOURCE_HINTS,
  BILLABLE_RESOURCE_LABELS,
  BANDS_MEET_EXACTLY,
  BANDS_START_AT_ZERO,
  CATCH_ALL_HINT,
  CATCH_ALL_LABEL,
  FLAT_AMOUNT_HINT,
  FLAT_DISCARDS_BANDS,
  FREE_ALLOWANCE_HINT,
  GRADUATED_VERSUS_VOLUME,
  MINIMUM_CHARGE_HINT,
  ORDER_OF_OPERATIONS,
  PLATFORM_FEE_HINT,
  PREVIEW_IS_NOT_AN_INVOICE,
  PRICING_MODEL_HINTS,
  PRICING_MODEL_LABELS,
  type PriceDraft,
  type StructureDraft,
  TOP_BAND_IS_UNBOUNDED,
  bandSpan,
  describeBillableResource,
  draftBand,
  draftPrice,
  draftVersion,
  formatAmount,
  newPriceDraft,
  nextBand,
  previewLines,
  priceRefusal,
  structureRefusal,
  totalOf,
  unpricedResources,
} from "@/lib/pricing";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTranslation } from "@/context/LocalizationContext";

const DEFAULT_PREVIEW_QUANTITY = "12000";

const MODELS: PricingModel[] = ["Flat", "Graduated", "Volume"];

const CATCH_ALL_VALUE = "__catch_all__";

export function PriceBuilder({
  draft,
  onChange,
  disabled = false,
}: {
  draft: StructureDraft;
  onChange: (next: StructureDraft) => void;
  disabled?: boolean;
}) {
  const t = useTranslation();
  const [previewQuantity, setPreviewQuantity] = useState(
    DEFAULT_PREVIEW_QUANTITY
  );
  const [previewResource, setPreviewResource] =
    useState<BillableResource>("RuleEvaluation");

  const [flatWarning, setFlatWarning] = useState<number | null>(null);

  const update = (index: number, next: PriceDraft) =>
    onChange({
      ...draft,
      prices: draft.prices.map((price, at) => (at === index ? next : price)),
    });

  const available = unpricedResources(
    draft.prices.map((price) => ({
      billableResource: price.billableResource,
      pricingModel: price.pricingModel,
      freeAllowance: 0,
      tiers: [],
    }))
  );

  const hasCatchAll = draft.prices.some(
    (price) => price.billableResource === null
  );

  const refusal = structureRefusal(draft);

  const quantity = Math.max(0, Math.floor(Number(previewQuantity) || 0));
  const lines = previewLines({
    version: draftVersion(draft),
    quantities: { [previewResource]: quantity },
  });

  return (
    <div className="space-y-4">
      <div className="space-y-1.5">
        <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
          {t("common.currency")}
        </label>
        <Select
          value={draft.currency}
          onValueChange={(value) =>
            onChange({ ...draft, currency: value as CurrencyCode })
          }
          disabled={disabled}
        >
          <SelectTrigger className="sm:w-72">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {CURRENCY_CODES.map((currency) => (
              <SelectItem key={currency} value={currency}>
                {CURRENCY_LABELS[currency]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <p className="text-xs text-slate-500">
          {CURRENCY_IS_PART_OF_THE_PRICE}
        </p>
      </div>

      <div className="grid gap-3 sm:grid-cols-2">
        <div className="space-y-1.5">
          <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("billing.platformFee")}
          </label>
          <Input
            value={draft.platformFee}
            onChange={(event) =>
              onChange({ ...draft, platformFee: event.target.value })
            }
            inputMode="decimal"
            placeholder="0"
            className="font-mono"
            disabled={disabled}
          />
          <p className="text-xs text-slate-500">{PLATFORM_FEE_HINT}</p>
        </div>

        <div className="space-y-1.5">
          <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("billing.monthlyMinimum")}
          </label>
          <Input
            value={draft.minimumCharge}
            onChange={(event) =>
              onChange({ ...draft, minimumCharge: event.target.value })
            }
            inputMode="decimal"
            placeholder="0"
            className="font-mono"
            disabled={disabled}
          />
          <p className="text-xs text-slate-500">{MINIMUM_CHARGE_HINT}</p>
        </div>
      </div>

      <div className="space-y-3">
        {draft.prices.map((price, index) => {
          const priceRefused = priceRefusal(draftPrice(price));

          return (
            <div
              key={index}
              className="space-y-3 rounded-lg border border-slate-200 bg-card p-3"
            >
              <div className="flex flex-wrap items-start gap-3">
                <div className="min-w-[200px] flex-1 space-y-1.5">
                  <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                    {t("billing.prices")}
                  </label>
                  <Select
                    value={price.billableResource ?? CATCH_ALL_VALUE}
                    onValueChange={(next) =>
                      update(index, {
                        ...price,
                        billableResource:
                          next === CATCH_ALL_VALUE
                            ? null
                            : (next as BillableResource),
                      })
                    }
                    disabled={disabled}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {(!hasCatchAll || price.billableResource === null) && (
                        <SelectItem value={CATCH_ALL_VALUE}>
                          {CATCH_ALL_LABEL}
                        </SelectItem>
                      )}
                      {available
                        .concat(
                          price.billableResource !== null
                            ? [price.billableResource]
                            : []
                        )
                        .map((resource) => (
                          <SelectItem key={resource} value={resource}>
                            {BILLABLE_RESOURCE_LABELS[resource]}
                          </SelectItem>
                        ))}
                    </SelectContent>
                  </Select>
                  <p className="text-xs text-slate-500">
                    {price.billableResource === null
                      ? CATCH_ALL_HINT
                      : BILLABLE_RESOURCE_HINTS[price.billableResource]}
                  </p>
                </div>

                <div className="min-w-[180px] flex-1 space-y-1.5">
                  <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                    {t("billing.priced")}
                  </label>
                  <div className="flex flex-wrap gap-1.5">
                    {MODELS.map((model) => (
                      <button
                        key={model}
                        type="button"
                        disabled={disabled}
                        onClick={() => {
                          if (model === "Flat" && price.tiers.length > 1) {
                            setFlatWarning(index);
                            return;
                          }
                          setFlatWarning(null);
                          update(index, { ...price, pricingModel: model });
                        }}
                        className={cn(
                          "rounded-full border px-3 py-1 font-mono text-xs transition-colors",
                          price.pricingModel === model
                            ? "border-brand-800 bg-brand-50 text-brand-900"
                            : "border-slate-200 bg-card text-slate-600 hover:bg-slate-50"
                        )}
                      >
                        {PRICING_MODEL_LABELS[model]}
                      </button>
                    ))}
                  </div>
                  <p className="text-xs text-slate-500">
                    {PRICING_MODEL_HINTS[price.pricingModel]}
                  </p>
                </div>

                {draft.prices.length > 0 && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="mt-5 h-8 w-8 text-slate-400 hover:text-critical-700"
                    disabled={disabled}
                    onClick={() =>
                      onChange({
                        ...draft,
                        prices: draft.prices.filter((_, at) => at !== index),
                      })
                    }
                  >
                    <X className="size-4" />
                  </Button>
                )}
              </div>

              {flatWarning === index && (
                <div className="space-y-2 rounded-lg border border-attention-200 bg-attention-50 p-3">
                  <p className="text-xs text-attention-700">
                    {FLAT_DISCARDS_BANDS}
                  </p>
                  <div className="flex gap-2">
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => {
                        update(index, {
                          ...price,
                          pricingModel: "Flat",
                          tiers: [
                            { ...price.tiers[0], toQuantity: "" },
                          ],
                        });
                        setFlatWarning(null);
                      }}
                    >
                      {t("billing.keepTheFirstBandOnly")}
                    </Button>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={() => setFlatWarning(null)}
                    >
                      {t("billing.leaveItBanded")}
                    </Button>
                  </div>
                </div>
              )}

              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("billing.freeAllowance")}
                </label>
                <Input
                  value={price.freeAllowance}
                  onChange={(event) =>
                    update(index, {
                      ...price,
                      freeAllowance: event.target.value,
                    })
                  }
                  inputMode="numeric"
                  placeholder="0"
                  className="w-40 font-mono"
                  disabled={disabled}
                />
                <p className="text-xs text-slate-500">{FREE_ALLOWANCE_HINT}</p>
              </div>

              <div className="space-y-2">
                <div className="grid grid-cols-[1fr_1fr_1fr_1fr_auto] gap-2 font-mono text-[11px] uppercase tracking-widest text-slate-400">
                  <span>{t("common.from")}</span>
                  <span>To</span>
                  <span>{t("billing.each")}</span>
                  <span>{t("billing.bandFee")}</span>
                  <span className="w-8" />
                </div>

                {price.tiers.map((band, bandIndex) => {
                  const last = bandIndex === price.tiers.length - 1;

                  return (
                    <div
                      key={bandIndex}
                      className="grid grid-cols-[1fr_1fr_1fr_1fr_auto] items-center gap-2"
                    >
                      <Input
                        value={band.fromQuantity}
                        onChange={(event) =>
                          update(index, {
                            ...price,
                            tiers: price.tiers.map((row, at) =>
                              at === bandIndex
                                ? { ...row, fromQuantity: event.target.value }
                                : row
                            ),
                          })
                        }
                        inputMode="numeric"
                        className="font-mono text-xs"
                        disabled={disabled}
                      />
                      <Input
                        value={band.toQuantity}
                        onChange={(event) =>
                          update(index, {
                            ...price,
                            tiers: price.tiers.map((row, at) =>
                              at === bandIndex
                                ? { ...row, toQuantity: event.target.value }
                                : row
                            ),
                          })
                        }
                        inputMode="numeric"
                        placeholder={last ? "no limit" : ""}
                        className="font-mono text-xs"
                        disabled={disabled}
                      />
                      <Input
                        value={band.unitAmount}
                        onChange={(event) =>
                          update(index, {
                            ...price,
                            tiers: price.tiers.map((row, at) =>
                              at === bandIndex
                                ? { ...row, unitAmount: event.target.value }
                                : row
                            ),
                          })
                        }
                        inputMode="decimal"
                        placeholder="0"
                        className="font-mono text-xs"
                        disabled={disabled}
                      />
                      <Input
                        value={band.flatAmount}
                        onChange={(event) =>
                          update(index, {
                            ...price,
                            tiers: price.tiers.map((row, at) =>
                              at === bandIndex
                                ? { ...row, flatAmount: event.target.value }
                                : row
                            ),
                          })
                        }
                        inputMode="decimal"
                        placeholder="0"
                        className="font-mono text-xs"
                        disabled={disabled}
                      />
                      {price.tiers.length > 1 ? (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-slate-400 hover:text-critical-700"
                          disabled={disabled}
                          onClick={() =>
                            update(index, {
                              ...price,
                              tiers: price.tiers.filter(
                                (_, at) => at !== bandIndex
                              ),
                            })
                          }
                        >
                          <X className="size-3.5" />
                        </Button>
                      ) : (
                        <span className="w-8" />
                      )}
                    </div>
                  );
                })}

                <div className="flex flex-wrap items-center gap-2">
                  {price.pricingModel !== "Flat" && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      disabled={disabled}
                      onClick={() =>
                        update(index, {
                          ...price,
                          tiers: [
                            ...price.tiers.map((row, at) =>
                              at === price.tiers.length - 1 &&
                              row.toQuantity.trim().length === 0
                                ? {
                                    ...row,
                                    toQuantity: nextBand(price.tiers)
                                      .fromQuantity,
                                  }
                                : row
                            ),
                            nextBand(price.tiers),
                          ],
                        })
                      }
                    >
                      <Plus className="size-3.5" />
                      {t("billing.band")}
                    </Button>
                  )}

                  <p className="text-xs text-slate-400">
                    {price.pricingModel === "Flat"
                      ? PRICING_MODEL_HINTS.Flat
                      : `${BANDS_START_AT_ZERO} ${BANDS_MEET_EXACTLY} ${TOP_BAND_IS_UNBOUNDED}`}{" "}
                    {FLAT_AMOUNT_HINT}
                  </p>
                </div>
              </div>

              {priceRefused !== null && (
                <p className="flex items-start gap-2 rounded-lg border border-critical-200 bg-critical-50 p-2 text-xs text-critical-700">
                  <TriangleAlert className="mt-0.5 size-3.5 flex-shrink-0" />
                  {priceRefused}
                </p>
              )}
            </div>
          );
        })}

        <div className="flex flex-wrap items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5"
            disabled={disabled || (hasCatchAll && available.length === 0)}
            onClick={() =>
              onChange({
                ...draft,
                prices: [
                  ...draft.prices,
                  newPriceDraft(hasCatchAll ? (available[0] ?? null) : null),
                ],
              })
            }
          >
            <Plus className="size-3.5" />
            {t("billing.priceAnotherKindOfWork")}
          </Button>
          <p className="text-xs text-slate-400">{GRADUATED_VERSUS_VOLUME}</p>
        </div>
      </div>

      {refusal !== null && draft.prices.length > 1 && (
        <p className="flex items-start gap-2 rounded-lg border border-critical-200 bg-critical-50 p-3 text-xs text-critical-700">
          <TriangleAlert className="mt-0.5 size-3.5 flex-shrink-0" />
          {refusal}
        </p>
      )}

      <div className="space-y-3 rounded-lg border border-elevated-200 bg-elevated-50 p-3">
        <div className="flex flex-wrap items-end gap-3">
          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("billing.ifAMonthMetered")}
            </label>
            <Input
              value={previewQuantity}
              onChange={(event) => setPreviewQuantity(event.target.value)}
              inputMode="numeric"
              className="w-32 font-mono"
            />
          </div>

          <div className="min-w-[200px] flex-1 space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              of
            </label>
            <Select
              value={previewResource}
              onValueChange={(next) =>
                setPreviewResource(next as BillableResource)
              }
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {(
                  [
                    "RuleEvaluation",
                    "RuleSetEvaluation",
                    "RuleTest",
                    "Backtest",
                    "SandboxWrite",
                    "Unclassified",
                  ] as BillableResource[]
                ).map((resource) => (
                  <SelectItem key={resource} value={resource}>
                    {BILLABLE_RESOURCE_LABELS[resource]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        {lines.length === 0 ? (
          <p className="text-xs text-slate-500">
            Nothing would be charged for that. Either the quantity is inside an
            allowance, or this version prices nothing at all yet.
          </p>
        ) : (
          <>
            <ul className="divide-y divide-elevated-200/60 border-y border-elevated-200/60">
              {lines.map((line) => (
                <li
                  key={line.sortOrder}
                  className="flex flex-wrap items-baseline justify-between gap-x-4 py-1.5 text-xs"
                >
                  <span className="text-slate-600">{line.description}</span>
                  <span className="font-mono text-slate-900">
                    {formatAmount(line.amount, draft.currency)}
                  </span>
                </li>
              ))}
            </ul>

            <div className="flex items-baseline justify-between">
              <span className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.total")}
              </span>
              <span className="font-mono text-sm font-medium text-slate-900">
                {formatAmount(totalOf(lines), draft.currency)}
              </span>
            </div>
          </>
        )}

        <p className="text-xs text-slate-500">
          {ORDER_OF_OPERATIONS} {PREVIEW_IS_NOT_AN_INVOICE}
        </p>
      </div>
    </div>
  );
}

export const bandSpans = (price: PriceDraft): string[] =>
  price.tiers.map((band) => bandSpan(draftBand(band)));

export const priceName = (price: PriceDraft): string =>
  describeBillableResource(price.billableResource);
