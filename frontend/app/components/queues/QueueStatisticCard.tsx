"use client";

import Link from "next/link";
import { useMemo } from "react";
import { TriangleAlert } from "lucide-react";

import { QueueStatistic, RoutingPolicy } from "@/types";
import {
  ROUTING_POLICY_HINTS,
  assignsOnArrival,
  hasIdleRoutingFromStatistic,
  nextUpComparator,
  oldestWaitingLabel,
  routingPolicyLabel,
} from "@/lib/queues";
import { shortId } from "@/lib/resources";
import { StatusBadge } from "@/components/StatusBadge";
import { Card, CardContent } from "@/components/ui/card";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

export function QueueStatisticCard({
  statistic,
  href,
  unroutedHref,
  memberHref,
}: {
  statistic: QueueStatistic;
  href?: string;
  unroutedHref?: string;
  memberHref?: (userId: string) => string;
}) {
  const t = useTranslation();
  const idle = hasIdleRoutingFromStatistic(statistic);
  const policy = statistic.routingPolicy as RoutingPolicy;

  const orderedLoads = useMemo(() => {
    const comparator = nextUpComparator(statistic.routingPolicy);
    if (!comparator) return statistic.memberLoads;
    return [...statistic.memberLoads].sort(comparator);
  }, [statistic.memberLoads, statistic.routingPolicy]);

  const showsNextUp =
    assignsOnArrival(statistic.routingPolicy) && orderedLoads.length > 0;

  return (
    <Card>
      <CardContent className="space-y-4 pt-6">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              tried at priority {statistic.priority}
            </p>
            <h3 className="mt-0.5 truncate font-mono text-base font-medium text-slate-900">
              {href ? (
                <Link href={href} className="text-brand-800 hover:underline">
                  {statistic.name}
                </Link>
              ) : (
                statistic.name
              )}
            </h3>
          </div>
          <div className="flex flex-shrink-0 items-center gap-2">
            <span
              className="font-mono text-[11px] uppercase tracking-widest text-slate-400"
              title={ROUTING_POLICY_HINTS[policy]}
            >
              {routingPolicyLabel(statistic.routingPolicy)}
            </span>
            <StatusBadge
              status={statistic.isEnabled ? "Enabled" : "Disabled"}
              label={statistic.isEnabled ? t("queues.takingWork") : "Paused"}
            />
          </div>
        </div>

        {idle && (
          <div className="flex items-start gap-2.5 rounded-md border border-attention-200 bg-attention-50 px-3 py-2">
            <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
            <p className="text-sm text-attention-700">
              Set to hand work out, with nobody on it. Cases arrive and sit
              unassigned until somebody is added.
            </p>
          </div>
        )}

        <div className="grid grid-cols-3 gap-4">
          <Figure
            label={t("queues.waiting")}
            value={statistic.depth}
            hint={t("queues.openNobodyHasPickedThem")}
            emphasis={statistic.depth > 0}
          />
          <Figure
            label={t("queues.inProgress")}
            value={statistic.inProgressCount}
            hint={t("queues.openSomebodyHoldingThem")}
          />
          <div>
            <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              {t("queues.oldestWaiting")}
            </p>
            <p className="mt-1 font-mono text-2xl text-slate-900">
              {oldestWaitingLabel(statistic.oldestWaitingForSeconds)}
            </p>
            <p className="mt-0.5 text-xs text-slate-500">
              {statistic.oldestWaitingForSeconds === null ? (
                <>
                  Nothing waiting.{" "}
                  {unroutedHref && (
                    <Link href={unroutedHref} className="text-brand-800 hover:underline">
                      {t("queues.checkTheUntriagedPile")}
                    </Link>
                  )}
                </>
              ) : (
                "How long the front of the queue has been there."
              )}
            </p>
          </div>
        </div>

        <div>
          <div className="flex items-baseline justify-between gap-3">
            <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              {t("queues.members")}
            </p>
            {showsNextUp && (
              <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                next up first
              </p>
            )}
          </div>

          {orderedLoads.length === 0 ? (
            <p className="mt-2 text-sm text-slate-500">
              {statistic.routingPolicy === "Manual"
                ?
                  "Nobody. Under Manual nothing is handed out anyway — cases wait here until a supervisor assigns one."
                : "Nobody yet."}
            </p>
          ) : (
            <ul className="mt-2 divide-y divide-slate-100">
              {orderedLoads.map((load) => (
                <li
                  key={load.userId}
                  className="flex items-center justify-between gap-3 py-2"
                >
                  {memberHref ? (
                    <Link
                      href={memberHref(load.userId)}
                      className="min-w-0 truncate text-sm text-brand-800 hover:underline"
                    >
                      {load.userName || shortId(load.userId)}
                    </Link>
                  ) : (
                    <span className="min-w-0 truncate text-sm text-slate-900">
                      {load.userName || shortId(load.userId)}
                    </span>
                  )}
                  <span className="flex flex-shrink-0 items-center gap-4">
                    <span className="text-right">
                      <span className="block font-mono text-sm text-slate-900">
                        {load.openCaseCount}
                      </span>
                      <span className="block font-mono text-[10px] uppercase tracking-widest text-slate-400">
                        open cases, all queues
                      </span>
                    </span>
                    <span className="w-32 text-right">
                      <span className="block text-xs text-slate-600">
                        {load.lastAssignedAt
                          ? <LocalTime value={load.lastAssignedAt} dateOnly />
                          :
                            "never"}
                      </span>
                      <span className="block font-mono text-[10px] uppercase tracking-widest text-slate-400">
                        last handed one
                      </span>
                    </span>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

function Figure({
  label,
  value,
  hint,
  emphasis = false,
}: {
  label: string;
  value: number;
  hint: string;
  emphasis?: boolean;
}) {
  return (
    <div>
      <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {label}
      </p>
      <p
        className={
          emphasis
            ? "mt-1 font-mono text-2xl text-attention-700"
            : "mt-1 font-mono text-2xl text-slate-900"
        }
      >
        {value}
      </p>
      <p className="mt-0.5 text-xs text-slate-500">{hint}</p>
    </div>
  );
}
