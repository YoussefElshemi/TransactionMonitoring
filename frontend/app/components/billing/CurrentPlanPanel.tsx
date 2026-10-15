"use client";

import { useCallback, useEffect, useState } from "react";
import { Tag } from "lucide-react";

import type { Subscription } from "@/types";
import { API_BASE, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import {
  inForceSubscription,
  scheduledSubscriptions,
  versionLabel,
} from "@/lib/plans";
import { usePermissions } from "@/context/PermissionsContext";
import { LocalTime } from "@/components/LocalTime";
import { PriceStructure } from "@/components/billing/PriceStructure";
import { Skeleton } from "@/components/ui/skeleton";
import { useTranslation } from "@/context/LocalizationContext";

const TIMELINE_PAGE_SIZE = 100;

export function CurrentPlanPanel({
  organizationId,
}: {
  organizationId: string;
}) {
  const t = useTranslation();
  const { holds, resolved } = usePermissions();
  const mayRead = holds("subscriptions:read");

  const [timeline, setTimeline] = useState<Subscription[]>([]);
  const [loading, setLoading] = useState(true);
  const [failed, setFailed] = useState(false);

  const fetchTimeline = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/subscriptions?PageNumber=1&PageSize=${TIMELINE_PAGE_SIZE}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<Subscription>(response);
        setTimeline(page.data);
        setFailed(false);
      } else {
        setFailed(true);
      }
    } catch {
      setFailed(true);
    } finally {
      setLoading(false);
    }
  }, [organizationId]);

  useEffect(() => {
    if (mayRead) fetchTimeline();
    else setLoading(false);
  }, [mayRead, fetchTimeline]);

  if (!resolved) return null;

  if (!mayRead) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-6">
        <p className="text-sm text-slate-500">
          {t("billing.needs")}{" "}
          <span className="font-mono text-xs">subscriptions:read</span>.
        </p>
      </div>
    );
  }

  if (loading) {
    return <Skeleton className="h-48 w-full rounded-xl" />;
  }

  if (failed) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-6">
        <p className="text-sm text-slate-400">{t("billing.couldNotBeRead")}</p>
      </div>
    );
  }

  const current = inForceSubscription(timeline);
  const [next] = scheduledSubscriptions(timeline);

  return (
    <div className="space-y-3">
      <div className="flex items-start gap-2">
        <Tag className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
        <div>
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("billing.currentPlan")}
          </p>
          <p className="mt-1 max-w-2xl text-sm text-slate-500">
            {t("billing.whatYouAreOnAndWhatItCosts")}
          </p>
        </div>
      </div>

      {current === null ? (
        <div className="rounded-xl border border-slate-200 bg-card p-6">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("billing.noPlan")}
          </p>
          <p className="mt-2 max-w-xl text-sm text-slate-500">
            {t("billing.notOnAPlanWhoever")}
          </p>
        </div>
      ) : (
        <div className="rounded-xl border border-slate-200 bg-card p-4">
          <div className="flex flex-wrap items-baseline justify-between gap-3">
            <p className="text-base font-medium text-slate-900">
              {versionLabel(
                current.planName,
                current.planVersion?.versionNumber ?? null
              )}
            </p>
            <p className="font-mono text-xs text-slate-500">
              {t("billing.inForceSince")}{" "}
              <LocalTime value={current.effectiveFrom} dateOnly />
            </p>
          </div>

          {current.planVersion !== null && (
            <div className="mt-3 border-t border-slate-100 pt-3">
              <PriceStructure version={current.planVersion} />
            </div>
          )}
        </div>
      )}

      {next && (
        <div className="rounded-xl border border-inflight-200 bg-inflight-50 p-4">
          <p className="font-mono text-xs uppercase tracking-widest text-inflight-700">
            {t("billing.scheduledChange")}
          </p>
          <p className="mt-1 text-sm text-slate-900">
            {versionLabel(
              next.planName,
              next.planVersion?.versionNumber ?? null
            )}
          </p>
          <p className="mt-0.5 text-xs text-slate-600">
            {t("billing.from")} <LocalTime value={next.effectiveFrom} dateOnly />
          </p>

          {next.planVersion !== null && (
            <div className="mt-3 border-t border-inflight-200 pt-3">
              <PriceStructure version={next.planVersion} />
            </div>
          )}
        </div>
      )}

      <p className="text-xs text-slate-400">
        {t("billing.yourPriceIsSetByWhoeverRuns")}
      </p>
    </div>
  );
}
