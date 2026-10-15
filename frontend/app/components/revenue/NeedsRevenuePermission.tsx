"use client";

import type { RevenuePermission } from "@/lib/permissions";
import { useTranslation } from "@/context/LocalizationContext";

export function NeedsRevenuePermission({
  permission,
}: {
  permission: RevenuePermission;
}) {
  const t = useTranslation();

  return (
    <div className="rounded-xl border border-slate-200 bg-card p-6">
      <p className="text-sm text-slate-500">
        {t("billing.needs")}{" "}
        <span className="font-mono text-xs">{permission}</span>{" "}
        {t("reconciliation.and")}{" "}
        <span className="font-mono text-xs">platform:administer</span>.
      </p>
      <p className="mt-2 max-w-xl text-xs text-slate-400">
        {t("revenue.neitherIsInAManagerBundle")}
      </p>
    </div>
  );
}
