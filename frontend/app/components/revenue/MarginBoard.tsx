"use client";

import { Fragment, useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { Scale, TriangleAlert } from "lucide-react";

import type { CustomerMargin } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import {
  COSTS_INCOMPLETE,
  COSTS_INCOMPLETE_EXPLAINED,
  SOME_MARGINS_ARE_OVERSTATED,
  type MonthPoint,
  formatRevenue,
  marginIsOverstated,
  marginRateAnswer,
  marginTone,
  resourceLabelKey,
  uncostedLines,
} from "@/lib/revenue";
import { canReadRevenue } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useFormat } from "@/lib/useFormat";
import { useLocalization, useTranslation } from "@/context/LocalizationContext";
import { cn } from "@/lib/utils";
import { ToneBadge } from "@/components/status/StatusChips";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  MonthField,
  lastCompleteMonth,
  monthLabel,
} from "@/components/revenue/MonthPicker";
import { NeedsRevenuePermission } from "@/components/revenue/NeedsRevenuePermission";

export function MarginBoard({ onUnauthorized }: { onUnauthorized: () => void }) {
  const t = useTranslation();
  const { locale } = useLocalization();
  const { grants, resolved } = usePermissions();

  const mayRead = canReadRevenue(grants);

  const [point, setPoint] = useState<MonthPoint>(() => lastCompleteMonth());
  const [margins, setMargins] = useState<CustomerMargin[]>([]);
  const [loading, setLoading] = useState(true);
  const [opened, setOpened] = useState<string | null>(null);

  const fetchMargins = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        month: String(point.month),
        year: String(point.year),
      });

      const response = await apiFetch(`${API_BASE}/revenue/margin?${query}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        setMargins(await unwrap<CustomerMargin[]>(response));
      } else {
        await reportApiError(response, {
          fallback: t("revenue.marginCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-revenue-margin",
        });
      }
    } catch {
      toast.error(t("revenue.marginCouldNotBeRead"), {
        id: "error-fetching-revenue-margin",
      });
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [point.month, point.year, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchMargins();
    else setLoading(false);
  }, [mayRead, fetchMargins]);

  if (!resolved) return null;
  if (!mayRead) return <NeedsRevenuePermission permission="revenue:read" />;

  const overstated = margins.filter(marginIsOverstated).length;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex items-start gap-2">
          <Scale className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("revenue.margin")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {t("revenue.whatEachCustomerWasWorth")}
            </p>
          </div>
        </div>

        <MonthField
          label={t("revenue.month")}
          value={point}
          onChange={setPoint}
          disabled={loading}
        />
      </div>

      {overstated > 0 && (
        <p
          role="status"
          className="flex items-start gap-2 rounded-lg border border-attention-200 bg-attention-50 p-3 text-xs text-attention-700"
        >
          <TriangleAlert className="mt-px size-3.5 flex-shrink-0" />
          <span>{t(SOME_MARGINS_ARE_OVERSTATED, overstated)}</span>
        </p>
      )}

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <caption className="sr-only">
            {t("revenue.marginTableCaption", monthLabel(point, locale))}
          </caption>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("reconciliation.customer")}</TableHead>
              <TableHead className="w-36 text-right">
                {t("revenue.revenue")}
              </TableHead>
              <TableHead className="w-36 text-right">
                {t("revenue.cost")}
              </TableHead>
              <TableHead className="w-36 text-right">
                {t("revenue.marginAmount")}
              </TableHead>
              <TableHead className="w-44">{t("revenue.marginRate")}</TableHead>
              <TableHead className="w-24">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 4 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 6 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : margins.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="py-10 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {t("revenue.nothingBilled", monthLabel(point, locale))}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    {t("revenue.noCustomerWasBilledThatMonth")}
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              margins.map((margin) => (
                <MarginRow
                  key={margin.organizationId}
                  margin={margin}
                  open={opened === margin.organizationId}
                  onToggle={() =>
                    setOpened(
                      opened === margin.organizationId
                        ? null
                        : margin.organizationId
                    )
                  }
                />
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}

function MarginRow({
  margin,
  open,
  onToggle,
}: {
  margin: CustomerMargin;
  open: boolean;
  onToggle: () => void;
}) {
  const t = useTranslation();
  const format = useFormat();

  const overstated = marginIsOverstated(margin);
  const rate = marginRateAnswer(margin, (fraction) => format.percent(fraction));
  const tone = marginTone(margin);

  return (
    <Fragment>
      <TableRow
        className={cn(
          "hover:bg-slate-50/50",
          overstated && "bg-attention-50/40"
        )}
      >
        <TableCell className="text-sm">
          <Link
            href={`/admin/organizations/${margin.organizationId}`}
            className="text-slate-900 underline-offset-2 hover:underline"
          >
            {margin.organizationName}
          </Link>
        </TableCell>

        <TableCell className="text-right font-mono text-sm tabular-nums text-slate-900">
          {formatRevenue(margin.revenue, margin.currency)}
        </TableCell>

        <TableCell className="text-right font-mono text-sm tabular-nums text-slate-700">
          {formatRevenue(margin.cost, margin.currency)}
          {overstated && (
            <span className="block font-sans text-[11px] font-normal text-attention-700">
              {t("revenue.costIsIncomplete")}
            </span>
          )}
        </TableCell>

        <TableCell className="text-right font-mono text-sm tabular-nums text-slate-900">
          {formatRevenue(margin.margin, margin.currency)}
        </TableCell>

        <TableCell>
          <div className="flex flex-col items-start gap-1">
            <ToneBadge
              tone={tone}
              title={rate.known ? undefined : t(rate.reasonKey)}
            >
              {rate.text}
            </ToneBadge>
            {overstated && (
              <span className="text-[11px] text-attention-700">
                {t(COSTS_INCOMPLETE)}
              </span>
            )}
            {!rate.known && (
              <span className="text-[11px] text-slate-500">
                {t(rate.reasonKey)}
              </span>
            )}
          </div>
        </TableCell>

        <TableCell>
          <Button
            variant="ghost"
            size="sm"
            className="h-8 text-xs"
            onClick={onToggle}
          >
            {open ? t("reconciliation.hide") : t("revenue.lines")}
          </Button>
        </TableCell>
      </TableRow>

      {open && (
        <TableRow className="hover:bg-transparent">
          <TableCell colSpan={6} className="bg-slate-50/60 p-4">
            <MarginLines margin={margin} />
          </TableCell>
        </TableRow>
      )}
    </Fragment>
  );
}

function MarginLines({ margin }: { margin: CustomerMargin }) {
  const t = useTranslation();
  const format = useFormat();

  const uncosted = uncostedLines(margin);

  return (
    <div className="space-y-3">
      {uncosted.length > 0 && (
        <p className="flex items-start gap-2 rounded-lg border border-attention-200 bg-attention-50 p-3 text-xs text-attention-700">
          <TriangleAlert className="mt-px size-3.5 flex-shrink-0" />
          <span>{t(COSTS_INCOMPLETE_EXPLAINED)}</span>
        </p>
      )}

      <Table>
        <TableHeader>
          <TableRow className="hover:bg-transparent">
            <TableHead className="h-8">{t("revenue.resource")}</TableHead>
            <TableHead className="h-8 w-32 text-right">
              {t("revenue.count")}
            </TableHead>
            <TableHead className="h-8 w-40 text-right">
              {t("revenue.unitCost")}
            </TableHead>
            <TableHead className="h-8 w-36 text-right">
              {t("revenue.cost")}
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {margin.lines.map((line) => (
            <TableRow
              key={line.billableResource}
              className="hover:bg-transparent"
            >
              <TableCell className="py-2 text-xs text-slate-700">
                {t(resourceLabelKey(line.billableResource))}
              </TableCell>
              <TableCell className="py-2 text-right font-mono text-xs tabular-nums text-slate-600">
                {format.count(line.count)}
              </TableCell>
              <TableCell className="py-2 text-right font-mono text-xs tabular-nums">
                {line.unitCost === null ? (
                  <span
                    className="text-attention-700"
                    title={t("revenue.nobodyHasCostedThis")}
                  >
                    {t("revenue.noRate")}
                  </span>
                ) : (
                  formatRevenue(line.unitCost, margin.currency)
                )}
              </TableCell>
              <TableCell className="py-2 text-right font-mono text-xs tabular-nums text-slate-700">
                {formatRevenue(line.cost, margin.currency)}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
