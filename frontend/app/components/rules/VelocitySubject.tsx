"use client";

import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from "react";
import { Check } from "lucide-react";

import {
  PartyMappingEntry,
  PartyRole,
  PARTY_ROLES,
  VelocityConditionDto,
  VelocityFunction,
  VelocityMetric,
  VelocityThreshold,
  VelocityWindowKind,
  VelocityWindowUnit,
  VELOCITY_FUNCTIONS,
  VELOCITY_WINDOW_UNITS,
} from "@/types";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { HintTip } from "@/components/rules/LeafParts";
import { declaredRoles, isManyParty } from "@/lib/partyMapping";
import {
  ANSWERS_ZERO_ON_EMPTY,
  ROLLING_MAX_LENGTH,
  VELOCITY_FUNCTION_HINT,
  VELOCITY_FUNCTION_LABEL,
  WINDOW_KIND_HINT,
  declares,
  isKnownTimeZone,
  structuringRange,
  thresholdValue,
  timeZones,
  velocityLeafName,
  withVelocityFunction,
  withWindowKind,
} from "@/lib/velocityProfile";
import { cn } from "@/lib/utils";

import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";

export function VelocitySubject({
  velocity,
  mappings,
  metrics,
  thresholds,
  disabled,
  tone = "default",
  onChange,
}: {
  velocity: VelocityConditionDto;
  mappings: PartyMappingEntry[] | null;
  metrics: VelocityMetric[] | null;
  thresholds: VelocityThreshold[] | null;
  disabled: boolean;
  tone?: "default" | "warning" | "critical";
  onChange: (velocity: VelocityConditionDto) => void;
}) {
  const t = useTranslation();
  const format = useFormat();
  const declaredRoleNames = declaredRoles(mappings);
  const declaredMetricNames = useMemo(
    () => (metrics ? metrics.map((metric) => metric.name) : null),
    [metrics]
  );
  const declaredThresholdNames = useMemo(
    () => (thresholds ? thresholds.map((threshold) => threshold.name) : null),
    [thresholds]
  );

  // Shadows the global `window` on purpose — inside this component the word
  // only ever means the velocity one, and spelling it anything else would make
  // every line below read worse to save a name nothing here uses.
  const window = velocity.window ?? { kind: "Rolling", unit: "Day", length: 1 };
  const kind = String(window.kind ?? "Rolling") as VelocityWindowKind;
  const unit = String(window.unit ?? "Day") as VelocityWindowUnit;
  const fn = String(velocity.function ?? "") as VelocityFunction;
  const structuring = fn === "StructuringCount";

  const entry = mappings?.find((mapping) => mapping.role === velocity.role);
  const many = Boolean(entry && isManyParty(entry));

  const setWindow = (updates: Partial<typeof window>) =>
    onChange({ ...velocity, window: { ...window, ...updates } });

  const named = thresholdValue(thresholds, velocity.threshold);
  const range =
    named !== null && typeof velocity.band === "number" && velocity.band > 0
      ? structuringRange(named, velocity.band)
      : null;

  return (
    <div
      className={cn(
        "min-w-0 flex-1 space-y-2 rounded-md border bg-slate-50/60 p-2",
        tone === "critical"
          ? "border-critical-200"
          : tone === "warning"
            ? "border-attention-200"
            : "border-slate-200"
      )}
    >
      <div className="flex flex-wrap items-center gap-1.5">
        <Select
          value={fn}
          onValueChange={(value) =>
            onChange(withVelocityFunction(velocity, value as VelocityFunction))
          }
          disabled={disabled}
        >
          <SelectTrigger className="h-8 w-auto min-w-[10rem] shrink-0 text-xs">
            <SelectValue placeholder={t("rules.aggregate")} />
          </SelectTrigger>
          <SelectContent>
            {VELOCITY_FUNCTIONS.map((candidate) => (
              <SelectItem key={candidate} value={candidate}>
                {VELOCITY_FUNCTION_LABEL[candidate]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {fn && VELOCITY_FUNCTION_HINT[fn] && (
          <HintTip label={`What ${VELOCITY_FUNCTION_LABEL[fn]} means`}>
            {VELOCITY_FUNCTION_HINT[fn]}
          </HintTip>
        )}

        <span aria-hidden className="font-mono text-xs text-slate-300">
          of
        </span>

        <DeclaredSelect
          value={String(velocity.role ?? "")}
          declared={declaredRoleNames}
          fallback={PARTY_ROLES as string[]}
          known={mappings !== null}
          placeholder={t("rules.whose")}
          width="min-w-[8rem]"
          disabled={disabled}
          onChange={(role) => onChange({ ...velocity, role: role as PartyRole })}
        />

        <span aria-hidden className="font-mono text-xs text-slate-300">
          ·
        </span>

        <DeclaredSelect
          value={velocity.metric ?? ""}
          declared={declaredMetricNames ?? []}
          fallback={[]}
          known={declaredMetricNames !== null}
          placeholder={t("rules.metric")}
          width="min-w-[10rem]"
          disabled={disabled}
          onChange={(metric) => onChange({ ...velocity, metric })}
        />

        {entry && many && (
          <span className="inline-flex shrink-0 items-center gap-1 rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-wide text-brand-800">
            many
            <HintTip label={t("rules.howAWindowOverMany")}>
              The mapping declares this role with a wildcard, so a payload can
              name several. The window is worked out{" "}
              <span className="font-medium">per party</span> and the quantifier
              is applied to the answers — so &ldquo;any of&rdquo; asks whether{" "}
              <span className="font-medium">one</span> of them ran the total up,
              not whether they did between them.
            </HintTip>
          </span>
        )}
      </div>

      <div className="flex flex-wrap items-center gap-1.5">
        <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
          over
        </span>

        <Select
          value={kind}
          // The whole window is replaced rather than merged into: switching to
          // Calendar has to *drop* the length, and merging would keep it and
          // send a body the API refuses.
          onValueChange={(value) =>
            onChange({
              ...velocity,
              window: withWindowKind(window, value as VelocityWindowKind),
            })
          }
          disabled={disabled}
        >
          <SelectTrigger className="h-8 w-auto min-w-[8rem] shrink-0 text-xs">
            <SelectValue placeholder={t("rules.window")} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Rolling">{t("rules.theLast")}</SelectItem>
            <SelectItem value="Calendar">{t("rules.this")}</SelectItem>
          </SelectContent>
        </Select>

        {kind === "Rolling" && (
          <Input
            type="number"
            min={1}
            max={ROLLING_MAX_LENGTH[unit]}
            placeholder="1"
            value={window.length ?? ""}
            disabled={disabled}
            onChange={(event) => {
              const raw = event.target.value.trim();
              setWindow({ length: raw === "" ? null : Number(raw) });
            }}
            className="h-8 w-20 font-mono text-xs"
          />
        )}

        <Select
          value={unit}
          onValueChange={(value) => setWindow({ unit: value as VelocityWindowUnit })}
          disabled={disabled}
        >
          <SelectTrigger className="h-8 w-auto min-w-[7rem] shrink-0 text-xs">
            <SelectValue placeholder={t("rules.unit")} />
          </SelectTrigger>
          <SelectContent>
            {VELOCITY_WINDOW_UNITS.map((candidate) => (
              <SelectItem key={candidate} value={candidate}>
                {candidate.toLowerCase()}
                {kind === "Rolling" && (window.length ?? 1) !== 1 ? "s" : ""}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <HintTip label={t("rules.howThisWindowReads")}>
          {WINDOW_KIND_HINT[kind] ?? WINDOW_KIND_HINT.Rolling}
          {kind === "Rolling" && (
            <>
              {" "}
              At most {format.count(ROLLING_MAX_LENGTH[unit] ?? 0)}{" "}
              {unit.toLowerCase()}s — a window cannot reach back further than
              about a year.
            </>
          )}
        </HintTip>

        {kind === "Calendar" && (
          <>
            <span aria-hidden className="font-mono text-xs text-slate-300">
              in
            </span>
            <TimeZoneCombobox
              value={window.timeZone ?? ""}
              disabled={disabled}
              onChange={(timeZone) =>
                setWindow({ timeZone: timeZone.trim() ? timeZone : null })
              }
            />
          </>
        )}
      </div>

      {kind === "Calendar" && unit === "Week" && (
        <p className="text-[11px] text-slate-400">
          Calendar weeks start on Monday, matching the ISO day numbering the
          expression functions already use.
        </p>
      )}
      {kind === "Calendar" && !window.timeZone?.trim() && (
        <p className="text-[11px] text-slate-400">
          No zone set, so the boundary falls at UTC midnight — which is not
          local midnight for most of the year in most places.
        </p>
      )}

      <IncludeCurrentChoice
        value={velocity.includeCurrent}
        disabled={disabled}
        onChange={(includeCurrent) => onChange({ ...velocity, includeCurrent })}
      />

      {structuring && (
        <div className="space-y-1.5 rounded-md border border-slate-200 bg-card p-2">
          <div className="flex flex-wrap items-center gap-1.5">
            <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              just under
            </span>
            <DeclaredSelect
              value={velocity.threshold ?? ""}
              declared={declaredThresholdNames ?? []}
              fallback={[]}
              known={declaredThresholdNames !== null}
              placeholder={t("rules.threshold")}
              width="min-w-[10rem]"
              disabled={disabled}
              onChange={(threshold) => onChange({ ...velocity, threshold })}
            />
            <span aria-hidden className="font-mono text-xs text-slate-300">
              by
            </span>
            <BandInput
              band={velocity.band ?? null}
              disabled={disabled}
              onChange={(band) => onChange({ ...velocity, band })}
            />
          </div>

          <p className="text-[11px] leading-snug text-slate-400">
            The threshold is a fact about your organisation&apos;s world and
            lives on the profile, shared by every rule that names it. The band is
            a judgement — how close counts as &ldquo;just under&rdquo; — and
            lives here, because two rules may legitimately disagree about it
            while only one of them is describing the law.
          </p>

          {range ? (
            <p className="text-[11px] leading-snug text-slate-500">
              Counts payments from{" "}
              <span className="font-mono font-medium text-slate-900">
                {format.count(range.from)}
              </span>{" "}
              up to but not including{" "}
              <span className="font-mono font-medium text-slate-900">
                {format.count(range.to)}
              </span>
              . A payment at exactly the threshold does not count — the figure a
              reporting duty starts at is the one a structurer is avoiding.
            </p>
          ) : (
            <p className="text-[11px] text-slate-400">
              Pick a threshold and a band and the range this counts will be
              worked out here.
            </p>
          )}
        </div>
      )}

      <p className="font-mono text-[11px] leading-snug text-slate-400">
        {velocityLeafName(velocity) || "—"}
      </p>

      {fn && (
        <p className="text-[11px] leading-snug text-slate-400">
          {ANSWERS_ZERO_ON_EMPTY.has(fn)
            ? "Over an empty window this answers 0, so a rule can be written about a party who has done nothing here."
            : "Over an empty window this answers nothing, and a leaf comparing nothing never matches — so this cannot fire on a party who has done nothing here."}
        </p>
      )}

      {declaredMetricNames &&
        velocity.metric &&
        !declares(declaredMetricNames, velocity.metric) && (
          <p className="text-[11px] text-attention-700">
            The velocity profile no longer declares{" "}
            <span className="font-mono">{velocity.metric}</span>. It is still
            shown because the rule says so — saving will be refused until it
            names a metric the profile declares.
          </p>
        )}
    </div>
  );
}

function DeclaredSelect({
  value,
  declared,
  fallback,
  known,
  placeholder,
  width,
  disabled,
  onChange,
}: {
  value: string;
  declared: string[];
  fallback: string[];
  known: boolean;
  placeholder: string;
  width: string;
  disabled: boolean;
  onChange: (value: string) => void;
}) {
  const t = useTranslation();
  const offered = [
    ...new Set<string>([...(value ? [value] : []), ...declared]),
  ];
  const options = offered.length > 0 ? offered : fallback;

  return (
    <Select
      value={value}
      onValueChange={onChange}
      disabled={disabled || options.length === 0}
    >
      <SelectTrigger className={cn("h-8 w-auto max-w-[18rem] shrink-0 text-xs", width)}>
        <SelectValue placeholder={
            options.length === 0 ? t("rules.noneDeclared") : placeholder
          } />
      </SelectTrigger>
      <SelectContent>
        {options.map((option) => (
          <SelectItem
            key={option}
            value={option}
            // Outside the mirrored text, or the trigger would read "amount not
            // declared" in a box sized for "amount".
            hint={
              known && !declared.includes(option) ? (
                <span className="ml-2 text-xs text-attention-700">
                  not declared
                </span>
              ) : null
            }
          >
            {option}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

function IncludeCurrentChoice({
  value,
  disabled,
  onChange,
}: {
  value: boolean | null | undefined;
  disabled: boolean;
  onChange: (value: boolean) => void;
}) {
  const unset = typeof value !== "boolean";

  const option = (choice: boolean, label: string, hint: string) => (
    <label
      className={cn(
        "flex cursor-pointer items-start gap-1.5 rounded-md border px-2 py-1.5 text-xs leading-snug transition-colors",
        value === choice
          ? "border-brand-200 bg-brand-50 text-brand-900"
          : "border-slate-200 bg-card text-slate-600 hover:border-slate-300",
        disabled && "cursor-not-allowed opacity-60"
      )}
    >
      <input
        type="radio"
        className="mt-0.5"
        checked={value === choice}
        disabled={disabled}
        onChange={() => onChange(choice)}
      />
      <span className="min-w-0">
        <span className="block font-medium">{label}</span>
        <span className="block text-[11px] text-slate-400">{hint}</span>
      </span>
    </label>
  );

  return (
    <div className="space-y-1">
      <div
        className={cn(
          "flex flex-wrap gap-1.5",
          unset && "rounded-md ring-1 ring-attention-200"
        )}
      >
        {option(
          true,
          "Count this payment towards the window",
          "Stops the payment that breaches the limit."
        )}
        {option(
          false,
          "Only count payments before this one",
          "Stops the one after — this payment is judged on their history."
        )}
      </div>
      {unset && (
        <p className="text-[11px] text-attention-700">
          Choose one. There is no default, and the API refuses a window that has
          not said which it means.
        </p>
      )}
    </div>
  );
}

function BandInput({
  band,
  disabled,
  onChange,
}: {
  band: number | null;
  disabled: boolean;
  onChange: (band: number | null) => void;
}) {
  const asPercent = (value: number): string =>
    String(Math.round(value * 100 * 1000) / 1000);

  const [text, setText] = useState<string>(band === null ? "" : asPercent(band));
  const pushed = useRef<number | null>(band);

  useEffect(() => {
    if (band === pushed.current) return;
    pushed.current = band;
    setText(band === null ? "" : asPercent(band));
  }, [band]);

  return (
    <span className="inline-flex items-center gap-1">
      <Input
        type="number"
        min={0}
        max={100}
        placeholder="20"
        value={text}
        disabled={disabled}
        onChange={(event) => {
          const raw = event.target.value;
          setText(raw);
          const parsed = raw.trim() === "" ? null : Number(raw) / 100;
          const next = parsed !== null && Number.isFinite(parsed) ? parsed : null;
          pushed.current = next;
          onChange(next);
        }}
        className="h-8 w-20 font-mono text-xs"
      />
      <span className="font-mono text-xs text-slate-400">%</span>
    </span>
  );
}

function TimeZoneCombobox({
  value,
  disabled,
  onChange,
}: {
  value: string;
  disabled: boolean;
  onChange: (value: string) => void;
}) {
  const t = useTranslation();
  const [open, setOpen] = useState(false);
  const [highlight, setHighlight] = useState(-1);
  const rowRefs = useRef<Record<string, HTMLLIElement | null>>({});

  const zones = useMemo(() => timeZones(), []);
  const needle = value.trim().toLowerCase();
  const matches = useMemo(
    () =>
      zones
        .filter((zone) => zone.toLowerCase().includes(needle))
        .slice(0, 50),
    [zones, needle]
  );

  const unknown = value.trim().length > 0 && !isKnownTimeZone(value);

  const pick = (zone: string) => {
    onChange(zone);
    setOpen(false);
    setHighlight(-1);
  };

  const handleKeyDown = (event: KeyboardEvent) => {
    if (event.key === "Escape" && open) {
      event.preventDefault();
      setOpen(false);
      return;
    }

    if (event.key === "Enter" && open && highlight >= 0 && matches[highlight]) {
      event.preventDefault();
      pick(matches[highlight]);
      return;
    }

    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;
    if (!matches.length) return;

    event.preventDefault();
    setOpen(true);

    const step = event.key === "ArrowDown" ? 1 : -1;
    const next =
      highlight === -1
        ? step === 1
          ? 0
          : matches.length - 1
        : (highlight + step + matches.length) % matches.length;

    setHighlight(next);
    rowRefs.current[matches[next]]?.scrollIntoView({ block: "nearest" });
  };

  return (
    <span className="relative">
      <Input
        placeholder={t("rules.utc")}
        value={value}
        disabled={disabled}
        onChange={(event) => {
          onChange(event.target.value);
          setOpen(true);
          setHighlight(-1);
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => setTimeout(() => setOpen(false), 200)}
        onKeyDown={handleKeyDown}
        title={value}
        className={cn(
          "h-8 w-[15rem] font-mono text-xs",
          unknown && "border-attention-500"
        )}
      />

      {open && matches.length > 0 && (
        <ul className="absolute left-0 z-20 mt-1 max-h-56 w-max min-w-full max-w-[22rem] overflow-y-auto rounded-md border border-slate-200 bg-card py-1 shadow-lg">
          {matches.map((zone, index) => (
            <li
              key={zone}
              ref={(node) => {
                rowRefs.current[zone] = node;
              }}
              // Ahead of the input's blur, so the pick lands before the list is
              // torn down.
              onMouseDown={() => pick(zone)}
              onMouseEnter={() => setHighlight(index)}
              className={cn(
                "flex cursor-pointer items-center gap-2 px-3 py-1.5",
                index === highlight && "bg-slate-100"
              )}
            >
              <span className="min-w-0 flex-1 truncate font-mono text-xs text-slate-900">
                {zone}
              </span>
              {zone === value && (
                <Check className="size-4 flex-shrink-0 text-brand-800" />
              )}
            </li>
          ))}
        </ul>
      )}
    </span>
  );
}
