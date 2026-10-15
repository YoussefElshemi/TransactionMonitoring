"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { ArrowRight, History, Link2, Link2Off, Plus } from "lucide-react";

import { CaseLinkResource } from "@/types";
import { API_BASE, reportApiError, unwrapPage } from "@/lib/api";
import { relationText, userName } from "@/lib/resources";
import { DispositionBadge } from "@/components/DispositionBadge";
import { SeverityBadge } from "@/components/SeverityBadge";
import { StatusBadge } from "@/components/StatusBadge";
import { LocalTime } from "@/components/LocalTime";
import { LinkCaseDialog } from "@/components/caseLinks/LinkCaseDialog";
import { HistoryDialog, historyPath } from "@/components/history";
import Pagination from "@/components/Pagination";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const PAGE_SIZE = 10;

export function CaseLinksPanel({
  caseId,
  onUnauthorized,
}: {
  caseId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [links, setLinks] = useState<CaseLinkResource[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [linking, setLinking] = useState(false);
  const [historyLink, setHistoryLink] = useState<CaseLinkResource | null>(null);

  const linkedCaseIds = useMemo(
    () => new Set(links.map((resource) => resource.caseLink.linkedCaseId)),
    [links]
  );

  const fetchLinks = useCallback(async () => {
    setLoading(true);

    const params = new URLSearchParams({
      PageNumber: String(pageNumber),
      PageSize: String(PAGE_SIZE),
    });

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/links?${params}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<CaseLinkResource>(response);
        setLinks(page.data);
        setTotalPages(page.metadata.pageCount || 1);
        setTotalCount(page.metadata.totalCount);
      } else {
        setLinks([]);
        await reportApiError(response, {
          fallback: t("caseLinks.errorOccurredFetchingRelatedCases"),
          onUnauthorized,
          toastId: "error-fetching-case-links",
        });
      }
    } catch {
      setLinks([]);
      toast.error(t("caseLinks.errorOccurredFetchingRelatedCases"), {
        id: "error-fetching-case-links",
      });
    } finally {
      setLoading(false);
    }
  }, [caseId, pageNumber, onUnauthorized]);

  useEffect(() => {
    fetchLinks();
  }, [fetchLinks]);

  const handleLink = async (linkedCaseId: string, reason: string | null) => {
    setLinking(true);
    try {
      const response = await apiFetch(`${API_BASE}/cases/${caseId}/links`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ linkedCaseId, reason }),
      });

      if (response.ok) {
        setDialogOpen(false);
        setPageNumber(1);
        fetchLinks();
        toast.success(t("caseLinks.casesLinked"));
        return;
      }

      await reportApiError(response, {
        fallback: t("caseLinks.thoseCasesCouldNotLinked"),
        onUnauthorized,
        toastId: "error-linking-case",
        fieldLabels: {
          "Body.LinkedCaseId": "Case",
          "Body.Reason": "Reason",
        },
      });
      if (response.status === 409) fetchLinks();
    } catch {
      toast.error(t("caseLinks.thoseCasesCouldNotLinked"), {
        id: "error-linking-case",
      });
    } finally {
      setLinking(false);
    }
  };

  const handleUnlink = async (caseLinkId: string, name: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/links/${caseLinkId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("caseLinks.linkCouldNotRemoved"),
          onUnauthorized,
          toastId: `unlink-${caseLinkId}`,
        });
        return;
      }

      fetchLinks();
      toast.success(t("caseLinks.unlinkedFrom", name), {
        description: t("caseLinks.twoCanLinkedAgainIf"),
      });
    } catch {
      toast.error(t("caseLinks.linkCouldNotRemoved"), {
        id: `unlink-${caseLinkId}`,
      });
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="max-w-prose text-sm text-slate-500">
          Cases connected to this one. The connection is mutual — whatever you
          link here shows this case in its own list.
        </p>
        <Button
          size="sm"
          className="gap-1.5"
          onClick={() => setDialogOpen(true)}
        >
          <Plus className="size-4" />
          {t("caseLinks.linkACase")}
        </Button>
      </div>

      {loading ? (
        <div className="space-y-2">
          {Array.from({ length: 2 }).map((_, index) => (
            <Skeleton key={index} className="h-20 w-full" />
          ))}
        </div>
      ) : links.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-card py-12 text-center">
          <Link2 className="mx-auto size-5 text-slate-300" />
          <p className="mt-2 text-sm text-slate-500">{t("caseLinks.noRelatedCases")}</p>
          <p className="mx-auto mt-1 max-w-md px-4 text-xs text-slate-400">
            Link a case when the same subject, the same week or the same pattern
            connects them. The next analyst reading either one will see it.
          </p>
        </div>
      ) : (
        <>
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {totalCount} {totalCount === 1 ? "related case" : "related cases"}
          </p>
          <ul className="space-y-2">
            {links.map((resource) => {
              const { caseLink, linkedCase } = resource;
              const name = relationText(
                linkedCase?.friendlyName,
                caseLink.linkedCaseId
              );

              return (
                <li
                  key={caseLink.caseLinkId}
                  // `relative` for the same reason the upload zone carries it:
                  // the row's buttons hold `sr-only` labels, which are
                  // absolutely positioned and would otherwise resolve against
                  // the initial containing block rather than this row.
                  className="relative rounded-md border border-slate-200 bg-card p-4"
                >
                  <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <Link
                          href={`/cases/${caseLink.linkedCaseId}`}
                          className="text-sm font-medium text-brand-800 hover:underline"
                        >
                          {name}
                        </Link>
                        {linkedCase && (
                          <>
                            <SeverityBadge severity={linkedCase.severity} />
                            <StatusBadge status={linkedCase.status} />
                            {linkedCase.disposition && (
                              <DispositionBadge
                                disposition={linkedCase.disposition}
                              />
                            )}
                          </>
                        )}
                      </div>

                      {caseLink.reason ? (
                        <p className="mt-1.5 max-w-prose break-words text-sm text-slate-700">
                          {caseLink.reason}
                        </p>
                      ) : null}

                      <p className="mt-1 text-xs text-slate-400">
                        linked by{" "}
                        {relationText(
                          userName(resource.createdByUser),
                          caseLink.createdByUserId
                        )}{" "}
                        · <LocalTime value={caseLink.createdAt} />
                      </p>
                    </div>

                    <div className="flex flex-shrink-0 items-center gap-1">
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-slate-400 hover:text-slate-700"
                        title={t("caseLinks.viewHistory")}
                        aria-label={t("caseLinks.viewHistory")}
                        onClick={() => setHistoryLink(resource)}
                      >
                        <History className="size-4" />
                        <span className="sr-only">
                          History of the link to {name}
                        </span>
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-critical-500 hover:text-critical-700"
                        title={t("caseLinks.unlink")}
                        aria-label={t("caseLinks.unlink")}
                        onClick={() =>
                          handleUnlink(caseLink.caseLinkId, name)
                        }
                      >
                        <Link2Off className="size-4" />
                        <span className="sr-only">
                          {t("caseLinks.unlinkNamed", name)}
                        </span>
                      </Button>
                      <Link
                        href={`/cases/${caseLink.linkedCaseId}`}
                        className="rounded-md p-2 text-slate-300 transition-colors hover:text-slate-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-800"
                        aria-label={`Open ${name}`}
                      >
                        <ArrowRight className="size-4" />
                      </Link>
                    </div>
                  </div>
                </li>
              );
            })}
          </ul>
        </>
      )}

      {links.length > 0 && totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((p) => Math.min(totalPages, p + 1))
          }
        />
      )}

      <LinkCaseDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        caseId={caseId}
        excludedCaseIds={linkedCaseIds}
        submitting={linking}
        onSubmit={handleLink}
        onUnauthorized={onUnauthorized}
      />

      <HistoryDialog
        open={historyLink !== null}
        onOpenChange={(open) => !open && setHistoryLink(null)}
        title={`the link to ${relationText(
          historyLink?.linkedCase?.friendlyName,
          historyLink?.caseLink.linkedCaseId
        )}`}
        entityType="CaseLink"
        path={historyPath.CaseLink(
          caseId,
          historyLink?.caseLink.caseLinkId ?? ""
        )}
        entityId={historyLink?.caseLink.caseLinkId}
        onUnauthorized={onUnauthorized}
        scopeNote="One relationship, one trail. A link created from the other case is the same record read from this end, so its history reads the same from either side."
      />
    </div>
  );
}
