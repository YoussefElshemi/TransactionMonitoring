"use client";

import { useEffect, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

import { usePermissions } from "@/context/PermissionsContext";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { loginHref } from "@/lib/authRedirect";
import { canWriteBilling } from "@/lib/permissions";
import { PageHeader } from "@/components/PageHeader";
import { TabBar } from "@/components/TabBar";
import { UnappliedCashPanel } from "@/components/reconciliation/UnappliedCashPanel";
import { ArrearsBoard } from "@/components/reconciliation/ArrearsBoard";
import { useTranslation } from "@/context/LocalizationContext";

const TABS = ["Unapplied", "Arrears"] as const;

export default function Reconciliation() {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const { allowed } = useRequirePermission("platform:administer", "/");

  const [activeTab, setActiveTab] = useState<string>(TABS[0]);

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const goToLogin = () => router.push(loginHref(redirectUri));

  if (!allowed) return null;

  return (
    <ReconciliationSections
      activeTab={activeTab}
      onChange={setActiveTab}
      goToLogin={goToLogin}
    />
  );
}

function ReconciliationSections({
  activeTab,
  onChange,
  goToLogin,
}: {
  activeTab: string;
  onChange: (tab: string) => void;
  goToLogin: () => void;
}) {
  const t = useTranslation();
  const { grants, holds, resolved } = usePermissions();

  const mayReconcile = canWriteBilling(grants, "payments:reconcile");
  const mayReadArrears = holds("dunning:read");

  const tabs = TABS.filter((tab) =>
    tab === "Unapplied" ? mayReconcile : mayReadArrears
  );

  useEffect(() => {
    if (!resolved || tabs.length === 0) return;

    const fromHash = decodeURIComponent(window.location.hash.replace("#", ""));
    if (fromHash && tabs.includes(fromHash as (typeof TABS)[number])) {
      onChange(fromHash);
      return;
    }

    if (!tabs.includes(activeTab as (typeof TABS)[number])) onChange(tabs[0]);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resolved, tabs.join("|")]);

  if (!resolved) return null;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("reconciliation.money")}
        title={t("reconciliation.reconciliation")}
        description={t("reconciliation.whatArrivedWithoutADocument")}
      />

      {tabs.length > 1 && (
        <TabBar
          tabs={[...tabs]}
          activeTab={activeTab}
          aria-label={t("reconciliation.reconciliationSections")}
          onChange={(tab) => {
            onChange(tab);
            window.history.replaceState(null, "", `#${encodeURIComponent(tab)}`);
          }}
        />
      )}

      {activeTab === "Unapplied" && mayReconcile && (
        <UnappliedCashPanel onUnauthorized={goToLogin} />
      )}

      {activeTab === "Arrears" && mayReadArrears && (
        <ArrearsBoard onUnauthorized={goToLogin} />
      )}
    </div>
  );
}
