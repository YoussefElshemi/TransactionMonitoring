"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import {
  useParams,
  usePathname,
  useRouter,
  useSearchParams,
} from "next/navigation";
import { toast } from "sonner";
import { Siren } from "lucide-react";

import { PartyResource, PARTY_ROLES, TransactionSummary } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { loginHref } from "@/lib/authRedirect";
import { partyName, paysThemselves } from "@/lib/parties";
import { useAuth } from "@/context/AuthContext";
import { usePermissions } from "@/context/PermissionsContext";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { Button } from "@/components/ui/button";
import Pagination from "@/components/Pagination";
import { TabBar } from "@/components/TabBar";
import { TransactionReference } from "@/components/TransactionReference";
import { HistoryPanel, historyPath } from "@/components/history";
import {
  RiskScore,
  RoleChips,
  SelfPaymentNote,
} from "@/components/parties/PartyParts";
import { PartyCaseHistory } from "@/components/parties/PartyCaseHistory";
import { RiskAssessmentPanel } from "@/components/parties/RiskAssessmentPanel";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const PAGE_SIZE = 10;
const ANY = "All";

const PartyDetails = () => {
  const t = useTranslation();
  const { partyId } = useParams();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { user } = useAuth();
  const { holds, holdsAll } = usePermissions();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [resource, setResource] = useState<PartyResource | null>(null);
  const party = resource?.party ?? null;
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState("Transactions");

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  useEffect(() => {
    const fetchParty = async () => {
      setLoading(true);

      try {
        const response = await apiFetch(`${API_BASE}/parties/${partyId}`, {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        });

        if (response.ok) {
          setResource(await unwrap<PartyResource>(response));
        } else if (response.status === 404) {
          setResource(null);
        } else {
          setResource(null);
          await reportApiError(response, {
            fallback: t("parties.errorOccurredFetchingParty"),
            onUnauthorized: goToLogin,
            toastId: "error-fetching-party",
          });
        }
      } catch {
        setResource(null);
        toast.error(t("parties.errorOccurredFetchingParty"), {
          id: "error-fetching-party",
        });
      } finally {
        setLoading(false);
      }
    };

    fetchParty();
  }, [partyId, goToLogin]);

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <Card>
          <CardContent className="space-y-2 pt-6">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-4 w-full" />
            ))}
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!party) {
    return (
      <NotFound
        title={t("parties.partyNotFound")}
        description={t("parties.noPartyWithThatId")}
        backHref="/parties"
        backLabel={t("parties.allParties")}
      />
    );
  }

  const tabs = ["Transactions", "Cases"];
  if (holdsAll("parties:read", "audit-logs:read")) tabs.push("History");

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("parties.party")} title={partyName(party)}>
        <Button variant="outline" size="sm" className="gap-1.5" asChild>
          <Link href={`/alerts?partyId=${party.partyId}&status=All`}>
            <Siren className="size-4" />
            {t("parties.alerts")}
          </Link>
        </Button>
        <RiskScore score={party.riskScore} className="text-sm" />
      </PageHeader>

      {paysThemselves(party.roles) && <SelfPaymentNote />}

      <Card>
        <CardHeader>
          <CardTitle className="text-base font-semibold">{t("common.details")}</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-x-8 gap-y-3 text-sm">
          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("parties.identifier")}</span>
            <p className="mt-0.5 break-all font-mono text-slate-900">
              {party.identifier}
            </p>
            <p className="mt-0.5 text-xs text-slate-400">
              {t("parties.comparedCaseSensitivelyThisExact")}
            </p>
          </div>
          <div className="min-w-0">
            <span className="font-medium text-slate-500">{t("common.name")}</span>
            {party.displayName ? (
              <p className="mt-0.5 break-words text-slate-900">
                {party.displayName}
              </p>
            ) : (
              <p className="mt-0.5 text-slate-400">
                None — the mapping declares no display name, or the payloads
                carried none
              </p>
            )}
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("parties.seenAs")}</span>
            <div className="mt-1">
              <RoleChips roles={party.roles} />
            </div>
            <p className="mt-1 text-xs text-slate-400">
              Every role this party has ever stood in. The role belongs to each
              payment, not to the party.
            </p>
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("parties.knownSince")}</span>
            <p className="mt-0.5 text-slate-900">
              <LocalTime value={party.firstSeenAt} />
            </p>
            <p className="mt-0.5 text-slate-500">
              {t("parties.lastSeen")} <LocalTime value={party.lastSeenAt} />
            </p>
          </div>
        </CardContent>
      </Card>

      <RiskAssessmentPanel
        party={party}
        currentUserId={user?.userId}
        canAssess={holds("parties:write")}
        onAssessed={setResource}
        onUnauthorized={goToLogin}
      />

      <Card>
        <CardHeader>
          <CardTitle className="text-base font-semibold">
            {t("parties.observedAttributes")}
          </CardTitle>
        </CardHeader>
        <CardContent>
          {party.attributes.length === 0 ? (
            <p className="text-sm text-slate-400">
              None. Attributes are whatever the mapping collects beside the
              identifier — a country, a bank, a device. Nothing is collected
              unless it is declared.
            </p>
          ) : (
            <>
              <dl className="grid grid-cols-2 gap-x-8 gap-y-2 text-sm">
                {party.attributes.map((attribute) => (
                  <div key={attribute.name} className="min-w-0">
                    <dt className="font-mono text-xs uppercase tracking-widest text-slate-400">
                      {attribute.name}
                    </dt>
                    <dd className="mt-0.5 break-words font-mono text-slate-900">
                      {attribute.value}
                    </dd>
                  </div>
                ))}
              </dl>
              <p className="mt-3 text-xs text-slate-400">
                As the payloads carried them. These are for you to read — a rule
                addresses the payload in front of it rather than a stored copy of
                an earlier one.
              </p>
            </>
          )}
        </CardContent>
      </Card>

      <div className="space-y-4">
        <TabBar tabs={tabs} activeTab={activeTab} onChange={setActiveTab} />

        {activeTab === "Transactions" && (
          <PartyTransactions partyId={party.partyId} onUnauthorized={goToLogin} />
        )}

        {activeTab === "Cases" && (
          <PartyCaseHistory
            partyId={party.partyId}
            onUnauthorized={goToLogin}
          />
        )}

        {activeTab === "History" && (
          <HistoryPanel
            entityType="Party"
            path={historyPath.Party(party.partyId)}
            entityId={party.partyId}
            onUnauthorized={goToLogin}
            scopeNote="Only the risk assessments are recorded here. Being seen on a payment is the engine's work rather than a person's, so it moves the dates above without writing an entry."
          />
        )}
      </div>
    </div>
  );
};

