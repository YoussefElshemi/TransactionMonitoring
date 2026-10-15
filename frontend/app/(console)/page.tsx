"use client";

import prettyMilliseconds from "pretty-ms";
import { useAuth } from "@/context/AuthContext";
import { useEffect, useState } from "react";
import { ProblemDetails, Statistic, ValidationProblemDetails } from "@/types";
import { toast } from "sonner";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import LandingPage from "@/components/LandingPage";
import { PageHeader } from "@/components/PageHeader";
import { DateRangePicker } from "@/components/DateRangePicker";
import { Bar } from "react-chartjs-2";
import "chartjs-adapter-moment";
import { Chart, registerables } from "chart.js";
import { subDays, addDays } from "date-fns";
import type { DateRange } from "react-day-picker";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, unwrap } from "@/lib/api";
import { caseStatusLabel } from "@/lib/cases";
import { BreachRateCard } from "@/components/cases/BreachRateCard";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

Chart.register(...registerables);

const accentColors = {
  attention: "text-attention-700",
  inflight: "text-inflight-700",
  settled: "text-settled-700",
  critical: "text-critical-700",
  elevated: "text-elevated-700",
} as const;

type Accent = keyof typeof accentColors;

const STATUS_ACCENTS: Record<string, Accent> = {
  Pending: "attention",
  Assigned: "inflight",
  Approved: "settled",
  Rejected: "critical",
  InReview: "inflight",
  Escalated: "elevated",
  PendingInformation: "attention",
  OnHold: "attention",
};

const STATUS_HINTS: Record<string, string> = {
  Escalated: "Handed up. Somebody senior is expected to look.",
  PendingInformation: "Waiting on an answer from outside the desk.",
  OnHold: "Parked deliberately — payments under these are still held.",
};

const STATUS_ORDER = [
  "Pending",
  "Assigned",
  "InReview",
  "Escalated",
  "PendingInformation",
  "OnHold",
  "Approved",
  "Rejected",
  "Expired",
];

const statusRank = (status: string): number => {
  const index = STATUS_ORDER.indexOf(status);
  return index === -1 ? STATUS_ORDER.length : index;
};

function StatCard({
  label,
  value,
  accent,
  hint,
}: {
  label: string;
  value: string | number;
  accent?: Accent;
  hint?: string;
}) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-xs font-medium uppercase tracking-widest text-slate-400">
          {label}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <p
          className={`font-mono text-3xl font-medium tabular-nums tracking-tight ${
            accent ? accentColors[accent] : "text-slate-900"
          }`}
        >
          {value}
        </p>
        {hint && <p className="mt-1.5 text-xs text-slate-500">{hint}</p>}
      </CardContent>
    </Card>
  );
}

