"use client";

import { useEffect, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

import { usePermissions } from "@/context/PermissionsContext";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { loginHref } from "@/lib/authRedirect";
import { canReadRevenue } from "@/lib/permissions";
import { PageHeader } from "@/components/PageHeader";
import { TabBar } from "@/components/TabBar";
import { RevenueMovementPanel } from "@/components/revenue/RevenueMovementPanel";
import { ConcentrationPanel } from "@/components/revenue/ConcentrationPanel";
import { MarginBoard } from "@/components/revenue/MarginBoard";
import { RecoveryPanel } from "@/components/revenue/RecoveryPanel";
import { HealthBoard } from "@/components/revenue/HealthBoard";
import { RecognitionExportDialog } from "@/components/revenue/RecognitionExportDialog";
import { ServiceCostRatesPanel } from "@/components/revenue/ServiceCostRatesPanel";
import { useTranslation } from "@/context/LocalizationContext";

const TABS = [
  "Movement",
  "Concentration",
  "Margin",
  "Recovery",
  "Health",
  "Rates",
] as const;

type Tab = (typeof TABS)[number];

const TAB_LABELS: Record<Tab, string> = {
  Movement: "revenue.tab.movement",
  Concentration: "revenue.tab.concentration",
  Margin: "revenue.tab.margin",
  Recovery: "revenue.tab.recovery",
  Health: "revenue.tab.health",
  Rates: "revenue.tab.rates",
};

export default function Revenue() {
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
    <RevenueSections
      activeTab={activeTab}
      onChange={setActiveTab}
      goToLogin={goToLogin}
    />
  );
}

function RevenueSections({
  activeTab,
  onChange,
  goToLogin,
}: {
  activeTab: string;
  onChange: (tab: string) => void;
  goToLogin: () => void;
}) {
  const t = useTranslation();
  const { grants, resolved } = usePermissions();

  const mayReadRevenue = canReadRevenue(grants);
  const mayReadRates = canReadRevenue(grants, "service-cost-rates:read");

  const tabs = TABS.filter((tab) =>
    tab === "Rates" ? mayReadRates : mayReadRevenue
  );

  useEffect(() => {
    if (!resolved || tabs.length === 0) return;

    const fromHash = decodeURIComponent(window.location.hash.replace("#", ""));
    if (fromHash && tabs.includes(fromHash as Tab)) {
      onChange(fromHash);
      return;
    }

    if (!tabs.includes(activeTab as Tab)) onChange(tabs[0]);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resolved, tabs.join("|")]);

  if (!resolved) return null;

  const tabLabel = (tab: string) => t(TAB_LABELS[tab as Tab]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("reconciliation.money")}
        title={t("revenue.revenue")}
        description={t("revenue.howTheBusinessIsDoing")}
      >
        {mayReadRevenue && (
          <RecognitionExportDialog onUnauthorized={goToLogin} />
        )}
      </PageHeader>

      {tabs.length > 1 && (
        <TabBar
          tabs={[...tabs]}
          activeTab={activeTab}
          label={tabLabel}
          aria-label={t("revenue.revenueSections")}
          onChange={(tab) => {
            onChange(tab);
            window.history.replaceState(null, "", `#${encodeURIComponent(tab)}`);
          }}
        />
      )}

      {activeTab === "Movement" && mayReadRevenue && (
        <RevenueMovementPanel onUnauthorized={goToLogin} />
      )}

      {activeTab === "Concentration" && mayReadRevenue && (
        <ConcentrationPanel onUnauthorized={goToLogin} />
      )}

      {activeTab === "Margin" && mayReadRevenue && (
        <MarginBoard onUnauthorized={goToLogin} />
      )}

      {activeTab === "Recovery" && mayReadRevenue && (
        <RecoveryPanel onUnauthorized={goToLogin} />
      )}

      {activeTab === "Health" && mayReadRevenue && (
        <HealthBoard onUnauthorized={goToLogin} />
      )}

      {activeTab === "Rates" && mayReadRates && (
        <ServiceCostRatesPanel onUnauthorized={goToLogin} />
      )}
    </div>
  );
}
