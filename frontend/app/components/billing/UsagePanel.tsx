"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { Gauge } from "lucide-react";

import type { BillableResource, Usage } from "@/types";
import { organizationScopedPath, reportApiError, unwrapPage } from "@/lib/api";
import {
  BILLABLE_RESOURCE_HINTS,
  BILLABLE_RESOURCE_LABELS,
  BILLABLE_RESOURCE_ORDER,
  UNCLASSIFIED_HINT,
  formatQuantity,
} from "@/lib/pricing";
import { apiFetch } from "@/lib/http";
import { usePermissions } from "@/context/PermissionsContext";
import { cn } from "@/lib/utils";
import Pagination from "@/components/Pagination";
import { LocalTime } from "@/components/LocalTime";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useTranslation } from "@/context/LocalizationContext";

const ANY_KIND = "Any";

const PAGE_SIZE = 100;

export function UsagePanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId?: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { holds, resolved } = usePermissions();
  const mayRead = holds("usages:read");

  const [usages, setUsages] = useState<Usage[]>([]);
  const [resourceFilter, setResourceFilter] = useState("");
  const [kindFilter, setKindFilter] = useState<string>(ANY_KIND);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const fetchUsages = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: String(PAGE_SIZE),
      });
      if (resourceFilter.trim().length > 0) {
        query.set("Resource", resourceFilter.trim());
      }

      const response = await apiFetch(
        `${organizationScopedPath("usages", organizationId)}?${query}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<Usage>(response);
        setUsages(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("billing.errorOccurredFetchingUsage"),
          onUnauthorized,
          toastId: "error-fetching-usage",
        });
      }
    } catch {
      toast.error(t("billing.errorOccurredFetchingUsage"), {
        id: "error-fetching-usage",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, pageNumber, resourceFilter, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchUsages();
    else setLoading(false);
  }, [mayRead, fetchUsages]);

  const shown = useMemo(
    () =>
      kindFilter === ANY_KIND
        ? usages
        : usages.filter((usage) => usage.billableResource === kindFilter),
    [usages, kindFilter]
  );

  const totals = useMemo(() => {
    const counts = new Map<BillableResource, number>();

    for (const usage of usages) {
      counts.set(
        usage.billableResource,
        (counts.get(usage.billableResource) ?? 0) + usage.count
      );
    }

    return BILLABLE_RESOURCE_ORDER.filter((kind) => counts.has(kind)).map(
      (kind) => ({ kind, count: counts.get(kind) ?? 0 })
    );
  }, [usages]);

  if (!resolved) return null;

  if (!mayRead) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-6">
        <p className="text-sm text-slate-500">
          {t("billing.needs")} <span className="font-mono text-xs">usages:read</span>.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <div className="flex items-start gap-2">
        <Gauge className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
        <div>
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("billing.meteredUsage")}
          </p>
          <p className="mt-1 max-w-2xl text-sm text-slate-500">
            What has been served and counted. The kind is what a plan prices
            against — an evaluation, a rule test and a backtest cost different
            amounts to serve, and a plan can charge for each separately.
          </p>
        </div>
      </div>

      {totals.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {totals.map(({ kind, count }) => (
            <button
              key={kind}
              type="button"
              onClick={() =>
                setKindFilter((current) => (current === kind ? ANY_KIND : kind))
              }
              title={BILLABLE_RESOURCE_HINTS[kind]}
              className={cn(
                "rounded-lg border px-3 py-2 text-left transition-colors",
                kindFilter === kind
                  ? "border-brand-800 bg-brand-50"
                  : "border-slate-200 bg-card hover:bg-slate-50"
              )}
            >
              <span className="block font-mono text-xs uppercase tracking-widest text-slate-500">
                {BILLABLE_RESOURCE_LABELS[kind]}
              </span>
              <span className="mt-0.5 block font-mono text-sm text-slate-900">
                {formatQuantity(count)}
              </span>
            </button>
          ))}
        </div>
      )}

      <div className="flex flex-wrap items-center gap-3">
        <Input
          placeholder={t("billing.filterByRuleOrRule")}
          value={resourceFilter}
          onChange={(event) => {
            setResourceFilter(event.target.value);
            setPageNumber(1);
          }}
          className="w-64"
        />

        {kindFilter !== ANY_KIND && (
          <Button
            variant="ghost"
            size="sm"
            className="h-8 text-xs"
            onClick={() => setKindFilter(ANY_KIND)}
          >
            {t("billing.showEveryKind")}
          </Button>
        )}
      </div>

      {kindFilter !== ANY_KIND && (
        <p className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500">
          Showing {BILLABLE_RESOURCE_LABELS[kindFilter as BillableResource]}{" "}
          among the rows loaded on this page. The meter cannot be queried by
          kind, so this narrows what is displayed rather than what was read —
          later pages may hold more.
        </p>
      )}

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("billing.ruleOrRuleSet")}</TableHead>
              <TableHead className="w-56">{t("common.kind")}</TableHead>
              <TableHead className="w-28 text-right">{t("billing.count")}</TableHead>
              <TableHead className="w-48">{t("billing.metered")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 4 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 4 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : shown.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="py-10 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {usages.length === 0 ? "Nothing metered" : "No results"}
                  </p>
                  {usages.length > 0 && (
                    <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                      Nothing of that kind on this page. Other pages may hold
                      some — the meter cannot be queried by kind.
                    </p>
                  )}
                </TableCell>
              </TableRow>
            ) : (
              shown.map((usage) => (
                <TableRow key={usage.usageId} className="hover:bg-slate-50/50">
                  <TableCell className="font-mono text-xs text-slate-700">
                    {usage.resource}
                  </TableCell>

                  <TableCell>
                    <span
                      className={cn(
                        "rounded-full border px-2 py-0.5 font-mono text-xs",
                        usage.billableResource === "Unclassified"
                          ? "border-slate-200 bg-slate-50 text-slate-500"
                          : "border-elevated-200 bg-elevated-50 text-elevated-700"
                      )}
                      title={BILLABLE_RESOURCE_HINTS[usage.billableResource]}
                    >
                      {BILLABLE_RESOURCE_LABELS[usage.billableResource]}
                    </span>
                    {usage.billableResource === "Unclassified" && (
                      <span className="mt-0.5 block text-xs text-slate-400">
                        {UNCLASSIFIED_HINT}
                      </span>
                    )}
                  </TableCell>

                  <TableCell className="text-right font-mono text-sm text-slate-900">
                    {formatQuantity(usage.count)}
                  </TableCell>

                  <TableCell className="text-xs text-slate-500">
                    <LocalTime value={usage.createdAt} />
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {usages.length > 0 && totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}
    </div>
  );
}
