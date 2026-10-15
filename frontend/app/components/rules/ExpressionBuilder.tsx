"use client";

import { useMemo } from "react";

import {
  PartyMappingEntry,
  RuleExpressionDto,
  RuleFunction,
  SchemaField,
  SchemaFieldType,
  VelocityMetric,
  VelocityThreshold,
} from "@/types";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { SchemaFieldCombobox } from "@/components/SchemaFieldCombobox";
import {
  ExpressionForm,
  FUNCTIONS,
  FUNCTION_GROUPS,
  FUNCTION_SPEC,
  FunctionSpec,
  argumentType,
  describeExpression,
  fitsArgument,
  fitsOperand,
  newExpression,
  withFunction,
} from "@/lib/ruleExpression";
import { schemaPaths } from "@/lib/transactionSchema";
import { readTypedValue, writeTypedValue } from "@/lib/ruleOperators";
import { VelocitySubject } from "@/components/rules/VelocitySubject";
import { newVelocityCondition } from "@/lib/velocityProfile";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function ExpressionBuilder({
  expression,
  fields,
  mappings = null,
  velocityMetrics = null,
  velocityThresholds = null,
  velocityOffered = false,
  disabled,
  depth = 0,
  expects = null,
  onChange,
}: {
  expression: RuleExpressionDto;
  fields: SchemaField[] | null;
  mappings?: PartyMappingEntry[] | null;
  velocityMetrics?: VelocityMetric[] | null;
  velocityThresholds?: VelocityThreshold[] | null;
  velocityOffered?: boolean;
  disabled: boolean;
  depth?: number;
  expects?: SchemaFieldType | null;
  onChange: (next: RuleExpressionDto) => void;
}) {
  const t = useTranslation();
  const hasField = expression.field !== undefined && expression.field !== null;

  const form: ExpressionForm = expression.velocity
    ? "velocity"
    : expression.party
      ? "party"
      : hasField
        ? "field"
        : expression.function
          ? "function"
          : "value";

  const spec = expression.function
    ? FUNCTION_SPEC[String(expression.function)]
    : undefined;

  const paths = useMemo(() => (fields ? schemaPaths(fields) : []), [fields]);

  const unreadable = (candidate: FunctionSpec): boolean =>
    paths.length > 0 && !paths.some((path) => fitsArgument(candidate, path));

  const blocked = (candidate: FunctionSpec): boolean =>
    candidate.takes !== "number" &&
    candidate.function !== expression.function &&
    unreadable(candidate);

  const setForm = (next: ExpressionForm) => {
    if (next === form) return;
    if (next === "velocity") {
      onChange({ velocity: newVelocityCondition(mappings?.[0]?.role ?? "Remitter") });
      return;
    }

    const seeded = newExpression(next);

    if (next === "function") {
      const seed = FUNCTION_SPEC[String(seeded.function)];
      const usable = seed && blocked(seed)
        ? FUNCTIONS.find((candidate) => !blocked(candidate))
        : undefined;

      if (usable) {
        onChange(withFunction(seeded, usable.function));
        return;
      }
    }

    onChange(seeded);
  };

  return (
    <div
      className={cn(
        "min-w-0 space-y-2 rounded-md border border-slate-200 bg-slate-50/60 p-2",
        depth > 0 && "bg-card"
      )}
    >
      <div className="flex flex-wrap items-center gap-2">
        <Select
          value={form}
          onValueChange={(value) => setForm(value as ExpressionForm)}
          disabled={disabled}
        >
          <SelectTrigger className="h-8 w-auto min-w-[8rem] shrink-0 text-xs">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="field">{t("rules.field")}</SelectItem>
            <SelectItem value="value">{t("rules.number")}</SelectItem>
            <SelectItem value="function">{t("rules.function")}</SelectItem>
            {(velocityOffered || form === "velocity") && (
              <SelectItem value="velocity">{t("rules.window")}</SelectItem>
            )}
            {form === "party" && <SelectItem value="party">{t("rules.party")}</SelectItem>}
          </SelectContent>
        </Select>

        {form === "field" && (
          <SchemaFieldCombobox
            value={expression.field ?? ""}
            onChange={(field) => onChange({ field })}
            disabled={disabled}
            fields={fields}
            className="w-full min-w-[10rem] max-w-[14rem]"
            placeholder={t("rules.fieldPath")}
            filter={(path) =>
              fitsOperand(path) && (expects === null || path.type === expects)
            }
            emptyNote={
              expects
                ? `Nothing fits: the schema declares no single ${expects} field.`
                : "Nothing fits: every path the schema declares is a list or an object."
            }
          />
        )}

        {form === "party" && (
          <span className="flex flex-wrap items-center gap-2">
            <span className="rounded bg-slate-100 px-2 py-1 font-mono text-xs text-slate-700">
              {describeExpression(expression)}
            </span>
            <span className="text-xs text-slate-400">
              A party fact, computed over. Readable here, and edited by changing
              the form above — this builder does not compose one.
            </span>
          </span>
        )}

        {form === "velocity" && (
          <VelocitySubject
            velocity={expression.velocity!}
            mappings={mappings}
            metrics={velocityMetrics}
            thresholds={velocityThresholds}
            disabled={disabled}
            onChange={(velocity) => onChange({ velocity })}
          />
        )}

        {form === "value" && (
          <Input
            placeholder={t("rules.value")}
            value={writeTypedValue(
              expression.value,
              typeof expression.value !== "string"
            )}
            onChange={(event) =>
              onChange({ value: readTypedValue(event.target.value).value })
            }
            disabled={disabled}
            className="w-32 font-mono"
          />
        )}

        {form === "function" && (
          <Select
            value={String(expression.function ?? "")}
            onValueChange={(value) =>
              onChange(withFunction(expression, value as RuleFunction))
            }
            disabled={disabled}
          >
            <SelectTrigger className="h-8 w-auto min-w-[11rem] shrink-0 text-xs">
              <SelectValue placeholder={t("rules.function")} />
            </SelectTrigger>
            <SelectContent>
              {FUNCTION_GROUPS.map((group) => (
                <SelectGroup key={group.label}>
                  <SelectLabel>{group.label}</SelectLabel>
                  {group.functions.map((candidate) => (
                    <SelectItem
                      key={candidate.function}
                      value={candidate.function}
                      disabled={blocked(candidate)}
                      // Outside the mirrored text, or the trigger would read
                      // "Sum no field fits" once one is chosen.
                      hint={
                        unreadable(candidate) ? (
                          <span className="ml-2 text-xs text-attention-700">
                            no field fits
                          </span>
                        ) : null
                      }
                    >
                      {candidate.label}
                    </SelectItem>
                  ))}
                </SelectGroup>
              ))}
            </SelectContent>
          </Select>
        )}
      </div>

      {spec && (
        <>
          <p className="text-xs text-slate-400">{spec.hint}</p>

          <div className="space-y-2 border-l-2 border-slate-200 pl-2">
            {Array.from({ length: spec.arity }).map((_, index) => {
              const argument = expression.arguments?.[index] ?? { field: "" };

              return (
                <div key={index} className="space-y-1">
                  {spec.arity > 1 && (
                    <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                      {index === 0 ? "First" : "Second"}
                    </span>
                  )}
                  <ExpressionArgument
                    argument={argument}
                    fields={fields}
                    mappings={mappings}
                    velocityMetrics={velocityMetrics}
                    velocityThresholds={velocityThresholds}
                    velocityOffered={velocityOffered}
                    disabled={disabled}
                    depth={depth + 1}
                    aggregate={spec.isAggregate}
                    expects={argumentType(spec)}
                    onChange={(next) => {
                      const args = Array.from(
                        { length: spec.arity },
                        (_, i) =>
                          i === index
                            ? next
                            : (expression.arguments?.[i] ?? { field: "" })
                      );
                      onChange({ ...expression, arguments: args });
                    }}
                  />
                </div>
              );
            })}
          </div>
        </>
      )}

      {depth === 0 && (
        <p className="font-mono text-[11px] text-slate-400">
          {describeExpression(expression) || "—"}
        </p>
      )}
    </div>
  );
}

