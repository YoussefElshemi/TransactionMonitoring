"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Archive, Eye, Plus, RotateCcw, Trash2 } from "lucide-react";

import {
  CaseReasonCode,
  CaseReasonCodeResource,
  CaseReasonCodeUsage,
} from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { DISPOSITIONS, dispositionLabelKey } from "@/lib/cases";
import Pagination from "@/components/Pagination";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { PageSizeSelect } from "@/components/PageSizeSelect";
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
import { DispositionSelect, DISPOSITION_ANY } from "./DispositionSelect";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const ANY_DISPOSITION = "All";
const ANY_STATE = "All";

export function ReasonCodesPanel({
  organizationId,
  detailHref,
  embedded = false,
  onUnauthorized,
}: {
  organizationId: string;
  detailHref: (caseReasonCodeId: string) => string;
  embedded?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();

  const [reasonCodes, setReasonCodes] = useState<CaseReasonCode[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState("");
  const [disposition, setDisposition] = useState(ANY_DISPOSITION);
  const [state, setState] = useState(ANY_STATE);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [newCode, setNewCode] = useState("");
  const [newDescription, setNewDescription] = useState("");
  const [newDisposition, setNewDisposition] = useState<string>(DISPOSITION_ANY);

  const [deleteTarget, setDeleteTarget] = useState<{
    reasonCode: CaseReasonCode;
    usage: CaseReasonCodeUsage | null;
  } | null>(null);

  const fetchReasonCodes = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: String(pageSize),
      });
      if (searchTerm) query.set("Code", searchTerm);
      if (disposition !== ANY_DISPOSITION) query.set("Disposition", disposition);
      if (state !== ANY_STATE) query.set("IsEnabled", String(state === "Enabled"));

      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/reason-codes?${query}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<CaseReasonCodeResource>(response);
        setReasonCodes(page.data.map(({ caseReasonCode }) => caseReasonCode));
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("reasonCodes.errorOccurredFetchingReasonCodes"),
          onUnauthorized,
          toastId: "error-fetching-reason-codes",
        });
      }
    } catch {
      toast.error(t("reasonCodes.errorOccurredFetchingReasonCodes"), {
        id: "error-fetching-reason-codes",
      });
    } finally {
      setLoading(false);
    }
  }, [
    organizationId,
    pageNumber,
    pageSize,
    searchTerm,
    disposition,
    state,
    onUnauthorized,
  ]);

  useEffect(() => {
    fetchReasonCodes();
  }, [fetchReasonCodes]);

  const handleCreate = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/reason-codes`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            code: newCode.trim(),
            description: newDescription.trim(),
            disposition:
              newDisposition === DISPOSITION_ANY ? null : newDisposition,
            isEnabled: true,
          }),
        }
      );

      if (response.ok) {
        const created = await unwrap<CaseReasonCode>(response);
        toast.success(t("reasonCodes.reasonCodeCreated"));
        setCreateOpen(false);
        setNewCode("");
        setNewDescription("");
        setNewDisposition(DISPOSITION_ANY);
        router.push(detailHref(created.caseReasonCodeId));
      } else {
        await reportApiError(response, {
          fallback: t("reasonCodes.failedCreateReasonCode"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("reasonCodes.failedCreateReasonCode"));
    } finally {
      setSaving(false);
    }
  };

  const setEnabled = async (reasonCode: CaseReasonCode, isEnabled: boolean) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/reason-codes/${reasonCode.caseReasonCodeId}`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ isEnabled }),
        }
      );

      if (response.ok) {
        toast.success(
          isEnabled
            ? t("common.namedRestored", reasonCode.code)
            : t("common.namedRetired", reasonCode.code),
          {
            description: isEnabled
              ? t("reasonCodes.offeredClosesAgain")
              : t("reasonCodes.noLongerAppearsClosesCases"),
          }
        );
        fetchReasonCodes();
      } else {
        await reportApiError(response, {
          fallback: t("reasonCodes.failedUpdateReasonCode2"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("reasonCodes.failedUpdateReasonCode2"));
    }
  };

  const askToDelete = async (reasonCode: CaseReasonCode) => {
    setDeleteTarget({ reasonCode, usage: null });

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/reason-codes/${reasonCode.caseReasonCodeId}/usages`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        setDeleteTarget({
          reasonCode,
          usage: await unwrap<CaseReasonCodeUsage>(response),
        });
      }
    } catch {
    }
  };

  const handleDelete = async (reasonCode: CaseReasonCode) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/reason-codes/${reasonCode.caseReasonCodeId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("common.namedDeleted", reasonCode.code), {
          action: {
            label: t("common.undo"),
            onClick: () => handleRecover(reasonCode),
          },
        });
        setDeleteTarget(null);
        fetchReasonCodes();
      } else {
        await reportApiError(response, {
          fallback: t("reasonCodes.failedDeleteReasonCode"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("reasonCodes.failedDeleteReasonCode"));
    }
  };

  const handleRecover = async (reasonCode: CaseReasonCode) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/reason-codes/${reasonCode.caseReasonCodeId}/recover`,
        { method: "POST", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("common.namedRestored", reasonCode.code));
        fetchReasonCodes();
      } else {
        await reportApiError(response, {
          fallback: t("reasonCodes.failedRecoverReasonCode"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("reasonCodes.failedRecoverReasonCode"));
    }
  };

  const createBlocked =
    saving || newCode.trim().length === 0 || newDescription.trim().length === 0;

  const createButton = (
    <Button size="sm" className="gap-1.5" onClick={() => setCreateOpen(true)}>
      <Plus className="size-4" />
      {t("reasonCodes.newReasonCode")}
    </Button>
  );

  return (
    <div className="space-y-4">
      {!embedded && (
        <PageHeader
          eyebrow={t("reasonCodes.organization")}
          title={t("reasonCodes.reasonCodes")}
          description={t("reasonCodes.whyCasesCloseTheWay")}
        >
          {createButton}
        </PageHeader>
      )}

      {embedded && (
        <p className="text-sm text-slate-500">
          Why cases close the way they do. Declaring none is fine — analysts
          then close cases without one. Declare any, and a close must name one.
        </p>
      )}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Input
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value);
              setPageNumber(1);
            }}
            placeholder={t("reasonCodes.searchByCode")}
            className="w-56"
          />
          <Select
            value={disposition}
            onValueChange={(v) => {
              setDisposition(v);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-48">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_DISPOSITION}>{t("reasonCodes.anyDisposition")}</SelectItem>
              {DISPOSITIONS.map((d) => (
                <SelectItem key={d} value={d}>
                  {t(dispositionLabelKey(d))}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select
            value={state}
            onValueChange={(v) => {
              setState(v);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_STATE}>{t("reasonCodes.allCodes")}</SelectItem>
              <SelectItem value="Enabled">{t("reasonCodes.inUse")}</SelectItem>
              <SelectItem value="Disabled">{t("reasonCodes.retired")}</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="flex items-center gap-3">
          <PageSizeSelect
            value={pageSize}
            onChange={(next) => {
              setPageSize(next);
              setPageNumber(1);
            }}
          />
          {embedded && createButton}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("reasonCodes.code")}</TableHead>
              <TableHead>{t("common.description")}</TableHead>
              <TableHead>{t("reasonCodes.appliesTo")}</TableHead>
              <TableHead>{t("common.status")}</TableHead>
              <TableHead className="w-40">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 5 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : reasonCodes.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={5}
                  className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {t("common.noResults")}
                </TableCell>
              </TableRow>
            ) : (
              reasonCodes.map((reasonCode) => (
                <TableRow
                  key={reasonCode.caseReasonCodeId}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell className="font-mono text-sm font-medium text-slate-900">
                    {reasonCode.code}
                  </TableCell>
                  <TableCell className="max-w-md text-sm text-slate-600">
                    {reasonCode.description}
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">
                    {reasonCode.disposition
                      ? t(dispositionLabelKey(reasonCode.disposition))
                      :
                        "All"}
                  </TableCell>
                  <TableCell>
                    <StatusBadge
                      status={reasonCode.isEnabled ? "Enabled" : "Disabled"}
                      label={reasonCode.isEnabled ? t("reasonCodes.inUse") : "Retired"}
                    />
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="outline"
                        size="sm"
                        className="gap-1.5"
                        onClick={() =>
                          router.push(detailHref(reasonCode.caseReasonCodeId))
                        }
                      >
                        <Eye className="size-4" />
                        {t("common.view")}
                      </Button>
                      {reasonCode.isEnabled ? (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-slate-500 hover:text-slate-900"
                          title={t("common.retire")}
                          aria-label={t("common.retire")}
                          onClick={() => setEnabled(reasonCode, false)}
                        >
                          <Archive className="size-4" />
                        </Button>
                      ) : (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-settled-700 hover:text-settled-700"
                          title={t("reasonCodes.restore")}
                          aria-label={t("reasonCodes.restore")}
                          onClick={() => setEnabled(reasonCode, true)}
                        >
                          <RotateCcw className="size-4" />
                        </Button>
                      )}
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-critical-500 hover:text-critical-700"
                        title={t("common.delete")}
                        aria-label={t("common.delete")}
                        onClick={() => askToDelete(reasonCode)}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {reasonCodes.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
        />
      )}

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("reasonCodes.newReasonCode2")}</DialogTitle>
            <DialogDescription>
              Analysts will pick this on a close, and the tuning reports will
              group by it.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("reasonCodes.code")}
              </label>
              <Input
                value={newCode}
                onChange={(e) => setNewCode(e.target.value)}
                placeholder={t("reasonCodes.mule")}
                maxLength={64}
                className="font-mono"
              />
              <p className="text-xs text-slate-500">
                Cannot be changed later — closed cases are filed under it.
                Unique per organisation, compared without case:{" "}
                <span className="font-mono">{t("reasonCodes.mule")}</span> and{" "}
                <span className="font-mono">mule</span> collide.
              </p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.description")}
              </label>
              <Input
                value={newDescription}
                onChange={(e) => setNewDescription(e.target.value)}
                placeholder={t("reasonCodes.accountShowedMuleCharacteristics")}
                maxLength={500}
              />
            </div>

            <DispositionSelect
              value={newDisposition}
              onChange={setNewDisposition}
            />
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setCreateOpen(false)}
              disabled={saving}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={handleCreate} disabled={createBlocked}>
              {saving ? "Creating..." : "Create"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <DeleteDialog
        target={deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        onDelete={handleDelete}
        onRetire={(reasonCode) => {
          setDeleteTarget(null);
          setEnabled(reasonCode, false);
        }}
      />
    </div>
  );
}

function DeleteDialog({
  target,
  onOpenChange,
  onDelete,
  onRetire,
}: {
  target: { reasonCode: CaseReasonCode; usage: CaseReasonCodeUsage | null } | null;
  onOpenChange: (open: boolean) => void;
  onDelete: (reasonCode: CaseReasonCode) => void;
  onRetire: (reasonCode: CaseReasonCode) => void;
}) {
  const t = useTranslation();
  const inUse = target?.usage === null || (target?.usage?.caseCount ?? 0) > 0;

  return (
    <Dialog open={target !== null} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {inUse ? "This code is in use" : "Delete this reason code?"}
          </DialogTitle>
          <DialogDescription>
            {target?.usage
              ? inUse
                ? `${target.usage.caseCount} case${
                    target.usage.caseCount === 1 ? " is" : "s are"
                  } filed under ${target.usage.code}. Deleting it would leave them pointing at nothing and every figure grouped by it short, so the API refuses.`
                : `Nothing is filed under ${target.usage.code}, so it can be removed outright.`
              : "Whether any case is filed under this code could not be read, so deleting may be refused. Retiring always works."}
          </DialogDescription>
        </DialogHeader>

        <p className="text-sm text-slate-600">
          Retiring takes it out of the close dropdowns and leaves every case that
          already carries it exactly as it reads today.
        </p>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            {t("common.cancel")}
          </Button>
          {target && target.reasonCode.isEnabled && (
            <Button
              variant="secondary"
              onClick={() => onRetire(target.reasonCode)}
            >
              {t("reasonCodes.retireInstead")}
            </Button>
          )}
          {target && !inUse && (
            <Button
              variant="destructive"
              onClick={() => onDelete(target.reasonCode)}
            >
              {t("common.delete")}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
