"use client";

import Link from "next/link";
import { ArrowRight } from "lucide-react";

import { PartyRole, TransactionParty, PARTY_ROLES } from "@/types";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { RoleChip } from "@/components/parties/PartyParts";
import { useTranslation } from "@/context/LocalizationContext";

export function TransactionParties({
  parties,
  canManage,
}: {
  parties: TransactionParty[];
  canManage: boolean;
}) {
  const t = useTranslation();
  const byRole = PARTY_ROLES.map((role) => ({
    role,
    parties: parties.filter((party) => party.role === role),
  })).filter((group) => group.parties.length > 0);

  const known = new Set<string>(PARTY_ROLES);
  const unknown = parties.filter((party) => !known.has(party.role));

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base font-semibold">{t("parties.parties")}</CardTitle>
      </CardHeader>
      <CardContent>
        {parties.length === 0 ? (
          <div className="space-y-2">
            <p className="text-sm text-slate-500">
              {t("parties.noPartiesWereResolvedFor")}
            </p>
            <p className="max-w-2xl text-xs text-slate-400">
              Either this organisation declares no party mapping — in which case
              nobody is resolved on any transaction — or the paths it declares
              found nothing here. Neither is an error: a payload missing a
              beneficiary is not a failed payment, and a party is recorded only
              where one was actually named.
              {canManage && (
                <>
                  {" "}
                  <Link
                    href="/organization#Party%20Mapping"
                    className="text-brand-800 underline underline-offset-2 hover:no-underline"
                  >
                    {t("parties.checkThePartyMapping")}
                  </Link>
                  .
                </>
              )}
            </p>
          </div>
        ) : (
          <div className="space-y-4">
            {byRole.map((group) => (
              <RoleGroup
                key={group.role}
                role={group.role}
                parties={group.parties}
              />
            ))}
            {unknown.length > 0 && (
              <RoleGroup role={unknown[0].role} parties={unknown} />
            )}
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function RoleGroup({
  role,
  parties,
}: {
  role: PartyRole | string;
  parties: TransactionParty[];
}) {
  return (
    <div className="space-y-1.5">
      <div className="flex items-baseline gap-2">
        <RoleChip role={role} />
        {parties.length > 1 && (
          <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
            {parties.length} on this payment
          </span>
        )}
      </div>

      <ul className="divide-y divide-slate-100 rounded-md border border-slate-200 bg-card">
        {parties.map((party) => (
          <li key={`${party.partyId}-${party.role}`}>
            <Link
              href={`/parties/${party.partyId}`}
              className="group flex items-center justify-between gap-3 px-3 py-2 transition-colors hover:bg-slate-50/50"
            >
              <span className="flex min-w-0 flex-col">
                <span className="truncate text-sm font-medium text-slate-900 group-hover:text-brand-800">
                  {party.displayName?.trim() || party.identifier}
                </span>
                <span className="truncate font-mono text-xs text-slate-400">
                  {party.identifier}
                </span>
              </span>
              <ArrowRight className="size-4 shrink-0 text-slate-300" />
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
}
