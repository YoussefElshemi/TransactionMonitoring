"use client";

import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { dispositionLabelKey } from "@/lib/cases";
import {
  FieldKind,
  fieldKind,
  isGuid,
  isTimestamp,
  referenceHref,
  shortenId,
} from "./auditFields";
import type { ActorLookup } from "./useAuditHistory";

import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";
import { LocalTime } from "@/components/LocalTime";

interface AuditValueProps {
  field: string;
  value: unknown;
  actors: ActorLookup;
  canReachAdminPages: boolean;
  muted?: boolean;
}

const Muted = ({ children }: { children: React.ReactNode }) => (
  <span className="text-slate-400">{children}</span>
);

export function AuditValue({
  field,
  value,
  actors,
  canReachAdminPages,
  muted = false,
}: AuditValueProps) {
  // Before the first early return, because the branches below it are returns
  // rather than components and a hook after one would not run on every render.
  const format = useFormat();
  const t = useTranslation();

  if (value === null || value === undefined) {
    return <Muted>—</Muted>;
  }

  if (typeof value === "boolean") {
    return (
      <Badge
        variant="outline"
        className={cn(
          "font-medium",
          value
            ? "border-settled-200 bg-settled-50 text-settled-700 hover:bg-settled-50"
            : "border-slate-200 bg-slate-100 text-slate-500 hover:bg-slate-100",
          muted && "opacity-70"
        )}
      >
        {value ? "Yes" : "No"}
      </Badge>
    );
  }

  const tone = muted ? "text-slate-500" : "text-slate-900";

  if (typeof value === "number") {
    const kind = fieldKind(field);
    return (
      <span className={cn("text-sm tabular-nums", tone)}>
        {kind === "money" ? formatMoney(value) : format.count(value)}
      </span>
    );
  }

  if (typeof value === "string") {
    const kind = fieldKind(field);

    if (kind === "disposition") {
      return (
        <span className={cn("text-sm", tone)}>{t(dispositionLabelKey(value))}</span>
      );
    }

    if (kind === "money" && value.trim() !== "" && !Number.isNaN(Number(value))) {
      return (
        <span className={cn("text-sm tabular-nums", tone)}>
          {formatMoney(Number(value))}
        </span>
      );
    }

    if (isTimestamp(value)) {
      return (
        <span className={cn("text-sm", tone)} title={value}>
          <LocalTime value={value} />
        </span>
      );
    }

    if (isGuid(value)) {
      return (
        <GuidValue
          kind={kind}
          id={value}
          actors={actors}
          canReachAdminPages={canReachAdminPages}
          tone={tone}
        />
      );
    }

    if (value === "") {
      return <Muted>(empty)</Muted>;
    }

    return <span className={cn("text-sm break-words", tone)}>{value}</span>;
  }

  return (
    <span className={cn("font-mono text-xs break-all", tone)}>
      {JSON.stringify(value)}
    </span>
  );
}

const formatMoney = (amount: number): string =>
  amount.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 6,
  });

function GuidValue({
  kind,
  id,
  actors,
  canReachAdminPages,
  tone,
}: {
  kind: FieldKind;
  id: string;
  actors: ActorLookup;
  canReachAdminPages: boolean;
  tone: string;
}) {
  if (kind === "user") {
    const name = actors.nameOf(id);
    return (
      <span className={cn("text-sm", tone)} title={id}>
        {name ?? shortenId(id)}
      </span>
    );
  }

  const href = referenceHref(kind, id, canReachAdminPages);
  if (href) {
    return (
      <Link
        href={href}
        title={id}
        className="text-sm text-brand-800 hover:underline"
      >
        {shortenId(id)}
      </Link>
    );
  }

  return (
    <span className={cn("font-mono text-xs", tone)} title={id}>
      {shortenId(id)}
    </span>
  );
}