function ExpressionArgument({
  argument,
  fields,
  mappings,
  velocityMetrics,
  velocityThresholds,
  velocityOffered,
  disabled,
  depth,
  aggregate,
  expects,
  onChange,
}: {
  argument: RuleExpressionDto;
  fields: SchemaField[] | null;
  mappings: PartyMappingEntry[] | null;
  velocityMetrics: VelocityMetric[] | null;
  velocityThresholds: VelocityThreshold[] | null;
  velocityOffered: boolean;
  disabled: boolean;
  depth: number;
  aggregate: boolean;
  expects: SchemaFieldType | null;
  onChange: (next: RuleExpressionDto) => void;
}) {
  const t = useTranslation();
  if (aggregate) {
    return (
      <div className="space-y-1">
        <SchemaFieldCombobox
          value={argument.field ?? ""}
          onChange={(field) => onChange({ field })}
          disabled={disabled}
          fields={fields}
          className="w-full max-w-[16rem]"
          placeholder={t("rules.partiesAmount")}
          filter={(path) =>
            path.multiple && (expects === null || path.type === expects)
          }
          emptyNote={
            expects
              ? `Nothing fits: the schema declares no [*] path holding ${expects}s.`
              : "Nothing fits: the schema declares no array to aggregate."
          }
        />
        <p className="text-xs text-slate-400">
          {t("rules.mustBeAPathCarrying")} <span className="font-mono">[*]</span>
          {expects ? ` holding ${expects}s` : ""} — an aggregate reduces many
          values to one.
        </p>
      </div>
    );
  }

  return (
    <ExpressionBuilder
      expression={argument}
      fields={fields}
      mappings={mappings}
      velocityMetrics={velocityMetrics}
      velocityThresholds={velocityThresholds}
      velocityOffered={velocityOffered}
      disabled={disabled}
      depth={depth}
      expects={expects}
      onChange={onChange}
    />
  );
}
