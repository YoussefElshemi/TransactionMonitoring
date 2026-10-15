"use client";

import { ReactNode } from "react";
import {
  AlertTriangle,
  Check,
  ChevronDown,
  Gauge,
  Hash,
  Info,
  Sigma,
  Users,
} from "lucide-react";

import { SchemaFieldType } from "@/types";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function HintTip({
  children,
  label = "What this means",
}: {
  children: ReactNode;
  label?: string;
}) {
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <button
          type="button"
          aria-label={label}
          className="shrink-0 rounded text-slate-300 transition-colors hover:text-slate-500 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-200"
        >
          <Info className="size-3.5" />
        </button>
      </TooltipTrigger>
      <TooltipContent
        side="top"
        className="max-w-xs whitespace-normal font-sans text-xs leading-snug"
      >
        {children}
      </TooltipContent>
    </Tooltip>
  );
}

export type SubjectKind = "field" | "expression" | "party" | "velocity";

const SUBJECT_KINDS: {
  kind: SubjectKind;
  icon: typeof Hash;
  labelKey: string;
  hintKey: string;
}[] = [
  {
    kind: "field",
    icon: Hash,
    labelKey: "rules.subjectReadField",
    hintKey: "rules.subjectReadFieldHint",
  },
  {
    kind: "expression",
    icon: Sigma,
    labelKey: "rules.subjectComputeValue",
    hintKey: "rules.subjectComputeValueHint",
  },
  {
    kind: "party",
    icon: Users,
    labelKey: "rules.subjectAskParty",
    hintKey: "rules.subjectAskPartyHint",
  },
  {
    kind: "velocity",
    icon: Gauge,
    labelKey: "rules.subjectAggregateWindow",
    hintKey: "rules.subjectAggregateWindowHint",
  },
];

export function SubjectKindToggle({
  kind,
  disabled,
  partyOffered,
  velocityOffered,
  onChange,
}: {
  kind: SubjectKind;
  disabled: boolean;
  partyOffered: boolean;
  velocityOffered: boolean;
  onChange: (kind: SubjectKind) => void;
}) {
  const t = useTranslation();
  const offered = SUBJECT_KINDS.filter((candidate) =>
    candidate.kind === "party"
      ? partyOffered
      : candidate.kind === "velocity"
        ? velocityOffered
        : true
  );

  const active =
    SUBJECT_KINDS.find((candidate) => candidate.kind === kind) ??
    SUBJECT_KINDS[0];
  const ActiveIcon = active.icon;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild disabled={disabled}>
        <button
          type="button"
          aria-label={t("rules.thisConditionIs", t(active.labelKey))}
          className={cn(
            "flex h-9 shrink-0 items-center gap-1 rounded-md bg-slate-100 px-1.5 text-brand-800 transition-colors",
            "hover:bg-slate-200 focus:outline-none focus-visible:ring-2 focus-visible:ring-brand-200",
            "disabled:pointer-events-none disabled:opacity-50"
          )}
        >
          <ActiveIcon className="size-3.5" />
          <ChevronDown className="size-3 text-slate-400" />
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="start" className="w-80">
        <DropdownMenuLabel>{t("rules.whatThisConditionIsAbout")}</DropdownMenuLabel>
        {offered.map((candidate) => {
          const Icon = candidate.icon;
          const current = candidate.kind === kind;
          return (
            <DropdownMenuItem
              key={candidate.kind}
              onSelect={() => {
                if (!current) onChange(candidate.kind);
              }}
              className="items-start gap-2 py-2"
            >
              <Icon
                className={cn(
                  "mt-0.5",
                  current ? "text-brand-800" : "text-slate-400"
                )}
              />
              <span className="min-w-0 flex-1">
                <span className="flex items-center gap-1.5">
                  <span className="text-sm">{t(candidate.labelKey)}</span>
                  {/* No size class here: the menu item sizes every descendant
                      `svg` with `[&_svg]:size-4`, which outranks a utility on
                      the element itself — one would read as if it did
                      something. */}
                  {current && <Check className="text-brand-800" />}
                </span>
                <span className="mt-0.5 block whitespace-normal font-sans text-xs leading-snug text-slate-400">
                  {t(candidate.hintKey)}
                </span>
              </span>
            </DropdownMenuItem>
          );
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

export function TypeChip({
  type,
  multiple,
  isRequired,
}: {
  type: SchemaFieldType;
  multiple: boolean;
  isRequired: boolean;
}) {
  const t = useTranslation();
  return (
    <span className="inline-flex shrink-0 items-center gap-1 rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-wide text-slate-500">
      {type}
      {multiple && (
        <span className="text-brand-800" title={t("rules.thisPathResolvesToMany")}>
          many
        </span>
      )}
      {isRequired && (
        <span className="text-attention-700" title={t("rules.everyPayloadMustCarryIt")}>
          req
        </span>
      )}
    </span>
  );
}

export interface LeafMessage {
  tone: "critical" | "attention";
  body: ReactNode;
}

export function LeafMessages({ messages }: { messages: LeafMessage[] }) {
  if (messages.length === 0) return null;

  const critical = messages.some((message) => message.tone === "critical");

  return (
    <div
      className={cn(
        "w-full space-y-1 border-t pt-2",
        critical ? "border-critical-200" : "border-attention-200"
      )}
    >
      {messages.map((message, index) => (
        <div
          key={index}
          className={cn(
            "flex items-start gap-1.5 text-xs leading-snug",
            message.tone === "critical"
              ? "text-critical-700"
              : "text-attention-700"
          )}
        >
          <AlertTriangle className="mt-0.5 size-3 shrink-0" />
          <span className="min-w-0">{message.body}</span>
        </div>
      ))}
    </div>
  );
}

export function ZoneDivider() {
  return <span aria-hidden className="h-6 w-px shrink-0 bg-slate-200" />;
}
