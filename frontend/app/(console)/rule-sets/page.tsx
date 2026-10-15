"use client";

import { useState, useEffect } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  Paged,
  RuleSetResource,
  ProblemDetails,
  ValidationProblemDetails
} from "@/types";
import { toast } from "sonner";
import Pagination from "@/components/Pagination";
import { Plus, Trash2, Eye } from "lucide-react";
import { useAuth } from "@/context/AuthContext";
import { usePermissions } from "@/context/PermissionsContext";
import {
  ownerBody,
  SubOrganizationOwner,
  SubOrganizationOwnerField,
} from "@/components/subOrganizations/SubOrganizationOwnerField";
import { PageHeader } from "@/components/PageHeader";
import { NarrowedListingNote } from "@/components/NarrowedListingNote";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import { relationText, userName } from "@/lib/resources";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter
} from "@/components/ui/dialog";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, unwrap } from "@/lib/api";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const RuleSets = () => {
  const t = useTranslation();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { user } = useAuth();
  const { holds, holdsOver } = usePermissions();
  const canCreateRuleSets = holds("rule-sets:write");

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const [ruleSets, setRuleSets] = useState<RuleSetResource[]>([]);
  const [pageNumber, setPageNumber] = useState<number>(
    parseInt(searchParams.get("pageNumber") || "1")
  );
  const [pageSize, setPageSize] = useState<number>(
    parseInt(searchParams.get("pageSize") || "10")
  );
  const [totalPages, setTotalPages] = useState<number>(1);
  const [loading, setLoading] = useState<boolean>(true);
  const [searchTerm, setSearchTerm] = useState<string>(
    searchParams.get("friendlyName") || ""
  );

  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [newFriendlyName, setNewFriendlyName] = useState<string>("");
  const [isNewEnabled, setNewIsEnabled] = useState<boolean>(true);
  const [newReviewThreshold, setNewReviewThreshold] = useState<string>("");
  const [newOwner, setNewOwner] = useState<SubOrganizationOwner>(undefined);

  const fetchRuleSets = async (
    pageNumber: number,
    pageSize: number,
    friendlyName: string
  ) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/rule-sets?PageNumber=${pageNumber}&PageSize=${pageSize}&friendlyName=${friendlyName}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = (await response.json()) as Paged<RuleSetResource>;
        setRuleSets(data.data);
        setTotalPages(data.metadata.pageCount);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginAccessResource"), {
          id: "unauthorized"
        });
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission"),
          {
            id: "forbidden"
          }
        );
        return router.push("/");
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("common.errorOccurredFetchingRuleSets"),
          { id: "error-fetching" }
        );
      }
    } catch {
      toast.error(t("common.errorOccurredFetchingRuleSets"), {
        id: "error-fetching"
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    setIsModalOpen(true);
    setNewFriendlyName("");
    setNewIsEnabled(true);
    setNewReviewThreshold("");
    setNewOwner(undefined);
  };

  const handleCreateRuleSet = async () => {
    try {
      const response = await apiFetch(`${API_BASE}/rule-sets`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          friendlyName: newFriendlyName,
          isEnabled: isNewEnabled,
          reviewThreshold:
            newReviewThreshold.trim() === "" ? null : Number(newReviewThreshold),
          ...ownerBody(newOwner)
        }),
        credentials: "include"
      });

      if (response.ok) {
        const data = await unwrap<RuleSetResource>(response);
        toast.success(t("ruleSets.successfullyCreatedRuleSet"));

        setIsModalOpen(false);
        router.push("rule-sets/" + data.ruleSet.ruleSetId);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("ruleSets.failedCreateRuleSet"));
      }
    } catch {
      toast.error(t("ruleSets.failedCreateRuleSet"));
    }
  };

  const deleteRuleSet = async (ruleSetId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/rule-sets/${ruleSetId}`,
        {
          method: "DELETE",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("ruleSets.successfullyDeletedRuleSet"), {
          action: { label: t("common.undo"), onClick: () => recoverRuleSet(ruleSetId) },
        });

        fetchRuleSets(pageNumber, pageSize, searchTerm);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("ruleSets.failedDeleteRuleSet"));
      }
    } catch {
      toast.error(t("ruleSets.failedDeleteRuleSet"));
    }
  };

  const recoverRuleSet = async (ruleSetId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/rule-sets/${ruleSetId}/recover`,
        {
          method: "POST",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("ruleSets.successfullyRecoveredRuleSet"));
        fetchRuleSets(pageNumber, pageSize, searchTerm);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("ruleSets.failedRecoverRuleSet"));
      }
    } catch {
      toast.error(t("ruleSets.failedRecoverRuleSet"));
    }
  };

  useEffect(() => {
    fetchRuleSets(pageNumber, pageSize, searchTerm);
  }, [pageNumber, pageSize, searchTerm]);

  useEffect(() => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    if (searchTerm.length) {
      params.set("friendlyName", searchTerm);
    }
    router.replace(`?${params.toString()}`);
  }, [pageNumber, pageSize, searchTerm, router]);

  return (
    <div className="space-y-4">
      <PageHeader
        eyebrow={t("ruleSets.rules2")}
        title={t("ruleSets.ruleSets")}
        description={t("ruleSets.rulesAreGroupedIntoSets")}
      >
        {canCreateRuleSets && (
          <Button size="sm" className="gap-1.5" onClick={handleCreate}>
            <Plus className="size-4" />
            {t("ruleSets.newRuleSet")}
          </Button>
        )}
      </PageHeader>

      <NarrowedListingNote permission="rule-sets:read" noun="rule set" />

      <div className="flex items-center justify-between gap-3">
        <Input
          value={searchTerm}
          onChange={(e) => { setSearchTerm(e.target.value); setPageNumber(1); }}
          placeholder={t("common.searchByName")}
          className="w-64"
        />
        <PageSizeSelect
          value={pageSize}
          onChange={(next) => {
            setPageSize(next);
            setPageNumber(1);
          }}
        />
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("common.name")}</TableHead>
              <TableHead>{t("ruleSets.rules")}</TableHead>
              <TableHead>{t("ruleSets.createdBy")}</TableHead>
              <TableHead>{t("common.status")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 5 }).map((_, j) => (
                    <TableCell key={j}><Skeleton className="h-4 w-full" /></TableCell>
                  ))}
                </TableRow>
              ))
            ) : ruleSets.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.noResults")}
                </TableCell>
              </TableRow>
            ) : (
              ruleSets.map(({ ruleSet, createdByUser }) => (
                <TableRow key={ruleSet.ruleSetId} className="hover:bg-slate-50/50">
                  <TableCell className="font-medium text-slate-900">{ruleSet.friendlyName}</TableCell>
                  <TableCell className="text-slate-600">{ruleSet.ruleCount}</TableCell>
                  <TableCell className="text-sm text-slate-600">
                    {relationText(
                      userName(createdByUser),
                      ruleSet.createdByUserId
                    )}
                  </TableCell>
                  <TableCell>
                    <StatusBadge status={ruleSet.isEnabled ? "Enabled" : "Disabled"} />
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button variant="outline" size="sm" className="gap-1.5" onClick={() => router.push(`/rule-sets/${ruleSet.ruleSetId}`)}>
                        <Eye className="size-4" />
                        {t("common.view")}
                      </Button>
                      {holdsOver("rule-sets:write", {
                        ruleSetId: ruleSet.ruleSetId,
                      }) && (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-critical-500 hover:text-critical-700"
                          onClick={() => deleteRuleSet(ruleSet.ruleSetId)}
                        >
                          <Trash2 className="size-4" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {ruleSets.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
        />
      )}

      <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("ruleSets.createRuleSet")}</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <Input
              placeholder={t("ruleSets.friendlyName")}
              value={newFriendlyName}
              onChange={(e) => setNewFriendlyName(e.target.value)}
            />
            <label className="flex items-center gap-2 font-mono text-sm text-slate-700">
              <input
                type="checkbox"
                checked={isNewEnabled}
                onChange={(e) => setNewIsEnabled(e.target.checked)}
                className="h-4 w-4 rounded border-slate-300 accent-brand-800"
              />
              {t("common.enabled")}
            </label>
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("ruleSets.reviewThreshold")}
              </label>
              <Input
                type="number"
                min={0}
                placeholder={t("ruleSets.serverDefault100")}
                value={newReviewThreshold}
                onChange={(e) => setNewReviewThreshold(e.target.value)}
              />
              <p className="text-xs text-slate-400">
                Risk score at which a fired rule&apos;s action is escalated to
                Review. Leave blank to use the server default rather than 0.
              </p>
            </div>
            <SubOrganizationOwnerField
              organizationId={user!.organizationId}
              value={newOwner}
              onChange={setNewOwner}
              allowUnchanged={false}
              label={t("ruleSets.ownedBy")}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsModalOpen(false)}>{t("common.cancel")}</Button>
            <Button onClick={handleCreateRuleSet}>{t("ruleSets.create")}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default RuleSets;