function PartyTransactions({
  partyId,
  onUnauthorized,
}: {
  partyId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();
  const [transactions, setTransactions] = useState<TransactionSummary[]>([]);
  const [role, setRole] = useState<string>(ANY);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    const fetchTransactions = async () => {
      setLoading(true);
      try {
        const params = new URLSearchParams({
          pageNumber: String(pageNumber),
          pageSize: String(PAGE_SIZE),
        });
        if (role !== ANY) params.set("role", role);

        const response = await apiFetch(
          `${API_BASE}/parties/${partyId}/transactions?${params}`,
          { credentials: "include" }
        );

        if (cancelled) return;

        if (response.ok) {
          const page = await unwrapPage<TransactionSummary>(response);
          setTransactions(page.data);
          setTotalPages(page.metadata.pageCount || 1);
          return;
        }

        setTransactions([]);
        await reportApiError(response, {
          fallback: t("parties.failedLoadPartySTransactions"),
          onUnauthorized,
          toastId: "error-party-transactions",
        });
      } catch {
        if (!cancelled) {
          setTransactions([]);
          toast.error(t("parties.failedLoadPartySTransactions"), {
            id: "error-party-transactions",
          });
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    fetchTransactions();
    return () => {
      cancelled = true;
    };
  }, [partyId, role, pageNumber, onUnauthorized]);

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-slate-500">
          {t("parties.everyTransactionThisPartyHas")}
        </p>
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
            <SelectItem value={ANY}>{t("parties.anyRole")}</SelectItem>
            {PARTY_ROLES.map((candidate) => (
              <SelectItem key={candidate} value={candidate}>
                As {candidate}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {loading ? (
        <div className="space-y-2">
          {Array.from({ length: 3 }).map((_, index) => (
            <Skeleton key={index} className="h-10 w-full" />
          ))}
        </div>
      ) : transactions.length === 0 ? (
        <p className="py-8 text-center text-sm text-slate-500">
          {role === ANY
            ? "No transaction names this party"
            : `No transaction names this party as ${role}`}
        </p>
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
          <Table>
            <TableHeader>
              <TableRow className="bg-slate-50 hover:bg-slate-50">
                <TableHead>{t("parties.reference")}</TableHead>
                <TableHead className="w-56">{t("common.created")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {transactions.map((transaction) => (
                <TableRow
                  key={transaction.transactionId}
                  className="cursor-pointer hover:bg-slate-50/50"
                  onClick={() =>
                    router.push(`/transactions/${transaction.transactionId}`)
                  }
                >
                  <TableCell>
                    <TransactionReference transaction={transaction} />
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">
                    <LocalTime value={transaction.createdAt} />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {transactions.length > 0 && totalPages > 1 && (
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
}

export default PartyDetails;
