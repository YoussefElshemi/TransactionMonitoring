"use client";

import { Handshake } from "lucide-react";

import { CURRENCY_NOT_YET_STATED } from "@/lib/currency";
import { canWriteBilling } from "@/lib/permissions";
import { useBillingProfile } from "@/lib/useBillingProfile";
import { usePermissions } from "@/context/PermissionsContext";
import { ORDER_OF_OPERATIONS } from "@/lib/pricing";
import { CommitmentsPanel } from "@/components/billing/CommitmentsPanel";
import { ContractDiscountsPanel } from "@/components/billing/ContractDiscountsPanel";
import { InvoiceAdjustmentsPanel } from "@/components/billing/InvoiceAdjustmentsPanel";

export function ContractTermsPanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const { grants, holds, resolved } = usePermissions();
  const { profile, resolved: currencyResolved } =
    useBillingProfile(organizationId);

  const mayRead = holds("contract-terms:read");
  const canWrite = canWriteBilling(grants, "contract-terms:write");

  const currency = profile?.currency ?? null;

  if (!resolved) return null;

  if (!mayRead) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-6">
        <p className="text-sm text-slate-500">
          Needs{" "}
          <span className="font-mono text-xs">contract-terms:read</span> to see
          what this organisation was agreed.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="flex items-start gap-2">
        <Handshake className="mt-0.5 size-4 flex-shrink-0 text-slate-400" />
        <div>
          <p className="text-sm text-slate-600">
            What was agreed with this customer on top of whatever plan they are
            on. Kept off the plan because two customers sold the same published
            version can have been given entirely different terms.
          </p>
          <p className="mt-1 text-xs text-slate-500">
            However they are listed here, they reach an invoice in a fixed
            order. {ORDER_OF_OPERATIONS}
          </p>
          {currencyResolved && currency === null && (
            <p className="mt-1 text-xs text-attention-700">
              {CURRENCY_NOT_YET_STATED}
            </p>
          )}
        </div>
      </div>

      <ContractDiscountsPanel
        organizationId={organizationId}
        currency={currency}
        canWrite={canWrite}
        onUnauthorized={onUnauthorized}
      />

      <InvoiceAdjustmentsPanel
        organizationId={organizationId}
        currency={currency}
        canWrite={canWrite}
        onUnauthorized={onUnauthorized}
      />

      <CommitmentsPanel
        organizationId={organizationId}
        currency={currency}
        canWrite={canWrite}
        onUnauthorized={onUnauthorized}
      />
    </div>
  );
}
