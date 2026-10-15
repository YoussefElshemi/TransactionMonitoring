"use client";

import { useCallback, useEffect, useState } from "react";

import { Subscription } from "@/types";
import { API_BASE, unwrapPage } from "@/lib/api";
import {
  inForceSubscription,
  scheduledSubscriptions,
  versionLabel,
} from "@/lib/plans";
import { priceShapeSentence } from "@/lib/pricing";
import { apiFetch } from "@/lib/http";
import { usePermissions } from "@/context/PermissionsContext";
import { Field } from "@/components/FieldList";
import { LocalTime } from "@/components/LocalTime";
import { Skeleton } from "@/components/ui/skeleton";
import { useTranslation } from "@/context/LocalizationContext";

const TIMELINE_PAGE_SIZE = 100;

export function CurrentPlanFields({
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
      <Field label={t("common.plan")}>
        <span className="text-slate-400">
          {t("billing.needs")} <span className="font-mono text-xs">subscriptions:read</span>
        </span>
      </Field>
    );
  }

  if (loading) {
    return (
      <Field label={t("common.plan")}>
        <Skeleton className="h-4 w-40" />
      </Field>
    );
  }

  if (failed) {
    return (
      <Field label={t("common.plan")}>
        <span className="text-slate-400">{t("billing.couldNotBeRead")}</span>
      </Field>
    );
  }

  const current = inForceSubscription(timeline);
  const [next] = scheduledSubscriptions(timeline);

  return (
    <>
      <Field label={t("common.plan")}>
        {current ? (
          <span>
            {versionLabel(
              current.planName,
              current.planVersion?.versionNumber ?? null
            )}
          </span>
        ) : (
          <span className="text-slate-400">
            {t("billing.notOnAPlanWhoever")}
          </span>
        )}
      </Field>

      {current && (
        <>
          <Field label={t("billing.price")} mono>
            {current.planVersion
              ? priceShapeSentence(current.planVersion)
              : "—"}
          </Field>
          <Field label={t("billing.inForceSince")} mono>
            <LocalTime value={current.effectiveFrom} />
          </Field>
        </>
      )}

      {next && (
        <Field label={t("billing.changesTo")}>
          <span>
            {versionLabel(
              next.planName,
              next.planVersion?.versionNumber ?? null
            )}
            {next.planVersion && (
              <span className="font-mono text-xs text-slate-500">
                {" · "}
                {priceShapeSentence(next.planVersion)}
              </span>
            )}
            <span className="block text-xs text-slate-500">
              from <LocalTime value={next.effectiveFrom} />
            </span>
          </span>
        </Field>
      )}
    </>
  );
}
