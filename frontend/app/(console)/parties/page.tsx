"use client";

import { useCallback, useEffect, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { format, parseISO } from "date-fns";
import type { DateRange } from "react-day-picker";
import { toast } from "sonner";
import { Eye } from "lucide-react";

import { Party, PartyResource, PARTY_ROLES } from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import { usePermissions } from "@/context/PermissionsContext";
import Pagination from "@/components/Pagination";
import { PageHeader } from "@/components/PageHeader";
import { DateRangePicker } from "@/components/DateRangePicker";
import {
  NoPartiesYet,
  PartyLink,
  RiskScore,
  RoleChips,
} from "@/components/parties/PartyParts";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import { useTranslation } from "@/context/LocalizationContext";

const asDateParam = (date: Date): string => format(date, "yyyy-MM-dd");

const fromDateParam = (value: string | null): Date | undefined =>
  value ? parseISO(value) : undefined;

const ANY_ROLE = "All";

const Parties = () => {
  const t = useTranslation();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { holds } = usePermissions();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [parties, setParties] = useState<Party[]>([]);
  const [pageNumber, setPageNumber] = useState(
    parseInt(searchParams.get("pageNumber") || "1")
  );
  const [pageSize, setPageSize] = useState(
    parseInt(searchParams.get("pageSize") || "10")
  );
  const [identifier, setIdentifier] = useState(
    searchParams.get("identifier") || ""
  );
  const [displayName, setDisplayName] = useState(
    searchParams.get("displayName") || ""
  );
  const [role, setRole] = useState<string>(
    searchParams.get("role") || ANY_ROLE
  );
  const [range, setRange] = useState<DateRange | undefined>(() => {
    const from = fromDateParam(searchParams.get("fromDate"));
    return from
      ? { from, to: fromDateParam(searchParams.get("toDate")) }
      : undefined;
  });
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const fromDate = range?.from ? asDateParam(range.from) : "";
  const toDate = range?.to ? asDateParam(range.to) : "";

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchParties = useCallback(async () => {
    setLoading(true);

    const params = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
    });
    if (identifier) params.set("identifier", identifier);
    if (displayName) params.set("displayName", displayName);
    if (role !== ANY_ROLE) params.set("role", role);
    if (fromDate) params.set("fromDate", fromDate);
    if (toDate) params.set("toDate", toDate);

    try {
      const response = await apiFetch(`${API_BASE}/parties?${params}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = await unwrapPage<PartyResource>(response);
        setParties(page.data.map((resource) => resource.party));
        setTotalPages(page.metadata.pageCount || 1);
      } else {
        setParties([]);
        await reportApiError(response, {
          fallback: t("parties.errorOccurredFetchingParties"),
          onUnauthorized: goToLogin,
          toastId: "error-fetching-parties",
        });
      }
    } catch {
      setParties([]);
      toast.error(t("parties.errorOccurredFetchingParties"), {
        id: "error-fetching-parties",
      });
    } finally {
      setLoading(false);
    }
  }, [
    pageNumber,
    pageSize,
    identifier,
    displayName,
    role,
    fromDate,
    toDate,
    goToLogin,
  ]);

  useEffect(() => {
    fetchParties();
  }, [fetchParties]);

  useEffect(() => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    if (identifier) params.set("identifier", identifier);
    if (displayName) params.set("displayName", displayName);
    if (role !== ANY_ROLE) params.set("role", role);
    if (fromDate) params.set("fromDate", fromDate);
    if (toDate) params.set("toDate", toDate);
    router.replace(`?${params.toString()}`);
  }, [
    pageNumber,
    pageSize,
    identifier,
    displayName,
    role,
    fromDate,
    toDate,
    router,
  ]);

  const filtered = Boolean(
    identifier || displayName || role !== ANY_ROLE || range?.from
  );

  return (
    <div className="space-y-4">
      <PageHeader
        eyebrow={t("parties.monitoring")}
        title={t("parties.parties")}
        description={t("parties.everyoneYourPayloadsHaveNamed")}
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Input
            value={identifier}
            onChange={(e) => {
              setIdentifier(e.target.value);
              setPageNumber(1);
            }}
            placeholder={t("parties.searchByIdentifier")}
            className="w-56 font-mono"
          />
          <Input
            value={displayName}
            onChange={(e) => {
              setDisplayName(e.target.value);
              setPageNumber(1);
            }}
            placeholder={t("common.searchByName")}
            className="w-48"
          />
          <Select
            value={role}
            onValueChange={(value) => {
              setRole(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_ROLE}>{t("parties.anyRole")}</SelectItem>
              {PARTY_ROLES.map((candidate) => (
                <SelectItem key={candidate} value={candidate}>
                  Seen as {candidate}
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

      {loading ? (
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
          <Table>
            <TableBody>
              {Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 6 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      ) : parties.length === 0 ? (
        <NoPartiesYet
          canManage={holds("party-mappings:write")}
          filtered={filtered}
        />
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
          <Table>
            <TableHeader>
              <TableRow className="bg-slate-50 hover:bg-slate-50">
                <TableHead>{t("parties.party2")}</TableHead>
                <TableHead className="w-56">{t("parties.seenAs")}</TableHead>
                <TableHead className="w-28">{t("parties.risk")}</TableHead>
                <TableHead>{t("parties.firstSeen")}</TableHead>
                <TableHead>{t("parties.lastSeen")}</TableHead>
                <TableHead className="w-28">{t("common.actions")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {parties.map((party) => (
                <TableRow key={party.partyId} className="hover:bg-slate-50/50">
                  <TableCell className="max-w-xs">
                    <PartyLink
                      partyId={party.partyId}
                      identifier={party.identifier}
                      displayName={party.displayName}
                    />
                  </TableCell>
                  <TableCell>
                    <RoleChips roles={party.roles} />
                  </TableCell>
                  <TableCell>
                    <RiskScore score={party.riskScore} />
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">
                    <LocalTime value={party.firstSeenAt} />
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">
                    <LocalTime value={party.lastSeenAt} />
                  </TableCell>
                  <TableCell>
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      onClick={() => router.push(`/parties/${party.partyId}`)}
                    >
                      <Eye className="size-4" />
                      {t("common.view")}
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {parties.length > 0 && (
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

export default Parties;
