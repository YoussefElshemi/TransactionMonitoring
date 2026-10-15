"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Bot, Plus, UserPen } from "lucide-react";

import type { CurrencyCode, ExchangeRate } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { CURRENCY_CODES, CURRENCY_LABELS } from "@/lib/currency";
import {
  EFFECTIVE_FROM_HINT,
  FED_RATE_HINT,
  FED_RATE_LABEL,
  MANUAL_RATE_LABEL,
  MISSING_RATE_REFUSES_THE_INVOICE,
  RATES_ARE_APPENDED,
  RATE_REFUSAL,
  SAME_CURRENCY_MESSAGE,
  formatExchangeRate,
  isFedRate,
  isValidRate,
  pairLabel,
  rateSentence,
  ratesByPair,
} from "@/lib/exchangeRates";
import { apiFetch } from "@/lib/http";
import { canWriteBilling } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import Pagination from "@/components/Pagination";
import { LocalTime } from "@/components/LocalTime";
import { PageHeader } from "@/components/PageHeader";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { DateTimePicker } from "@/components/DateTimePicker";
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
import { useTranslation } from "@/context/LocalizationContext";

const ExchangeRates = () => {
  const t = useTranslation();
  const { allowed } = useRequirePermission(
    ["exchange-rates:read", "platform:administer"],
    "/"
  );

  const router = useRouter();
  const { grants } = usePermissions();
  const submission = useIdempotencyKey();

  const canWrite = canWriteBilling(grants, "exchange-rates:write");

  const [rates, setRates] = useState<ExchangeRate[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [fromFilter, setFromFilter] = useState<CurrencyCode | "any">("any");
  const [toFilter, setToFilter] = useState<CurrencyCode | "any">("any");

  const [recordOpen, setRecordOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const [fromCurrency, setFromCurrency] = useState<CurrencyCode>("GBP");
  const [toCurrency, setToCurrency] = useState<CurrencyCode>("EUR");
  const [rate, setRate] = useState("");
  const [effectiveFrom, setEffectiveFrom] = useState("");

  const fetchRates = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/exchange-rates?PageNumber=${pageNumber}&PageSize=100` +
          (fromFilter === "any" ? "" : `&FromCurrency=${fromFilter}`) +
          (toFilter === "any" ? "" : `&ToCurrency=${toFilter}`),
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<ExchangeRate>(response);
        setRates(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("exchangeRates.errorOccurredFetchingExchangeRates"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-fetching-exchange-rates",
        });
      }
    } catch {
      toast.error(t("exchangeRates.errorOccurredFetchingExchangeRates"), {
        id: "error-fetching-exchange-rates",
      });
    } finally {
      setLoading(false);
    }
  }, [pageNumber, fromFilter, toFilter, router]);

  useEffect(() => {
    if (allowed) fetchRates();
  }, [allowed, fetchRates]);

  const sameCurrency = fromCurrency === toCurrency;
  const rateValid = isValidRate(Number(rate.trim()));

  const record = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/exchange-rates`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            fromCurrency,
            toCurrency,
            rate: Number(rate.trim()),
            ...(effectiveFrom.trim().length > 0
              ? { effectiveFrom: new Date(effectiveFrom).toISOString() }
              : {}),
          }),
        },
        { idempotencyKey: submission.current("record") }
      );

      if (response.ok) {
        const created = await unwrap<ExchangeRate>(response);
        submission.completed("record");

        toast.success(
          t(
            "exchangeRates.pairRecorded",
            pairLabel(created.fromCurrency, created.toCurrency)
          ),
          { description: rateSentence(created) }
        );

        setRecordOpen(false);
        setRate("");
        setEffectiveFrom("");
        fetchRates();
      } else if (response.status === 400 && sameCurrency) {
        toast.error(t("exchangeRates.currencyDoesNotConvertItself"), {
          description: SAME_CURRENCY_MESSAGE,
        });
      } else {
        await reportApiError(response, {
          fallback: t("exchangeRates.rateCouldNotRecorded"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-recording-exchange-rate",
        });
      }
    } catch {
      toast.error(t("exchangeRates.rateCouldNotRecorded"), {
        id: "error-recording-exchange-rate",
      });
    } finally {
      setSaving(false);
    }
  };

  if (!allowed) return null;

  const pairs = ratesByPair(rates);

  return (
    <div className="p-6">
      <PageHeader
        eyebrow={t("exchangeRates.admin")}
        title={t("exchangeRates.exchangeRates")}
        description={t("exchangeRates.whatCarriesAPlanPriced")}
      >
        {canWrite && (
          <Button size="sm" onClick={() => setRecordOpen(true)}>
            <Plus />
            {t("exchangeRates.recordARate")}
          </Button>
        )}
      </PageHeader>

      <div className="mb-4 space-y-1">
        <p className="max-w-3xl text-sm text-slate-600">
          {RATES_ARE_APPENDED}
        </p>
        <p className="max-w-3xl text-xs text-slate-500">
          {MISSING_RATE_REFUSES_THE_INVOICE}
        </p>
      </div>

      <div className="mb-3 flex flex-wrap items-center gap-2">
        <Select
          value={fromFilter}
          onValueChange={(value) => {
            setFromFilter(value as CurrencyCode | "any");
            setPageNumber(1);
          }}
        >
          <SelectTrigger className="w-40">
            <SelectValue placeholder={t("common.from")} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t("exchangeRates.fromAny")}</SelectItem>
            {CURRENCY_CODES.map((code) => (
              <SelectItem key={code} value={code}>
                From: {code}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select
          value={toFilter}
          onValueChange={(value) => {
            setToFilter(value as CurrencyCode | "any");
            setPageNumber(1);
          }}
        >
          <SelectTrigger className="w-40">
            <SelectValue placeholder="To" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t("exchangeRates.toAny")}</SelectItem>
            {CURRENCY_CODES.map((code) => (
              <SelectItem key={code} value={code}>
                To: {code}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {loading ? (
        <div className="space-y-2">
          <Skeleton className="h-32 w-full" />
          <Skeleton className="h-32 w-full" />
        </div>
      ) : pairs.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-card p-6">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("exchangeRates.noRates")}
          </p>
          <p className="mt-2 max-w-2xl text-sm text-slate-500">
            Nothing has been recorded for these currencies. Any customer whose
            plan is priced in one currency and who is billed in another cannot
            be invoiced until there is a rate in force at the end of the month —
            the generate endpoint refuses rather than converting at today&apos;s.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {pairs.map((pair) => (
            <div
              key={pair.key}
              className="overflow-hidden rounded-xl border border-slate-200 bg-card"
            >
              <div className="flex flex-wrap items-baseline justify-between gap-3 border-b border-slate-200 bg-slate-50 px-4 py-2.5">
                <p className="font-mono text-sm text-slate-900">
                  {pairLabel(pair.fromCurrency, pair.toCurrency)}
                </p>
                <p className="text-xs text-slate-500">
                  In force now: {rateSentence(pair.rates[0])}
                </p>
              </div>

              <Table>
                <TableHeader>
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="w-44">{t("exchangeRates.effectiveFrom")}</TableHead>
                    <TableHead className="w-40 text-right">{t("exchangeRates.rate")}</TableHead>
                    <TableHead>{t("exchangeRates.whatOneUnitBuys")}</TableHead>
                    <TableHead className="w-44">{t("exchangeRates.recordedBy")}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {pair.rates.map((entry, index) => (
                    <TableRow
                      key={entry.exchangeRateId}
                      className="hover:bg-slate-50/50"
                    >
                      <TableCell className="text-sm text-slate-700">
                        <LocalTime value={entry.effectiveFrom} />
                        {index === 0 && (
                          <span className="block text-xs text-slate-400">
                            in force
                          </span>
                        )}
                      </TableCell>

                      <TableCell className="text-right font-mono text-sm text-slate-900">
                        {formatExchangeRate(entry.rate)}
                      </TableCell>

                      <TableCell className="text-xs text-slate-600">
                        {rateSentence(entry)}
                      </TableCell>

                      <TableCell className="text-xs">
                        {isFedRate(entry) ? (
                          <span
                            className="flex items-center gap-1.5 text-slate-500"
                            title={FED_RATE_HINT}
                          >
                            <Bot className="size-3.5 flex-shrink-0" />
                            {FED_RATE_LABEL}
                          </span>
                        ) : (
                          <span className="flex items-center gap-1.5 text-slate-700">
                            <UserPen className="size-3.5 flex-shrink-0" />
                            {MANUAL_RATE_LABEL}
                          </span>
                        )}
                        <span className="mt-0.5 block text-slate-400">
                          <LocalTime value={entry.createdAt} dateOnly />
                        </span>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          ))}

          <p className="text-xs text-slate-400">{FED_RATE_HINT}</p>
        </div>
      )}

      {pairs.length > 0 && totalPages > 1 && (
        <div className="mt-3">
          <Pagination
            handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
            pageNumber={pageNumber}
            totalPages={totalPages}
            handleNextPage={() =>
              setPageNumber((page) => Math.min(totalPages, page + 1))
            }
          />
        </div>
      )}

      <Dialog open={recordOpen} onOpenChange={setRecordOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("exchangeRates.recordARate")}</DialogTitle>
            <DialogDescription>{RATES_ARE_APPENDED}</DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("common.from")}
                </label>
                <Select
                  value={fromCurrency}
                  onValueChange={(value) =>
                    setFromCurrency(value as CurrencyCode)
                  }
                  disabled={saving}
                >
                  <SelectTrigger>
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

              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  To
                </label>
                <Select
                  value={toCurrency}
                  onValueChange={(value) => setToCurrency(value as CurrencyCode)}
                  disabled={saving}
                >
                  <SelectTrigger>
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

            {sameCurrency && (
              <p className="text-xs text-critical-500">
                {SAME_CURRENCY_MESSAGE}
              </p>
            )}

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("exchangeRates.rate")}
              </label>
              <Input
                value={rate}
                onChange={(event) => setRate(event.target.value)}
                inputMode="decimal"
                className="font-mono"
                disabled={saving}
              />
              {rateValid ? (
                <p className="text-xs text-slate-500">
                  {rateSentence({
                    fromCurrency,
                    toCurrency,
                    rate: Number(rate.trim()),
                  })}
                </p>
              ) : (
                rate.trim().length > 0 && (
                  <p className="text-xs text-critical-500">{RATE_REFUSAL}</p>
                )
              )}
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("exchangeRates.effectiveFrom")}
              </label>
              <DateTimePicker
                value={effectiveFrom}
                onChange={setEffectiveFrom}
                placeholder={t("exchangeRates.effectiveNow")}
                aria-label={t("exchangeRates.exchangeRateEffectiveFrom")}
                disabled={saving}
              />
              <p className="text-xs text-slate-500">{EFFECTIVE_FROM_HINT}</p>
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => setRecordOpen(false)}
              disabled={saving}
            >
              {t("common.cancel")}
            </Button>
            <Button
              onClick={record}
              disabled={saving || sameCurrency || !rateValid}
            >
              {saving ? "Recording..." : "Record"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default ExchangeRates;
