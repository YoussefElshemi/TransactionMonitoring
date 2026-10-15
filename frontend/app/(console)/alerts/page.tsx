"use client";

import { useCallback, useEffect, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { format, parseISO } from "date-fns";
import type { DateRange } from "react-day-picker";
import { toast } from "sonner";
import { X } from "lucide-react";

import {
  ALERT_STATUSES,
  AlertResource,
  Party,
  PartyResource,
  Rule,
  RuleResource,
} from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import { SEVERITY_FILTERS } from "@/lib/severity";
import { partyName } from "@/lib/parties";
import { shortId } from "@/lib/resources";
import { AlertTable } from "@/components/alerts/AlertTable";
import Pagination from "@/components/Pagination";
import { PageHeader } from "@/components/PageHeader";
import { TabBar } from "@/components/TabBar";
import { DateRangePicker } from "@/components/DateRangePicker";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const asDateParam = (date: Date): string => format(date, "yyyy-MM-dd");

const fromDateParam = (value: string | null): Date | undefined =>
  value ? parseISO(value) : undefined;

const STATUS_TABS = ["All", ...ALERT_STATUSES];

const ANY_SEVERITY = "All";

const Alerts = () => {
  const t = useTranslation();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [alerts, setAlerts] = useState<AlertResource[]>([]);
  const [pageNumber, setPageNumber] = useState(
    parseInt(searchParams.get("pageNumber") || "1")
  );
  const [pageSize, setPageSize] = useState(
    parseInt(searchParams.get("pageSize") || "10")
  );
  const [status, setStatus] = useState(searchParams.get("status") || "Open");
  const [severity, setSeverity] = useState(
    searchParams.get("severity") || ANY_SEVERITY
  );
  const [friendlyName, setFriendlyName] = useState(
    searchParams.get("friendlyName") || ""
  );
  const [ruleId, setRuleId] = useState(searchParams.get("ruleId") || "");
  const [partyId, setPartyId] = useState(searchParams.get("partyId") || "");
  const [range, setRange] = useState<DateRange | undefined>(() => {
    const from = fromDateParam(searchParams.get("fromDate"));
    return from
      ? { from, to: fromDateParam(searchParams.get("toDate")) }
      : undefined;
  });
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  const fromDate = range?.from ? asDateParam(range.from) : "";
  const toDate = range?.to ? asDateParam(range.to) : "";

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchAlerts = useCallback(async () => {
    setLoading(true);

    const params = new URLSearchParams({
      PageNumber: String(pageNumber),
      PageSize: String(pageSize),
    });
    if (friendlyName) params.set("FriendlyName", friendlyName);
    if (status !== "All") params.set("Status", status);
    if (severity !== ANY_SEVERITY) params.set("Severity", severity);
    if (ruleId) params.set("RuleId", ruleId);
    if (partyId) params.set("PartyId", partyId);
    if (fromDate) params.set("FromDate", fromDate);
    if (toDate) params.set("ToDate", toDate);

    try {
      const response = await apiFetch(`${API_BASE}/alerts?${params}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = await unwrapPage<AlertResource>(response);
        setAlerts(page.data);
        setTotalPages(page.metadata.pageCount || 1);
        setTotalCount(page.metadata.totalCount);
      } else {
        setAlerts([]);
        await reportApiError(response, {
          fallback: t("alerts.errorOccurredFetchingAlerts"),
          onUnauthorized: goToLogin,
          toastId: "error-fetching-alerts",
        });
      }
    } catch {
      setAlerts([]);
      toast.error(t("alerts.errorOccurredFetchingAlerts"), {
        id: "error-fetching-alerts",
      });
    } finally {
      setLoading(false);
    }
  }, [
    pageNumber,
    pageSize,
    friendlyName,
    status,
    severity,
    ruleId,
    partyId,
    fromDate,
    toDate,
    goToLogin,
  ]);

  useEffect(() => {
    fetchAlerts();
  }, [fetchAlerts]);

  useEffect(() => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    if (status !== "All") params.set("status", status);
    if (severity !== ANY_SEVERITY) params.set("severity", severity);
    if (friendlyName) params.set("friendlyName", friendlyName);
    if (ruleId) params.set("ruleId", ruleId);
    if (partyId) params.set("partyId", partyId);
    if (fromDate) params.set("fromDate", fromDate);
    if (toDate) params.set("toDate", toDate);
    router.replace(`?${params.toString()}`);
  }, [
    pageNumber,
    pageSize,
    status,
    severity,
    friendlyName,
    ruleId,
    partyId,
    fromDate,
    toDate,
    router,
  ]);

  return (
    <div className="space-y-4">
      <PageHeader
        eyebrow={t("alerts.reviewQueue")}
        title={t("alerts.alerts")}
        description={t("alerts.everyFindingARuleHas")}
      />

      <TabBar
        tabs={STATUS_TABS}
        activeTab={status || "All"}
        onChange={(tab) => {
          setStatus(tab);
          setPageNumber(1);
        }}
      />

      {status === "Suppressed" && (
        <p className="rounded-md border border-inflight-200 bg-inflight-50 px-3 py-2 text-xs text-inflight-700">
          These findings stood. Each one held or blocked its own payment exactly
          as its rule required — what was suppressed is the second entry in this
          queue, not the control on the money. Every row names the open alert it
          folded into.
        </p>
      )}

      <ActiveRelationFilters
        ruleId={ruleId}
        partyId={partyId}
        onClearRule={() => {
          setRuleId("");
          setPageNumber(1);
        }}
        onClearParty={() => {
          setPartyId("");
          setPageNumber(1);
        }}
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Input
            value={friendlyName}
            onChange={(e) => {
              setFriendlyName(e.target.value);
              setPageNumber(1);
            }}
            placeholder={t("common.searchByName")}
            className="w-56 font-mono"
          />
          <Select
            value={severity}
            onValueChange={(value) => {
              setSeverity(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_SEVERITY}>{t("filter.anySeverity")}</SelectItem>
              {SEVERITY_FILTERS.map((candidate) => (
                <SelectItem key={candidate} value={candidate}>
                  {candidate}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="flex items-center gap-3">
          <DateRangePicker
            value={range}
            onChange={(next) => {
              setRange(next);
              setPageNumber(1);
            }}
          />
          <PageSizeSelect
            value={pageSize}
            onChange={(next) => {
              setPageSize(next);
              setPageNumber(1);
            }}
          />
        </div>
      </div>

      {!loading && alerts.length > 0 && (
        <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
          {totalCount} {totalCount === 1 ? "alert" : "alerts"}
          {status !== "All" && ` · ${status.toLowerCase()}`}
          {" · newest raised first"}
        </p>
      )}

      <AlertTable
        alerts={alerts}
        loading={loading}
        emptyMessage={
          status === "Open"
            ? t("alerts.nothingOpenNoFindingsAre")
            : t("alerts.noAlertsFound")
        }
      />

      {alerts.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((p) => Math.min(totalPages, p + 1))
          }
        />
      )}
    </div>
  );
};

function ActiveRelationFilters({
  ruleId,
  partyId,
  onClearRule,
  onClearParty,
}: {
  ruleId: string;
  partyId: string;
  onClearRule: () => void;
  onClearParty: () => void;
}) {
  const t = useTranslation();
  const rule = useNamedRecord<RuleResource, Rule>(
    ruleId,
    "rules",
    (resource) => resource.rule
  );
  const party = useNamedRecord<PartyResource, Party>(
    partyId,
    "parties",
    (resource) => resource.party
  );

  if (!ruleId && !partyId) return null;

  return (
    <div className="flex flex-wrap items-center gap-2">
      <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {t("alerts.filteredTo")}
      </span>
      {ruleId && (
        <FilterChip
          label={t("common.rule")}
          value={rule?.friendlyName ?? shortId(ruleId)}
          href={`/rules/${ruleId}`}
          onClear={onClearRule}
        />
      )}
      {partyId && (
        <FilterChip
          label={t("alerts.party")}
          value={party ? partyName(party) : shortId(partyId)}
          href={`/parties/${partyId}`}
          onClear={onClearParty}
        />
      )}
    </div>
  );
}

function FilterChip({
  label,
  value,
  href,
  onClear,
}: {
  label: string;
  value: string;
  href: string;
  onClear: () => void;
}) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-md border border-brand-200 bg-brand-50 py-0.5 pl-2 pr-0.5 text-xs">
      <span className="font-mono uppercase tracking-wide text-brand-800/60">
        {label}
      </span>
      <Link href={href} className="font-medium text-brand-900 hover:underline">
        {value}
      </Link>
      <Button
        variant="ghost"
        size="icon"
        // `size-6` rather than `size-5`: 20x20 is under the 24x24 floor that
        // WCAG 2.2 AA 2.5.8 sets for a pointer target.
        className="size-6 text-brand-800/60 hover:bg-brand-100 hover:text-brand-900"
        onClick={onClear}
        title={`Clear the ${label.toLowerCase()} filter`}
        aria-label={`Clear the ${label.toLowerCase()} filter`}
      >
        <X className="size-3" />
      </Button>
    </span>
  );
}

const useNamedRecord = <TResource, TRecord>(
  id: string,
  collection: string,
  read: (resource: TResource) => TRecord
): TRecord | null => {
  const [record, setRecord] = useState<TRecord | null>(null);

  useEffect(() => {
    if (!id) {
      setRecord(null);
      return;
    }

    let live = true;

    void (async () => {
      try {
        const response = await apiFetch(`${API_BASE}/${collection}/${id}`, {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        });

        if (!response.ok) return;

        const resource = await unwrap<TResource>(response);
        if (live) setRecord(read(resource));
      } catch {
      }
    })();

    return () => {
      live = false;
    };
    // `read` is an inline arrow from the caller and would restart this on every
    // render; the id and the collection are what actually identify the record.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id, collection]);

  return record;
};

export default Alerts;
