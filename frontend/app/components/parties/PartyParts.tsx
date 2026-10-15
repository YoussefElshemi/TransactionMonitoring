"use client";

import Link from "next/link";
import { ArrowRight, Users } from "lucide-react";

import { PartyRole } from "@/types";
import { Button } from "@/components/ui/button";
import { partyName, riskWeight } from "@/lib/parties";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function RoleChip({
  role,
  muted = false,
}: {
  role: PartyRole | string;
  muted?: boolean;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded px-1.5 py-0.5 font-mono text-[11px] font-medium uppercase tracking-wide",
        muted
          ? "bg-slate-100 text-slate-500"
          : "bg-brand-100 text-brand-900"
      )}
    >
      {role}
    </span>
  );
}

export function RoleChips({ roles }: { roles: PartyRole[] }) {
  if (roles.length === 0) {
    return (
      <span className="font-mono text-xs text-slate-400">
        no role yet
      </span>
    );
  }

  return (
    <span className="flex flex-wrap gap-1">
      {roles.map((role) => (
        <RoleChip key={role} role={role} />
      ))}
    </span>
  );
}

export function RiskScore({
  score,
  className,
}: {
  score: number | null;
  className?: string;
}) {
  if (score === null) {
    return (
      <span className={cn("font-mono text-xs text-slate-400", className)}>
        not assessed
      </span>
    );
  }

  const weight = riskWeight(score);

  return (
    <span
      className={cn(
        "inline-flex items-center rounded-md border px-2 py-0.5 font-mono text-[11px] font-medium",
        weight === "settled" && "border-transparent bg-settled-50 text-settled-700",
        weight === "inflight" && "border-inflight-200 bg-inflight-50 text-inflight-700",
        weight === "attention" &&
          "border-attention-200 bg-attention-50 text-attention-700 ring-1 ring-inset ring-attention-200",
        weight === "critical" &&
          "border-critical-solid bg-critical-solid text-critical-solid-foreground",
        className
      )}
    >
      {score}
    </span>
  );
}

export function PartyLink({
  partyId,
  identifier,
  displayName,
  className,
}: {
  partyId: string;
  identifier: string;
  displayName?: string | null;
  className?: string;
}) {
  const name = partyName({ identifier, displayName: displayName ?? null });

  return (
    <Link
      href={`/parties/${partyId}`}
      className={cn("group inline-flex min-w-0 flex-col", className)}
    >
      <span className="truncate text-sm font-medium text-slate-900 group-hover:text-brand-800">
        {name}
      </span>
      <span className="truncate font-mono text-xs text-slate-400">
        {identifier}
      </span>
    </Link>
  );
}

export function NoPartiesYet({
  canManage,
  filtered = false,
}: {
  canManage: boolean;
  filtered?: boolean;
}) {
  const t = useTranslation();
  if (filtered) {
    return (
      <div className="rounded-lg border border-dashed border-slate-300 bg-slate-50/60 px-6 py-12 text-center">
        <p className="text-sm font-medium text-slate-600">
          {t("parties.noPartyMatchesThat")}
        </p>
        <p className="mx-auto mt-2 max-w-md text-xs text-slate-500">
          Identifiers are matched case-insensitively here, but they are stored
          case-sensitively — <span className="font-mono">ACC-1</span> and{" "}
          <span className="font-mono">acc-1</span> are two different parties, and
          a search for either finds both.
        </p>
      </div>
    );
  }

  return (
    <div className="rounded-lg border border-dashed border-slate-300 bg-slate-50/60 px-6 py-12 text-center">
      <Users className="mx-auto size-6 text-slate-300" />
      <p className="mt-3 text-sm font-medium text-slate-600">
        {t("parties.noPartiesYet")}
      </p>
      <p className="mx-auto mt-2 max-w-lg text-xs text-slate-500">
        A transaction carries a payload; it does not, on its own, carry anybody.
        Declaring a party mapping tells the engine where in your payloads the
        sender, the beneficiary and the merchant sit — and from the next
        evaluate on, each one gets a lasting identity: the payments they have
        stood in, the cases they have been part of, and an assessment an analyst
        can record against them.
      </p>
      {canManage ? (
        <Button asChild size="sm" className="mt-4 gap-1.5">
          <Link href="/organization#Party%20Mapping">
            {t("parties.declareAPartyMapping")}
            <ArrowRight className="size-4" />
          </Link>
        </Button>
      ) : (
        <p className="mx-auto mt-3 max-w-lg text-xs text-slate-400">
          A manager or an admin declares the mapping, under My Organization.
          Until then nothing appears here, and nothing is wrong.
        </p>
      )}
    </div>
  );
}

export function SelfPaymentNote() {
  return (
    <p className="rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs text-attention-700">
      This party has been seen both sending and receiving. One identifier
      standing as Remitter and Beneficiary is somebody paying themselves —
      ordinary for an account transfer, and worth a second look anywhere else.
    </p>
  );
}
