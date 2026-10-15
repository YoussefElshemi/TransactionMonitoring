"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { toast } from "sonner";
import { ArrowLeft, TriangleAlert } from "lucide-react";

import type { BillingEntity, CurrencyCode } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import {
  ADDRESS_HINT,
  CLEARING_THE_REGISTRATION,
  COUNTRY_DECIDES_THE_TAX,
  DELETE_IS_NARROW,
  DISABLE_RATHER_THAN_DELETE,
  EDITABLE_FIELDS,
  ENTITY_IN_USE_MESSAGE,
  LEGAL_NAME_HINT,
  NOT_REGISTERED_HINT,
  NUMBERING_POSITION_UNAVAILABLE,
  TAX_IDENTIFIER_HINT,
  entityRefusal,
  numberFormat,
} from "@/lib/billingEntities";
import { CURRENCY_CODES, CURRENCY_LABELS } from "@/lib/currency";
import { NUMBERING_IS_GAP_FREE } from "@/lib/invoiceDocument";
import {
  JURISDICTION_LIST_HINT,
  TAX_JURISDICTIONS,
  countryName,
} from "@/lib/tax";
import { apiFetch } from "@/lib/http";
import { canWriteBilling } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { LocalTime } from "@/components/LocalTime";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

const BillingEntityDetail = () => {
  const t = useTranslation();
  const { allowed } = useRequirePermission(
    ["billing-entities:read", "platform:administer"],
    "/admin/billing-entities"
  );

  const router = useRouter();
  const params = useParams<{ billingEntityId: string }>();
  const billingEntityId = params.billingEntityId;

  const { grants } = usePermissions();
  const submission = useIdempotencyKey();

  const canWrite = canWriteBilling(grants, "billing-entities:write");

  const [entity, setEntity] = useState<BillingEntity | null>(null);
  const [loading, setLoading] = useState(true);

  const [editOpen, setEditOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [toggling, setToggling] = useState(false);

  const [legalName, setLegalName] = useState("");
  const [address, setAddress] = useState("");
  const [countryCode, setCountryCode] = useState("GB");
  const [taxIdentifier, setTaxIdentifier] = useState("");
  const [currency, setCurrency] = useState<CurrencyCode>("GBP");

  const fetchEntity = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/billing-entities/${billingEntityId}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        setEntity(await unwrap<BillingEntity>(response));
      } else {
        await reportApiError(response, {
          fallback: t("billingEntities.billingEntityCouldNotRead"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-fetching-billing-entity",
        });
      }
    } catch {
      toast.error(t("billingEntities.billingEntityCouldNotRead"), {
        id: "error-fetching-billing-entity",
      });
    } finally {
      setLoading(false);
    }
  }, [billingEntityId, router]);

  useEffect(() => {
    if (allowed) fetchEntity();
  }, [allowed, fetchEntity]);

  const patch = async (
    body: Record<string, unknown>,
    intent: string
  ): Promise<BillingEntity | null> => {
    const response = await apiFetch(
      `${API_BASE}/billing-entities/${billingEntityId}`,
      {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(body),
      },
      { idempotencyKey: submission.current(intent) }
    );

    if (response.ok) {
      submission.completed(intent);
      return await unwrap<BillingEntity>(response);
    }

    await reportApiError(response, {
      fallback: t("billingEntities.billingEntityCouldNotChanged"),
      onUnauthorized: () => router.push("/auth/login"),
      toastId: `patch-billing-entity-${intent}`,
    });

    return null;
  };

  const save = async () => {
    setSaving(true);

    try {
      const updated = await patch(
        {
          legalName: legalName.trim(),
          address: address.trim(),
          countryCode: countryCode.trim().toUpperCase(),
          taxIdentifier:
            taxIdentifier.trim().length > 0 ? taxIdentifier.trim() : null,
          currency,
        },
        "save"
      );

      if (updated !== null) {
        setEntity(updated);
        setEditOpen(false);
        toast.success(t("billingEntities.billingEntityUpdated"), {
          description:
            t("billingEntities.documentsIssuedFromNowSay"),
        });
      }
    } finally {
      setSaving(false);
    }
  };

  const setEnabled = async (isEnabled: boolean) => {
    setToggling(true);

    try {
      const updated = await patch({ isEnabled }, "enabled");

      if (updated !== null) {
        setEntity(updated);
        toast.success(isEnabled ? t("billingEntities.entityOffered") : t("billingEntities.entityWithdrawn"), {
          description: isEnabled
            ? t("billingEntities.newCustomersCanPointedAgain")
            : DISABLE_RATHER_THAN_DELETE,
        });
      }
    } finally {
      setToggling(false);
    }
  };

  const remove = async () => {
    setDeleting(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/billing-entities/${billingEntityId}`,
        {
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: submission.current("delete") }
      );

      if (response.ok) {
        submission.completed("delete");
        toast.success(t("billingEntities.billingEntityDeleted"));
        router.push("/admin/billing-entities");
      } else if (response.status === 409) {
        toast.error(t("billingEntities.entityUse"), {
          description: ENTITY_IN_USE_MESSAGE,
        });
        setDeleteOpen(false);
      } else {
        await reportApiError(response, {
          fallback: t("billingEntities.billingEntityCouldNotDeleted"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-deleting-billing-entity",
        });
      }
    } catch {
      toast.error(t("billingEntities.billingEntityCouldNotDeleted"), {
        id: "error-deleting-billing-entity",
      });
    } finally {
      setDeleting(false);
    }
  };

  if (!allowed) return null;

  if (loading) {
    return (
      <div className="space-y-3 p-6">
        <Skeleton className="h-8 w-72" />
        <Skeleton className="h-32 w-full" />
      </div>
    );
  }

  if (entity === null) {
    return (
      <div className="p-6">
        <p className="text-sm text-slate-500">
          {t("billingEntities.thatBillingEntityCouldNot")}
        </p>
      </div>
    );
  }

  const refusal = entityRefusal({
    legalName,
    address,
    countryCode,
    taxIdentifier,
    invoiceNumberPrefix: entity.invoiceNumberPrefix,
    creditNoteNumberPrefix: entity.creditNoteNumberPrefix,
  });

  return (
    <div className="p-6">
      <Button asChild variant="ghost" size="sm" className="mb-3 -ml-2 text-xs">
        <Link href="/admin/billing-entities">
          <ArrowLeft className="size-3.5" />
          {t("billingEntities.billingEntities")}
        </Link>
      </Button>

      <PageHeader
        eyebrow={t("billingEntities.billingEntity")}
        title={entity.legalName}
        description={`Registered in ${countryName(entity.countryCode)}, issuing in ${entity.currency}.`}
      >
        <StatusBadge
          status={entity.isEnabled ? "Enabled" : "Disabled"}
          label={entity.isEnabled ? "Offered" : "Withdrawn"}
        />
        {canWrite && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => {
              setLegalName(entity.legalName);
              setAddress(entity.address);
              setCountryCode(entity.countryCode);
              setTaxIdentifier(entity.taxIdentifier ?? "");
              setCurrency(entity.currency);
              setEditOpen(true);
            }}
          >
            {t("common.edit")}
          </Button>
        )}
      </PageHeader>

      <div className="grid gap-3 lg:grid-cols-2">
        <div className="rounded-xl border border-slate-200 bg-card p-4">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("billingEntities.onTheDocument")}
          </p>
          <p className="mt-2 text-sm font-medium text-slate-900">
            {entity.legalName}
          </p>
          <p className="mt-1 whitespace-pre-line text-xs text-slate-600">
            {entity.address}
          </p>
          <p className="mt-1 text-xs text-slate-500">
            {countryName(entity.countryCode)}
          </p>
          <p className="mt-2 font-mono text-xs text-slate-600">
            {entity.taxIdentifier ?? (
              <span className="font-sans text-slate-400">
                {NOT_REGISTERED_HINT}
              </span>
            )}
          </p>
        </div>

        <div className="rounded-xl border border-slate-200 bg-card p-4">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("billingEntities.numbering")}
          </p>

          <dl className="mt-2 space-y-1.5 text-xs">
            <div className="flex items-baseline justify-between gap-4">
              <dt className="text-slate-500">{t("billingEntities.invoices")}</dt>
              <dd className="font-mono text-slate-800">
                {numberFormat(entity.invoiceNumberPrefix)}
              </dd>
            </div>
            <div className="flex items-baseline justify-between gap-4">
              <dt className="text-slate-500">{t("billingEntities.creditNotes")}</dt>
              <dd className="font-mono text-slate-800">
                {numberFormat(entity.creditNoteNumberPrefix)}
              </dd>
            </div>
          </dl>

          <p className="mt-3 text-xs text-slate-500">
            {NUMBERING_POSITION_UNAVAILABLE}
          </p>

          <p className="mt-3 border-t border-slate-100 pt-3 text-xs text-slate-400">
            {NUMBERING_IS_GAP_FREE}
          </p>
        </div>
      </div>

      <div className="mt-3 rounded-xl border border-slate-200 bg-card p-4">
        <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
          {t("billingEntities.record")}
        </p>
        <p className="mt-2 text-xs text-slate-500">
          {t("common.created")} <LocalTime value={entity.createdAt} dateOnly />, last changed{" "}
          <LocalTime value={entity.updatedAt} dateOnly />.
        </p>
        <p className="mt-1 text-xs text-slate-400">{EDITABLE_FIELDS}</p>
      </div>

      {canWrite && (
        <div className="mt-6 space-y-3 border-t border-slate-200 pt-6">
          <div className="flex flex-wrap items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={toggling}
              onClick={() => setEnabled(!entity.isEnabled)}
            >
              {entity.isEnabled ? "Withdraw" : "Offer again"}
            </Button>

            <Button
              variant="ghost"
              size="sm"
              className="text-critical-700 hover:text-critical-700"
              onClick={() => setDeleteOpen(true)}
            >
              {t("common.delete")}
            </Button>
          </div>

          <p className="max-w-2xl text-xs text-slate-500">
            {DISABLE_RATHER_THAN_DELETE}
          </p>
          <p className="max-w-2xl text-xs text-slate-400">{DELETE_IS_NARROW}</p>
        </div>
      )}

      <Dialog open={editOpen} onOpenChange={setEditOpen}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              {t("billingEntities.editTitle", entity.legalName)}
            </DialogTitle>
            <DialogDescription>{EDITABLE_FIELDS}</DialogDescription>
          </DialogHeader>

          <div className="max-h-[60vh] space-y-4 overflow-y-auto pr-1">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billingEntities.legalName")}
              </label>
              <Input
                value={legalName}
                onChange={(event) => setLegalName(event.target.value)}
                disabled={saving}
              />
              <p className="text-xs text-slate-500">{LEGAL_NAME_HINT}</p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billingEntities.address")}
              </label>
              <Textarea
                value={address}
                onChange={(event) => setAddress(event.target.value)}
                rows={4}
                disabled={saving}
              />
              <p className="text-xs text-slate-500">{ADDRESS_HINT}</p>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("billingEntities.country")}
                </label>
                <Select
                  value={countryCode}
                  onValueChange={setCountryCode}
                  disabled={saving}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {TAX_JURISDICTIONS.map((entry) => (
                      <SelectItem
                        key={entry.countryCode}
                        value={entry.countryCode}
                      >
                        {entry.name} ({entry.countryCode}) ·{" "}
                        {entry.standardRate}%
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-xs text-slate-500">
                  {COUNTRY_DECIDES_THE_TAX}
                </p>
                <p className="text-xs text-slate-400">
                  {JURISDICTION_LIST_HINT}
                </p>
              </div>

              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("common.currency")}
                </label>
                <Select
                  value={currency}
                  onValueChange={(value) => setCurrency(value as CurrencyCode)}
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

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billingEntities.taxRegistration")}
              </label>
              <Input
                value={taxIdentifier}
                onChange={(event) => setTaxIdentifier(event.target.value)}
                className="font-mono"
                placeholder={t("billingEntities.none")}
                disabled={saving}
              />
              <p className="text-xs text-slate-500">{TAX_IDENTIFIER_HINT}</p>
              {entity.taxIdentifier !== null &&
                taxIdentifier.trim().length === 0 && (
                  <p className="flex items-start gap-2 rounded-lg border border-attention-200 bg-attention-50 p-2 text-xs text-attention-700">
                    <TriangleAlert className="mt-0.5 size-3.5 flex-shrink-0" />
                    <span>{CLEARING_THE_REGISTRATION}</span>
                  </p>
                )}
            </div>

            {refusal !== null && (
              <p className="text-xs text-critical-500">{refusal}</p>
            )}
          </div>

          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => setEditOpen(false)}
              disabled={saving}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={save} disabled={saving || refusal !== null}>
              {saving ? "Saving..." : "Save"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>
              {t("billingEntities.deleteTitle", entity.legalName)}
            </DialogTitle>
            <DialogDescription>{DELETE_IS_NARROW}</DialogDescription>
          </DialogHeader>

          <p className="text-sm text-slate-600">{DISABLE_RATHER_THAN_DELETE}</p>

          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => setDeleteOpen(false)}
              disabled={deleting}
            >
              {t("common.cancel")}
            </Button>
            <Button
              variant="outline"
              disabled={deleting || toggling || !entity.isEnabled}
              onClick={() => {
                setDeleteOpen(false);
                setEnabled(false);
              }}
            >
              {t("billingEntities.withdrawInstead")}
            </Button>
            <Button
              variant="destructive"
              onClick={remove}
              disabled={deleting}
            >
              {deleting ? "Deleting..." : "Delete"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default BillingEntityDetail;
