"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Archive, History, Lock, RotateCcw, Save } from "lucide-react";

import { CaseReasonCode, CaseReasonCodeResource, CaseReasonCodeUsage } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { dispositionLabelKey } from "@/lib/cases";
import { HistoryDialog, historyPath } from "@/components/history";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { DispositionSelect, DISPOSITION_ANY } from "./DispositionSelect";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

export function ReasonCodeDetail({
  organizationId,
  caseReasonCodeId,
  backHref,
  onUnauthorized,
}: {
  organizationId: string;
  caseReasonCodeId: string;
  backHref: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();

  const [reasonCode, setReasonCode] = useState<CaseReasonCode | null>(null);
  const [usage, setUsage] = useState<CaseReasonCodeUsage | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);

  const [description, setDescription] = useState("");
  const [disposition, setDisposition] = useState<string>(DISPOSITION_ANY);

  const fetchReasonCode = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/reason-codes/${caseReasonCodeId}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const { caseReasonCode } = await unwrap<CaseReasonCodeResource>(response);
        setReasonCode(caseReasonCode);
        setDescription(caseReasonCode.description);
        setDisposition(caseReasonCode.disposition ?? DISPOSITION_ANY);
      } else {
        setReasonCode(null);
        await reportApiError(response, {
          fallback: t("reasonCodes.errorOccurredFetchingReasonCode"),
          onUnauthorized,
        });
      }
    } catch {
      setReasonCode(null);
      toast.error(t("reasonCodes.errorOccurredFetchingReasonCode"));
    } finally {
      setLoading(false);
    }
  }, [organizationId, caseReasonCodeId, onUnauthorized]);

  const fetchUsage = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/reason-codes/${caseReasonCodeId}/usages`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) setUsage(await unwrap<CaseReasonCodeUsage>(response));
    } catch {
    }
  }, [organizationId, caseReasonCodeId]);

  useEffect(() => {
    fetchReasonCode();
    fetchUsage();
  }, [fetchReasonCode, fetchUsage]);

  const patch = async (body: Record<string, unknown>, success: string) => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/reason-codes/${caseReasonCodeId}`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(body),
        }
      );

      if (response.ok) {
        toast.success(success);
        fetchReasonCode();
      } else {
        await reportApiError(response, {
          fallback: t("reasonCodes.failedUpdateReasonCode"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("reasonCodes.failedUpdateReasonCode"));
    } finally {
      setSaving(false);
    }
  };

  const handleSave = () =>
    patch(
      {
        description: description.trim(),
        ...(disposition === DISPOSITION_ANY
          ? { clearDisposition: true }
          : { disposition }),
      },
      "Reason code saved"
    );

  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-56" />
        <Skeleton className="h-64 w-full max-w-2xl" />
      </div>
    );
  }

  if (!reasonCode) {
    return (
      <NotFound
        title={t("reasonCodes.reasonCodeNotFound")}
        description={t("common.notFound.description")}
        backHref={backHref}
        backLabel={t("reasonCodes.allReasonCodes")}
      />
    );
  }

  const dirty =
    description.trim() !== reasonCode.description ||
    disposition !== (reasonCode.disposition ?? DISPOSITION_ANY);

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("reasonCodes.reasonCode")} title={reasonCode.code}>
        <Button
          variant="outline"
          size="sm"
          className="gap-1.5"
          onClick={() => setHistoryOpen(true)}
        >
          <History className="size-4" />
          {t("common.history")}
        </Button>
        {reasonCode.isEnabled ? (
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5 text-slate-600"
            disabled={saving}
            onClick={() =>
              patch({ isEnabled: false }, `${reasonCode.code} retired`)
            }
          >
            <Archive className="size-4" />
            {t("common.retire")}
          </Button>
        ) : (
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5 text-settled-700 hover:text-settled-700"
            disabled={saving}
            onClick={() =>
              patch({ isEnabled: true }, `${reasonCode.code} restored`)
            }
          >
            <RotateCcw className="size-4" />
            {t("reasonCodes.restore")}
          </Button>
        )}
      </PageHeader>

      <Card>
        <CardContent className="space-y-5 pt-6">
          <div className="flex flex-wrap items-center gap-3">
            <StatusBadge
              status={reasonCode.isEnabled ? "Enabled" : "Disabled"}
              label={reasonCode.isEnabled ? t("reasonCodes.inUse") : "Retired"}
            />
            {usage && (
              <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                {usage.caseCount} case{usage.caseCount === 1 ? "" : "s"} filed
                under it
              </span>
            )}
          </div>

          {!reasonCode.isEnabled && (
            <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600">
              Retired. Analysts are no longer offered this code, and every case
              already filed under it still shows it exactly as before.
            </p>
          )}

          <div className="space-y-1.5">
            <label className="flex items-center gap-1.5 font-mono text-xs uppercase tracking-widest text-slate-500">
              <Lock className="size-3" />
              {t("reasonCodes.code")}
            </label>
            <p className="font-mono text-sm text-slate-900">{reasonCode.code}</p>
            <p className="text-xs text-slate-500">
              Cannot be changed. Closed cases are filed under this name and the
              tuning reports group by it, so renaming it would rewrite figures
              that have already been reported. Retire it and declare a new one
              instead.
            </p>
          </div>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("common.description")}
            </label>
            <Input
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              maxLength={500}
            />
          </div>

          <DispositionSelect value={disposition} onChange={setDisposition} />

          <div className="flex items-center gap-3 border-t border-slate-100 pt-4">
            <Button
              className="gap-1.5"
              onClick={handleSave}
              disabled={saving || !dirty || description.trim().length === 0}
            >
              <Save className="size-4" />
              {saving ? "Saving..." : "Save"}
            </Button>
            <Button
              variant="outline"
              onClick={() => router.push(backHref)}
              disabled={saving}
            >
              {t("reasonCodes.back")}
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="grid grid-cols-2 gap-x-8 gap-y-3 pt-6 text-sm">
          <div>
            <span className="font-medium text-slate-500">{t("reasonCodes.appliesTo")}</span>
            <p className="mt-0.5 text-slate-900">
              {reasonCode.disposition
                ? t(
                    "reasonCodes.dispositionOnly",
                    t(dispositionLabelKey(reasonCode.disposition))
                  )
                : t("reasonCodes.allConclusions")}
            </p>
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("common.created")}</span>
            <p className="mt-0.5 text-slate-900">
              <LocalTime value={reasonCode.createdAt} />
            </p>
          </div>
          <div>
            <span className="font-medium text-slate-500">{t("common.updated")}</span>
            <p className="mt-0.5 text-slate-900">
              <LocalTime value={reasonCode.updatedAt} />
            </p>
          </div>
        </CardContent>
      </Card>

      <HistoryDialog
        open={historyOpen}
        onOpenChange={setHistoryOpen}
        title={t("reasonCodes.thisReasonCode")}
        entityType="CaseReasonCode"
        path={historyPath.CaseReasonCode(organizationId, caseReasonCodeId)}
        entityId={caseReasonCodeId}
        onUnauthorized={onUnauthorized}
      />
    </div>
  );
}
