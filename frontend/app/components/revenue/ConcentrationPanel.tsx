"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { PieChart } from "lucide-react";

import type { RevenueConcentration } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import {
  CONCENTRATION_READING_LABELS,
  CUSTOMER_COUNT_IS_NOT_A_TENANT_COUNT,
  DEFAULT_TOP_COUNT,
  MAX_TOP_COUNT,
  MONTH_BILLED_NOTHING,
  type MonthPoint,
  answered,
  clampTopCount,
  concentrationReading,
  concentrationTone,
  equivalentCustomers,
  formatRevenue,
  monthHasRevenue,
} from "@/lib/revenue";
import { canReadRevenue } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useFormat } from "@/lib/useFormat";
import { useLocalization, useTranslation } from "@/context/LocalizationContext";
import { ToneBadge } from "@/components/status/StatusChips";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
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

const TOP_COUNTS = [10, DEFAULT_TOP_COUNT, 50, 100, MAX_TOP_COUNT];

export function ConcentrationPanel({
  onUnauthorized,
}: {
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { locale } = useLocalization();
  const format = useFormat();
  const { grants, resolved } = usePermissions();

  const mayRead = canReadRevenue(grants);

  const [point, setPoint] = useState<MonthPoint>(() => lastCompleteMonth());
  const [topCount, setTopCount] = useState(DEFAULT_TOP_COUNT);
  const [concentration, setConcentration] =
    useState<RevenueConcentration | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchConcentration = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        month: String(point.month),
        year: String(point.year),
        topCount: String(clampTopCount(topCount)),
      });

      const response = await apiFetch(
        `${API_BASE}/revenue/concentration?${query}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        setConcentration(await unwrap<RevenueConcentration>(response));
      } else {
        await reportApiError(response, {
          fallback: t("revenue.concentrationCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-revenue-concentration",
        });
      }
    } catch {
      toast.error(t("revenue.concentrationCouldNotBeRead"), {
        id: "error-fetching-revenue-concentration",
      });
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [point.month, point.year, topCount, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchConcentration();
    else setLoading(false);
  }, [mayRead, fetchConcentration]);

  if (!resolved) return null;
  if (!mayRead) return <NeedsRevenuePermission permission="revenue:read" />;

  const hasRevenue = concentration !== null && monthHasRevenue(concentration);
  const reading =
    concentration === null
      ? null
      : concentrationReading(concentration.concentrationIndex);
  const equivalent =
    concentration === null
      ? null
      : equivalentCustomers(concentration.concentrationIndex);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex items-start gap-2">
          <PieChart className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("revenue.concentration")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {t("revenue.howMuchRestsOnHowFew")}
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-end gap-3">
          <MonthField
            label={t("revenue.month")}
            value={point}
            onChange={setPoint}
            disabled={loading}
          />
          <div className="flex flex-col gap-1">
            <span
              id="revenue-top-count"
              className="font-mono text-[11px] uppercase tracking-widest text-slate-500"
            >
              {t("revenue.listLength")}
            </span>
            <Select
              value={String(topCount)}
              disabled={loading}
              onValueChange={(value) => setTopCount(Number(value))}
            >
              <SelectTrigger
                className="h-8 w-28 text-xs"
                aria-labelledby="revenue-top-count"
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {TOP_COUNTS.map((count) => (
                  <SelectItem key={count} value={String(count)}>
                    {t("revenue.topCustomers", format.count(count))}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>

      {loading || concentration === null ? (
        <Skeleton className="h-56 w-full rounded-xl" />
      ) : !hasRevenue ? (
        <div className="rounded-xl border border-slate-200 bg-card p-10 text-center">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("revenue.nothingBilled", monthLabel(concentration, locale))}
          </p>
          <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
            {t(MONTH_BILLED_NOTHING)}
          </p>
        </div>
      ) : (
        <>
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <Tile
              label={t("revenue.totalRevenue")}
              value={formatRevenue(
                concentration.totalRevenue,
                concentration.currency
              )}
            />
            <Tile
              label={t("revenue.contributingCustomers")}
              value={format.count(concentration.customerCount)}
              hint={t(CUSTOMER_COUNT_IS_NOT_A_TENANT_COUNT)}
            />
            <ShareTile
              label={t("revenue.largestCustomer")}
              share={concentration.largestCustomerShare}
            />
            <ShareTile
              label={t("revenue.topFive")}
              share={concentration.topFiveShare}
            />
          </div>

          <div className="grid gap-3 sm:grid-cols-2">
            <ShareTile
              label={t("revenue.topTen")}
              share={concentration.topTenShare}
            />

            <div className="rounded-xl border border-slate-200 bg-card p-4">
              <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                {t("revenue.howConcentrated")}
              </p>
              {equivalent === null || reading === null ? (
                <p className="mt-2 font-mono text-2xl text-slate-400">—</p>
              ) : (
                <>
                  <p className="mt-2 text-sm text-slate-900">
                    {t(
                      "revenue.asConcentratedAsEqualCustomers",
                      format.decimal(equivalent, {
                        maximumFractionDigits: 1,
                      })
                    )}
                  </p>
                  <div className="mt-2 flex flex-wrap items-center gap-2">
                    <ToneBadge tone={concentrationTone(reading)}>
                      {t(CONCENTRATION_READING_LABELS[reading])}
                    </ToneBadge>
                    <span className="font-mono text-[11px] text-slate-400">
                      {t(
                        "revenue.herfindahlIndexIs",
                        format.decimal(concentration.concentrationIndex ?? 0, {
                          maximumFractionDigits: 3,
                        })
                      )}
                    </span>
                  </div>
                  <p className="mt-2 text-xs text-slate-500">
                    {t("revenue.concentrationIsNotAFault")}
                  </p>
                </>
              )}
            </div>
          </div>

          <CustomerTable concentration={concentration} />
        </>
      )}
    </div>
  );
}

function Tile({
  label,
  value,
  hint,
}: {
  label: string;
  value: string;
  hint?: string;
}) {
  return (
    <div className="rounded-xl border border-slate-200 bg-card p-4">
      <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {label}
      </p>
      <p className="mt-2 font-mono text-2xl tabular-nums text-slate-900">
        {value}
      </p>
      {hint && <p className="mt-1.5 text-xs text-slate-500">{hint}</p>}
    </div>
  );
}

function ShareTile({ label, share }: { label: string; share: number | null }) {
  const t = useTranslation();
  const format = useFormat();

  const answer = answered(
    share,
    (fraction) => format.percent(fraction),
    "nothingBilled"
  );

  return (
    <div className="rounded-xl border border-slate-200 bg-card p-4">
      <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {label}
      </p>
      <p
        className={`mt-2 font-mono text-2xl tabular-nums ${
          answer.known ? "text-slate-900" : "text-slate-400"
        }`}
        title={answer.known ? undefined : t(answer.reasonKey)}
      >
        {answer.text}
      </p>
      {!answer.known && (
        <p className="mt-1.5 text-xs text-slate-500">{t(answer.reasonKey)}</p>
      )}
    </div>
  );
}

function CustomerTable({
  concentration: month,
}: {
  concentration: RevenueConcentration;
}) {
  const t = useTranslation();
  const { locale } = useLocalization();
  const format = useFormat();

  const largest = month.customers[0]?.share ?? 1;

  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
      <Table>
        <caption className="sr-only">
          {t("revenue.concentrationTableCaption", monthLabel(month, locale))}
        </caption>
        <TableHeader>
          <TableRow className="bg-slate-50 hover:bg-slate-50">
            <TableHead className="w-16 text-right">
              {t("revenue.rank")}
            </TableHead>
            <TableHead>{t("reconciliation.customer")}</TableHead>
            <TableHead className="w-40 text-right">
              {t("revenue.revenue")}
            </TableHead>
            <TableHead className="w-56">{t("revenue.share")}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {month.customers.map((customer) => (
            <TableRow
              key={customer.organizationId}
              className="hover:bg-slate-50/50"
            >
              <TableCell className="text-right font-mono text-xs tabular-nums text-slate-500">
                {format.count(customer.rank)}
              </TableCell>
              <TableCell className="text-sm">
                <Link
                  href={`/admin/organizations/${customer.organizationId}`}
                  className="text-slate-900 underline-offset-2 hover:underline"
                >
                  {customer.organizationName}
                </Link>
              </TableCell>
              <TableCell className="text-right font-mono text-sm tabular-nums text-slate-900">
                {formatRevenue(customer.revenue, month.currency)}
              </TableCell>
              <TableCell>
                <div className="flex items-center gap-2">
                  <span
                    aria-hidden="true"
                    className="h-2 flex-1 overflow-hidden rounded-full bg-slate-100"
                  >
                    <span
                      className="block h-full rounded-full bg-attention-700"
                      style={{
                        width: `${Math.max(
                          2,
                          (customer.share / (largest || 1)) * 100
                        )}%`,
                      }}
                    />
                  </span>
                  <span className="w-14 text-right font-mono text-xs tabular-nums text-slate-600">
                    {format.percent(customer.share)}
                  </span>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
