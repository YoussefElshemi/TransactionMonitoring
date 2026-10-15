"use client";

import { AlertTriangle, RotateCcw } from "lucide-react";

import { HoldExpiryVerdict, Organization } from "@/types";
import {
  DEFAULT_HOLD_EXPIRY_VERDICT,
  DEFAULT_HOLD_WINDOW_MINUTES,
  formatHoldWindow,
  holdExpiryVerdictText,
} from "@/lib/decisions";
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

export const INHERIT = "__inherit__";

const VERDICT_OPTIONS: HoldExpiryVerdict[] = [
  "Release",
  "Block",
  "Expire",
  "Never",
];

export interface HoldPolicy {
  holdWindow: number | null;
  holdExpiryVerdict: string | null;
}

export interface HoldPolicyInheritance {
  organization: Pick<Organization, "holdWindow" | "holdExpiryVerdict"> | null;
}

const effectiveWindow = (
  policy: HoldPolicy,
  { organization }: HoldPolicyInheritance
): { minutes: number; from: string } => {
  if (policy.holdWindow !== null)
    return { minutes: policy.holdWindow, from: "this rule" };
  if (organization?.holdWindow != null)
    return { minutes: organization.holdWindow, from: "the organisation" };
  return { minutes: DEFAULT_HOLD_WINDOW_MINUTES, from: "the server default" };
};

const effectiveVerdict = (
  policy: HoldPolicy,
  { organization }: HoldPolicyInheritance
): { verdict: string; from: string } => {
  if (policy.holdExpiryVerdict !== null)
    return { verdict: policy.holdExpiryVerdict, from: "this rule" };
  if (organization?.holdExpiryVerdict != null)
    return {
      verdict: organization.holdExpiryVerdict,
      from: "the organisation",
    };
  return { verdict: DEFAULT_HOLD_EXPIRY_VERDICT, from: "the server default" };
};

export function HoldPolicyFields({
  value,
  onChange,
  inheritance,
  disabled = false,
  scope = "rule",
  errors,
}: {
  value: HoldPolicy;
  onChange: (next: HoldPolicy) => void;
  inheritance: HoldPolicyInheritance;
  disabled?: boolean;
  scope?: "rule" | "organization";
  errors?: Partial<Record<keyof HoldPolicy, string>>;
}) {
  const t = useTranslation();
  const isNever = value.holdExpiryVerdict === "Never";
  const inherited =
    value.holdWindow === null && value.holdExpiryVerdict === null;

  const window = effectiveWindow(value, inheritance);
  const verdict = effectiveVerdict(value, inheritance);
  const effectiveIsNever = verdict.verdict === "Never";

  const setVerdict = (next: string) =>
    onChange({
      ...value,
      holdExpiryVerdict: next === INHERIT ? null : next,
    });

  const setWindow = (next: number | null) =>
    onChange({ ...value, holdWindow: next });

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-end gap-4">
        <div className="min-w-56 max-w-sm flex-1 space-y-1.5">
          <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("holdPolicyFields.whenTheHoldExpires")}
          </label>
          <Select
            value={value.holdExpiryVerdict ?? INHERIT}
            onValueChange={setVerdict}
            disabled={disabled}
          >
            <SelectTrigger
              aria-invalid={errors?.holdExpiryVerdict ? true : undefined}
              className={
                errors?.holdExpiryVerdict
                  ? "border-critical-500 focus:ring-critical-500"
                  : undefined
              }
            >
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={INHERIT}>
                Inherit
                {scope === "rule"
                  ? " from the organisation"
                  : " the server default"}
              </SelectItem>
              {VERDICT_OPTIONS.map((option) => (
                <SelectItem key={option} value={option}>
                  {option}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {!isNever && (
          <div className="w-40 shrink-0 space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("holdPolicyFields.holdWindowMin")}
            </label>
            <Input
              type="number"
              min={1}
              placeholder={t("holdPolicyFields.inherit")}
              value={value.holdWindow ?? ""}
              disabled={disabled}
              aria-invalid={errors?.holdWindow ? true : undefined}
              className={
                errors?.holdWindow
                  ? "border-critical-500 focus-visible:ring-critical-500"
                  : undefined
              }
              onChange={(event) => {
                const raw = event.target.value;
                if (raw === "") return setWindow(null);
                setWindow(Math.max(1, Number(raw) || 1));
              }}
            />
          </div>
        )}

        {!inherited && !disabled && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="gap-1.5"
            onClick={() =>
              onChange({ holdWindow: null, holdExpiryVerdict: null })
            }
            title={t("holdPolicyFields.clearBothFieldsSoThis")}
          >
            <RotateCcw className="size-3.5" />
            {t("holdPolicyFields.resetToInherit")}
          </Button>
        )}
      </div>

      {(errors?.holdExpiryVerdict || errors?.holdWindow) && (
        <div className="space-y-0.5">
          {errors.holdExpiryVerdict && (
            <p className="text-xs text-critical-700">
              When the hold expires: {errors.holdExpiryVerdict}
            </p>
          )}
          {errors.holdWindow && (
            <p className="text-xs text-critical-700">
              Hold window: {errors.holdWindow}
            </p>
          )}
        </div>
      )}

      {value.holdExpiryVerdict !== null && (
        <p className="text-xs text-slate-500">
          {holdExpiryVerdictText(value.holdExpiryVerdict)}.
        </p>
      )}

      {isNever && (
        <div className="flex items-start gap-2 rounded-md border border-attention-200 bg-attention-50 px-3 py-2">
          <AlertTriangle className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
          <div className="text-xs text-attention-700">
            <p className="font-medium">
              {scope === "organization"
                ? "Holds across this organisation will never resolve on their own."
                : "Transactions held by this rule never resolve on their own."}
            </p>
            <p className="mt-0.5">
              They wait indefinitely for someone to work the case. Use this
              where a hold must not fail because an outside party was slow — an
              outstanding information request to the client, say — and not as a
              way to make holds last longer.
              {scope === "organization" &&
                " An individual rule can still set its own verdict to reinstate a deadline."}
            </p>
          </div>
        </div>
      )}

      {(value.holdWindow === null || value.holdExpiryVerdict === null) && (
        <p className="text-xs text-slate-500">
          {effectiveIsNever ? (
            <>
              In effect:{" "}
              <span className="font-medium text-slate-700">
                held indefinitely
              </span>
              , with no deadline (from {verdict.from}).
            </>
          ) : (
            <>
              In effect:{" "}
              <span className="font-medium text-slate-700">
                {verdict.verdict.toLowerCase()} after{" "}
                {formatHoldWindow(window.minutes)}
              </span>{" "}
              — verdict from {verdict.from}, window from {window.from}.
            </>
          )}
        </p>
      )}
    </div>
  );
}

export const holdPolicyPatch = (
  next: HoldPolicy,
  current: HoldPolicy
): Record<string, number | string | null> => {
  const patch: Record<string, number | string | null> = {};
  if (next.holdWindow !== current.holdWindow) {
    patch.holdWindow = next.holdWindow;
  }
  if (next.holdExpiryVerdict !== current.holdExpiryVerdict) {
    patch.holdExpiryVerdict = next.holdExpiryVerdict;
  }
  return patch;
};
