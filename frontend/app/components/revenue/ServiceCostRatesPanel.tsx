"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Coins, Plus, TriangleAlert } from "lucide-react";

import type { BillableResource, ServiceCostRate } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { BILLABLE_RESOURCE_ORDER } from "@/lib/pricing";
import { CURRENCY_CODES, CURRENCY_LABELS } from "@/lib/currency";
import {
  RATES_ARE_APPENDED,
  RESOURCE_HAS_NO_RATE,
  formatRevenue,
  isValidUnitCost,
  resourceLabelKey,
  uncostedResources,
} from "@/lib/revenue";
import { canReadRevenue } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { useTranslation } from "@/context/LocalizationContext";
import Pagination from "@/components/Pagination";
import { LocalTime } from "@/components/LocalTime";
import { DateTimePicker } from "@/components/DateTimePicker";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { NeedsRevenuePermission } from "@/components/revenue/NeedsRevenuePermission";

export function ServiceCostRatesPanel({
  onUnauthorized,
}: {
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { grants, resolved } = usePermissions();
  const submission = useIdempotencyKey();

  const mayRead = canReadRevenue(grants, "service-cost-rates:read");
  const mayWrite = canReadRevenue(grants, "service-cost-rates:write");

  const [rates, setRates] = useState<ServiceCostRate[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [recordOpen, setRecordOpen] = useState(false);
  const [resource, setResource] = useState<BillableResource>("RuleEvaluation");
  const [unitCost, setUnitCost] = useState("");
  const [currency, setCurrency] = useState(CURRENCY_CODES[0]);
  const [effectiveFrom, setEffectiveFrom] = useState("");
  const [saving, setSaving] = useState(false);

  const fetchRates = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/service-cost-rates?PageNumber=${pageNumber}&PageSize=25`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<ServiceCostRate>(response);
        setRates(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("revenue.ratesCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-service-cost-rates",
        });
      }
    } catch {
      toast.error(t("revenue.ratesCouldNotBeRead"), {
        id: "error-fetching-service-cost-rates",
      });
    } finally {
      setLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageNumber, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchRates();
    else setLoading(false);
  }, [mayRead, fetchRates]);

  const record = async () => {
    if (!isValidUnitCost(unitCost)) return;
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/service-cost-rates`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            billableResource: resource,
            unitCost: Number(unitCost.trim()),
            currency,
            ...(effectiveFrom
              ? { effectiveFrom: new Date(effectiveFrom).toISOString() }
              : {}),
          }),
        },
        { idempotencyKey: submission.current("record") }
      );

      if (response.ok) {
        const created = await unwrap<ServiceCostRate>(response);
        submission.completed("record");

        toast.success(
          t("revenue.rateRecorded", t(resourceLabelKey(created.billableResource)))
        );

        setRecordOpen(false);
        setUnitCost("");
        setEffectiveFrom("");
        setPageNumber(1);
        fetchRates();
      } else {
        await reportApiError(response, {
          fallback: t("revenue.rateCouldNotBeRecorded"),
          onUnauthorized,
          toastId: "recording-service-cost-rate",
        });
      }
    } catch {
      toast.error(t("revenue.rateCouldNotBeRecorded"), {
        id: "recording-service-cost-rate",
      });
    } finally {
      setSaving(false);
    }
  };

  if (!resolved) return null;
  if (!mayRead) {
    return <NeedsRevenuePermission permission="service-cost-rates:read" />;
  }

  const uncosted =
    pageNumber === 1
      ? uncostedResources(rates, BILLABLE_RESOURCE_ORDER)
      : [];

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex items-start gap-2">
          <Coins className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("revenue.costRates")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {t("revenue.whatEachKindOfWorkCosts")}
            </p>
          </div>
        </div>

        {mayWrite && (
          <Button size="sm" onClick={() => setRecordOpen(true)}>
            <Plus />
            {t("revenue.recordARate")}
          </Button>
        )}
      </div>

      <p className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500">
        {t(RATES_ARE_APPENDED)}
      </p>

      {uncosted.length > 0 && !loading && (
        <p
          role="status"
          className="flex items-start gap-2 rounded-lg border border-attention-200 bg-attention-50 p-3 text-xs text-attention-700"
        >
          <TriangleAlert className="mt-px size-3.5 flex-shrink-0" />
          <span>
            {t(
              RESOURCE_HAS_NO_RATE,
              uncosted.map((item) => t(resourceLabelKey(item))).join(", ")
            )}
          </span>
        </p>
      )}

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("revenue.resource")}</TableHead>
              <TableHead className="w-40 text-right">
                {t("revenue.unitCost")}
              </TableHead>
              <TableHead className="w-48">
                {t("revenue.effectiveFrom")}
              </TableHead>
              <TableHead className="w-48">{t("revenue.recorded")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 3 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 4 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : rates.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="py-10 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {t("revenue.nothingCostedYet")}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    {t("revenue.everyMarginIsOverstatedUntilARateExists")}
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              rates.map((rate) => (
                <TableRow
                  key={rate.serviceCostRateId}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell className="text-sm text-slate-900">
                    {t(resourceLabelKey(rate.billableResource))}
                  </TableCell>
                  <TableCell className="text-right font-mono text-sm tabular-nums text-slate-900">
                    {formatRevenue(rate.unitCost, rate.currency)}
                  </TableCell>
                  <TableCell className="text-xs text-slate-600">
                    <LocalTime value={rate.effectiveFrom} />
                  </TableCell>
                  <TableCell className="text-xs text-slate-500">
                    <LocalTime value={rate.createdAt} />
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {totalPages > 1 && (
        <Pagination
          pageNumber={pageNumber}
          totalPages={totalPages}
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      <Dialog open={recordOpen} onOpenChange={setRecordOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("revenue.recordARate")}</DialogTitle>
            <DialogDescription>
              {t("revenue.whatItCostsToServeOneUnit")}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-3">
            <div className="flex flex-col gap-1">
              <span
                id="cost-rate-resource"
                className="font-mono text-[11px] uppercase tracking-widest text-slate-500"
              >
                {t("revenue.resource")}
              </span>
              <Select
                value={resource}
                onValueChange={(value) =>
                  setResource(value as BillableResource)
                }
              >
                <SelectTrigger aria-labelledby="cost-rate-resource">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {BILLABLE_RESOURCE_ORDER.map((item) => (
                    <SelectItem key={item} value={item}>
                      {t(resourceLabelKey(item))}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <div className="flex flex-col gap-1">
                <label
                  htmlFor="cost-rate-unit-cost"
                  className="font-mono text-[11px] uppercase tracking-widest text-slate-500"
                >
                  {t("revenue.unitCost")}
                </label>
                <Input
                  id="cost-rate-unit-cost"
                  inputMode="decimal"
                  value={unitCost}
                  placeholder="0.00"
                  onChange={(event) => setUnitCost(event.target.value)}
                />
                <p className="text-xs text-slate-500">
                  {t("revenue.zeroIsARealRate")}
                </p>
              </div>

              <div className="flex flex-col gap-1">
                <span
                  id="cost-rate-currency"
                  className="font-mono text-[11px] uppercase tracking-widest text-slate-500"
                >
                  {t("revenue.currency")}
                </span>
                <Select
                  value={currency}
                  onValueChange={(value) =>
                    setCurrency(value as (typeof CURRENCY_CODES)[number])
                  }
                >
                  <SelectTrigger aria-labelledby="cost-rate-currency">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {CURRENCY_CODES.map((code) => (
                      <SelectItem key={code} value={code}>
                        {CURRENCY_LABELS[code]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="flex flex-col gap-1">
              <span
                id="cost-rate-effective-from"
                className="font-mono text-[11px] uppercase tracking-widest text-slate-500"
              >
                {t("revenue.effectiveFrom")}
              </span>
              <DateTimePicker
                value={effectiveFrom}
                onChange={setEffectiveFrom}
                aria-label={t("revenue.effectiveFrom")}
              />
              <p className="text-xs text-slate-500">
                {t("revenue.leaveEmptyForNow")}
              </p>
            </div>
          </div>

          <DialogFooter>
            <Button
              onClick={record}
              disabled={saving || !isValidUnitCost(unitCost)}
            >
              {saving ? t("revenue.recording") : t("revenue.recordIt")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