export default function Home() {
  const t = useTranslation();
  const { user } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [statistics, setStatistics] = useState<Statistic | null>(null);
  const [previousStatistics, setPreviousStatistics] = useState<Statistic | null>(
    null
  );
  const [loading, setLoading] = useState(false);
  const [dateRange, setDateRange] = useState<DateRange | undefined>(() => {
    const fromDate = searchParams.get("fromDate");
    const toDate = searchParams.get("toDate");
    try {
      if (fromDate?.length && toDate?.length) {
        return { from: new Date(fromDate), to: new Date(toDate) };
      }
    } catch {}
    return { from: subDays(new Date(), 30), to: new Date() };
  });

  const fetchPreviousStatistics = async (fromDate: Date, toDate: Date) => {
    const span = toDate.getTime() - fromDate.getTime();
    // A single-day selection has a zero span, which would compare the window
    // against itself and always read "no change". A day back is the honest
    // previous window there.
    const previousTo = new Date(fromDate.getTime() - 1);
    const previousFrom = new Date(
      fromDate.getTime() - (span > 0 ? span : 24 * 60 * 60 * 1000)
    );

    try {
      const queryString = new URLSearchParams();
      queryString.append("FromDate", previousFrom.toISOString().split("T")[0]);
      queryString.append("ToDate", previousTo.toISOString().split("T")[0]);

      const response = await apiFetch(
        `${API_BASE}/statistics?${queryString}`,
        { method: "GET", headers: { "Content-Type": "application/json" }, credentials: "include" }
      );

      setPreviousStatistics(
        response.ok ? await unwrap<Statistic>(response) : null
      );
    } catch {
      setPreviousStatistics(null);
    }
  };

  const fetchStatistics = async (
    firstTime: boolean,
    fromDate?: Date,
    toDate?: Date
  ) => {
    setLoading(firstTime);
    try {
      const queryString = new URLSearchParams();
      if (fromDate)
        queryString.append("FromDate", fromDate.toISOString().split("T")[0]);
      if (toDate)
        queryString.append("ToDate", toDate.toISOString().split("T")[0]);

      const response = await apiFetch(
        `${API_BASE}/statistics?${queryString}`,
        { method: "GET", headers: { "Content-Type": "application/json" }, credentials: "include" }
      );

      if (response.ok) {
        setStatistics(await unwrap<Statistic>(response));
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map((k) => `${k}${k.length ? ": " : ""}${errorData.errors[k].join("\n")}`)
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginAccessResource"), { id: "unauthorized" });
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("common.youDoNotHavePermission"), { id: "forbidden" });
        return router.push("/");
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("common.errorOccurredFetchingStatistics"), { id: "error-fetching" });
      }
    } catch {
      toast.error(t("common.errorOccurredFetchingStatistics"), { id: "error-fetching" });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user) {
      let firstTime = true;
      const interval = setInterval(
        (function fetchData() {
          fetchStatistics(firstTime, dateRange?.from, dateRange?.to);
          firstTime = false;
          return fetchData;
        })(),
        5000
      );
      return () => clearInterval(interval);
    }
  }, [dateRange]);

  useEffect(() => {
    if (!user) return;
    if (dateRange?.from && dateRange?.to) {
      fetchPreviousStatistics(dateRange.from, dateRange.to);
    } else {
      setPreviousStatistics(null);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dateRange, user]);

  useEffect(() => {
    if (dateRange?.from && dateRange?.to) {
      const params = new URLSearchParams();
      params.set("fromDate", dateRange.from.toISOString().split("T")[0]);
      params.set("toDate", dateRange.to.toISOString().split("T")[0]);
      router.replace(`?${params.toString()}`);
    } else {
      router.replace("?");
    }
  }, [dateRange, router]);

  if (!user) {
    return <LandingPage />;
  }

  const noTime = (date: Date) =>
    new Date(date.getFullYear(), date.getMonth(), date.getDate());

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("page.overview")}
        title={t("page.dashboard")}
        description={t("page.caseVolumeAndApiUsage")}
      >
        <DateRangePicker value={dateRange} onChange={setDateRange} />
      </PageHeader>

      {loading ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <Card key={i}>
              <CardHeader className="pb-2">
                <Skeleton className="h-4 w-28" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-8 w-16" />
              </CardContent>
            </Card>
          ))}
        </div>
      ) : statistics ? (
        <>
          <div className="space-y-4">
            <h2 className="font-mono text-sm font-medium tracking-tight text-slate-900">
              {t("page.cases")}
            </h2>

            <BreachRateCard
              statistic={statistics.case}
              previous={previousStatistics?.case ?? null}
              periodLabel={
                dateRange?.from && dateRange?.to
                  ? "the window before this one"
                  : undefined
              }
              onShowBreached={() =>
                router.push("/cases?status=&breached=true&pageNumber=1&pageSize=25")
              }
            />

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              <StatCard label={t("page.totalCases")} value={statistics.case.total} />
              {Object.entries(statistics.case.byStatus)
                .sort(([a], [b]) => statusRank(a) - statusRank(b))
                .map(([status, count]) => (
                  <StatCard
                    key={status}
                    label={caseStatusLabel(status)}
                    value={count}
                    accent={
                      status === "Expired"
                        ? count > 0
                          ? "attention"
                          : undefined
                        : STATUS_ACCENTS[status]
                    }
                    hint={
                      status === "Expired"
                        ? count > 0
                          ? t("page.closedClockNotAnalystRising")
                          : t("page.noCaseHasRunOut")
                        : STATUS_HINTS[status]
                    }
                  />
                ))}
              <StatCard
                label={t("page.avgResolutionTime")}
                value={
                  statistics.case.averageResolutionTime
                    ? prettyMilliseconds(statistics.case.averageResolutionTime * 10000)
                    : "N/A"
                }
                hint={t("page.analystResolutionsOnlyExpiredCases")}
              />
            </div>
          </div>

          <Card>
            <CardHeader>
              <CardTitle className="text-base font-semibold text-slate-700">
                {t("page.usage")}
              </CardTitle>
            </CardHeader>
            <CardContent>
              {(() => {
                const dates = Object.keys(statistics.usage).sort();
                const firstDate = dateRange?.from ?? (dates.length ? new Date(dates[0]) : subDays(new Date(), 30));
                const lastDate = dateRange?.to ?? (dates.length ? new Date(dates[dates.length - 1]) : new Date());
                return (
                  <Bar
                    data={{
                      labels: Object.keys(statistics.usage),
                      datasets: [
                        {
                          data: Object.values(statistics.usage),
                          borderColor: "rgb(79, 70, 229)",
                          backgroundColor: "rgb(79, 70, 229)",
                          hoverBackgroundColor: "rgba(79, 70, 229, 0.85)",
                          borderWidth: 1,
                        },
                      ],
                    }}
                    options={{
                      responsive: true,
                      plugins: { legend: { display: false } },
                      scales: {
                        x: {
                          title: { display: true, text: "Date" },
                          type: "time",
                          time: { minUnit: "day", tooltipFormat: "MMM DD, yyyy" },
                          suggestedMin: noTime(
                            firstDate.getTime() === lastDate.getTime()
                              ? subDays(firstDate, 1)
                              : firstDate
                          ).toISOString(),
                          suggestedMax: noTime(
                            firstDate.getTime() === lastDate.getTime()
                              ? addDays(lastDate, 1)
                              : lastDate
                          ).toISOString(),
                        },
                        y: {
                          title: { display: true, text: "Usage" },
                          type: "linear",
                          beginAtZero: true,
                          ticks: { precision: 0 },
                        },
                      },
                    }}
                  />
                );
              })()}
            </CardContent>
          </Card>
        </>
      ) : null}
    </div>
  );
}
