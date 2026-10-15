"use client";

import { useState } from "react";
import { X } from "lucide-react";

import { RuleBodyDto } from "@/types";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import {
  MAX_LIST_VALUES,
  MAX_PATTERN_LENGTH,
  ValueShape,
  readTypedValue,
  writeTypedValue,
} from "@/lib/ruleOperators";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function ConditionValue({
  shape,
  node,
  disabled,
  onChange,
}: {
  shape: ValueShape;
  node: RuleBodyDto;
  disabled: boolean;
  onChange: (updates: Partial<RuleBodyDto>) => void;
}) {
  const t = useTranslation();
  if (shape === "none") return null;

  if (shape === "list") {
    return (
      <ListValue
        values={Array.isArray(node.value) ? (node.value as unknown[]) : []}
        disabled={disabled}
        onChange={(value) => onChange({ value, json: true })}
      />
    );
  }

  if (shape === "pair") {
    return (
      <PairValue
        bounds={Array.isArray(node.value) ? (node.value as unknown[]) : []}
        disabled={disabled}
        onChange={(value) => onChange({ value, json: true })}
      />
    );
  }

  if (shape === "pattern") {
    return (
      <PatternValue
        value={typeof node.value === "string" ? node.value : ""}
        disabled={disabled}
        // A pattern is a string, always — never re-read as JSON, or a pattern
        // like `^\d+$` that happens to parse would be stored as something else.
        onChange={(value) => onChange({ value, json: false })}
      />
    );
  }

  if (shape === "text") {
    return (
      <Input
        placeholder={t("rules.text")}
        value={typeof node.value === "string" ? node.value : writeTypedValue(node.value, node.json)}
        onChange={(event) =>
          onChange({ value: event.target.value, json: false })
        }
        disabled={disabled}
        className="w-40"
      />
    );
  }

  return (
    <Input
      placeholder={t("rules.value")}
      value={writeTypedValue(node.value, node.json)}
      onChange={(event) => onChange(readTypedValue(event.target.value))}
      disabled={disabled}
      className="w-40"
    />
  );
}

function ListValue({
  values,
  disabled,
  onChange,
}: {
  values: unknown[];
  disabled: boolean;
  onChange: (values: unknown[]) => void;
}) {
  const t = useTranslation();
  const [pending, setPending] = useState("");

  const add = () => {
    const raw = pending.trim();
    if (!raw || values.length >= MAX_LIST_VALUES) return;
    onChange([...values, readTypedValue(raw).value]);
    setPending("");
  };

  return (
    <div className="w-full space-y-1.5">
      {values.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {values.map((value, index) => (
            <span
              key={index}
              className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-0.5 font-mono text-xs text-slate-700"
            >
              {typeof value === "string" ? value : JSON.stringify(value)}
              {!disabled && (
                <button
                  type="button"
                  aria-label={`Remove value ${index + 1}`}
                  onClick={() =>
                    onChange(values.filter((_, i) => i !== index))
                  }
                  className="text-slate-400 hover:text-critical-700"
                >
                  <X className="size-3" />
                </button>
              )}
            </span>
          ))}
        </div>
      )}

      {!disabled && (
        <div className="flex items-center gap-2">
          <Input
            placeholder={t("rules.addAValue")}
            value={pending}
            disabled={values.length >= MAX_LIST_VALUES}
            onChange={(event) => setPending(event.target.value)}
            onKeyDown={(event) => {
              if (event.key !== "Enter") return;
              // The row sits inside no form, but Enter here would still bubble
              // to whatever dialog is hosting the builder.
              event.preventDefault();
              add();
            }}
            className="w-48 font-mono"
          />
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={add}
            disabled={!pending.trim() || values.length >= MAX_LIST_VALUES}
          >
            {t("rules.add")}
          </Button>
        </div>
      )}

      {values.length === 0 && (
        <p className="text-xs text-attention-700">
          A list condition needs at least one value — an empty list can never
          match, so the API rejects it.
        </p>
      )}
    </div>
  );
}

function PairValue({
  bounds,
  disabled,
  onChange,
}: {
  bounds: unknown[];
  disabled: boolean;
  onChange: (bounds: unknown[]) => void;
}) {
  const t = useTranslation();
  const set = (index: 0 | 1, raw: string) => {
    const next: unknown[] = [bounds[0] ?? "", bounds[1] ?? ""];
    next[index] = readTypedValue(raw).value;
    onChange(next);
  };

  const [lower, upper] = bounds;
  const reversed =
    typeof lower === typeof upper &&
    (typeof lower === "number" || typeof lower === "string") &&
    (lower as number | string) > (upper as number | string);

  return (
    <div className="flex flex-wrap items-center gap-1.5">
      <Input
        placeholder={t("rules.lower")}
        value={writeTypedValue(bounds[0], typeof bounds[0] !== "string")}
        onChange={(event) => set(0, event.target.value)}
        disabled={disabled}
        className={cn("w-28 font-mono", reversed && "border-attention-500")}
      />
      <span className="font-mono text-xs text-slate-400">and</span>
      <Input
        placeholder={t("rules.upper")}
        value={writeTypedValue(bounds[1], typeof bounds[1] !== "string")}
        onChange={(event) => set(1, event.target.value)}
        disabled={disabled}
        className={cn("w-28 font-mono", reversed && "border-attention-500")}
      />
      {reversed && (
        <p className="w-full text-xs text-attention-700">
          The bounds are the wrong way round. A range from a higher value to a
          lower one can never match, so the API rejects it — put the lower bound
          first.
        </p>
      )}
    </div>
  );
}

function PatternValue({
  value,
  disabled,
  onChange,
}: {
  value: string;
  disabled: boolean;
  onChange: (value: string) => void;
}) {
  const t = useTranslation();
  const tooLong = value.length > MAX_PATTERN_LENGTH;
  const unsupported = /\(\?<?[=!]|\\\d/.test(value);

  return (
    <div className="w-full space-y-1">
      <Input
        placeholder={t("rules.aZ2D")}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        disabled={disabled}
        className={cn(
          "w-64 font-mono",
          (tooLong || unsupported) && "border-attention-500"
        )}
      />
      {tooLong && (
        <p className="text-xs text-attention-700">
          {value.length} characters — a pattern may be at most{" "}
          {MAX_PATTERN_LENGTH}.
        </p>
      )}
      {unsupported && !tooLong && (
        <p className="text-xs text-attention-700">
          This looks like it uses a lookaround or a backreference. Patterns run
          without backtracking, which rules both out — the API will reject it.
        </p>
      )}
    </div>
  );
}
