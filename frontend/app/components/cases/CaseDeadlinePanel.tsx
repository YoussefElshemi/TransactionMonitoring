"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { ArrowUpRight, Clock } from "lucide-react";

import { Case, CaseAgeing, EscalationRule, EscalationRuleResource } from "@/types";
import { API_BASE, unwrap } from "@/lib/api";
import { usePermissions } from "@/context/PermissionsContext";
import { shortId } from "@/lib/resources";
import {
  BUCKET_HINTS,
  duePhrase,
  durationLabel,
  isAgeingFrozen,
  tickAgeing,
} from "@/lib/ageing";
import { Card, CardContent } from "@/components/ui/card";
import { AgeBucketBadge, useElapsedSince } from "./CaseAgeing";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const useEscalationRule = (
  organizationId: string,
  escalationRuleId: string | null,
  enabled: boolean
): { rule: EscalationRule | null; resolved: boolean } => {
  const [rule, setRule] = useState<EscalationRule | null>(null);
  const [resolved, setResolved] = useState(false);

  useEffect(() => {
    if (!enabled || !escalationRuleId) {
      setRule(null);
      setResolved(true);
      return;
    }

    let cancelled = false;
    setResolved(false);

    (async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/organizations/${organizationId}/escalation-rules/${escalationRuleId}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );

        if (cancelled) return;

        if (response.ok) {
          const resource = await unwrap<EscalationRuleResource>(response);
          setRule(resource.escalationRule);
        } else {
          setRule(null);
        }
      } catch {
        if (!cancelled) setRule(null);
      } finally {
        if (!cancelled) setResolved(true);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [organizationId, escalationRuleId, enabled]);

  return { rule, resolved };
};

export function CaseDeadlinePanel({
  subject,
  ageing,
}: {
  subject: Case;
  ageing: CaseAgeing;
}) {
  const t = useTranslation();
  const { holds } = usePermissions();
  const canReadEscalationRules = holds("escalation-rules:read");
  const frozen = isAgeingFrozen(subject);
  /**
   * Anchored on the ageing block itself, not on the case id.
   *
   * The block is a new object on every fetch, so the count restarts whenever
   * the page reloads the case — assigning it, closing it, settling a payment.
   * Anchoring on the id instead would keep counting from the first mount and
   * add the elapsed time to a figure that had already moved on.
   *
   * A detail page is not refetched on a timer the way a list is, so the
   * durations tick and the bucket is as of the last load — which is the honest
   * thing to show, and is said in the footnote below.
   */
  const elapsed = useElapsedSince(ageing);
  const live = tickAgeing(ageing, elapsed, frozen);
  const phrase = duePhrase(live.timeToDueInMilliseconds);

  const { rule, resolved } = useEscalationRule(
    subject.organizationId,
    subject.escalationRuleId,
    canReadEscalationRules && subject.escalatedAt !== null
  );

  return (
    <Card>
      <CardContent className="space-y-4 pt-6">
        <div className="flex flex-wrap items-center gap-3">
          <Clock className="size-4 text-slate-400" />
          <h2 className="font-mono text-sm font-medium text-slate-900">
            {t("cases.deadline")}
          </h2>
          <AgeBucketBadge bucket={live.bucket} />
          {frozen && (
            <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              as at close
            </span>
          )}
        </div>

        <div className="grid gap-x-8 gap-y-3 text-sm sm:grid-cols-3">
          <div>
            <span className="font-medium text-slate-500">{t("cases.due")}</span>
            <p className="mt-0.5 text-slate-900">
              <LocalTime value={subject.dueAt} />
            </p>
          </div>
          <div>
            <span className="font-medium text-slate-500">
              {phrase.overdue ? "Overdue by" : "Time left"}
            </span>
            <p
              className={`mt-0.5 font-mono tabular-nums ${
                phrase.overdue ? "text-critical-700" : "text-slate-900"
              }`}
            >
              {durationLabel(live.timeToDueInMilliseconds)}
            </p>
          </div>
          <div>
            <span className="font-medium text-slate-500">
              {frozen ? "Open for" : "Open"}
            </span>
            <p className="mt-0.5 font-mono tabular-nums text-slate-900">
              {durationLabel(live.ageInMilliseconds)}
            </p>
          </div>
        </div>

        <p className="text-xs text-slate-500">
          The earlier of this severity&apos;s SLA target and the moment the
          soonest hold underneath runs out, fixed when the case opened. Editing
          the organisation&apos;s targets does not re-date it, and neither does
          an escalation.
        </p>

        <p className="text-xs text-slate-500">
          {BUCKET_HINTS[live.bucket as keyof typeof BUCKET_HINTS]}
          {!frozen && (
            <>
              {" "}
              The countdown ticks here; the band is as of the last load.
            </>
          )}
        </p>

        {subject.escalatedAt && (
          <div className="rounded-md border border-elevated-200 bg-elevated-50 px-3 py-2.5">
            <div className="flex items-start gap-2.5">
              <ArrowUpRight className="mt-0.5 size-4 flex-shrink-0 text-elevated-700" />
              <div className="min-w-0 space-y-1">
                <p className="text-sm font-medium text-elevated-700">
                  A rule escalated this case on{" "}
                  <LocalTime value={subject.escalatedAt} />.
                </p>

                {rule ? (
                  <p className="text-sm text-elevated-700">
                    <Link
                      href={`/organization/escalation-rules/${rule.escalationRuleId}`}
                      className="font-medium text-brand-800 hover:underline"
                    >
                      {rule.name}
                    </Link>
                    {!rule.isEnabled && (
                      <span className="ml-1.5 font-mono text-[11px] uppercase tracking-widest text-slate-400">
                        paused since
                      </span>
                    )}
                  </p>
                ) : subject.escalationRuleId && resolved ? (
                  <p className="text-sm text-elevated-700">
                    Rule{" "}
                    <span className="font-mono">
                      {shortId(subject.escalationRuleId)}
                    </span>
                    {canReadEscalationRules
                      ? " — retired, or no longer readable."
                      : " — opening the rule itself needs escalation-rules:read."}
                  </p>
                ) : null}

                <p className="text-xs text-elevated-700">
                  Escalation stamps the case and may move its queue or raise its
                  severity. It does not change the status and does not move the
                  deadline.
                </p>
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
