"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Eye, Plus } from "lucide-react";

import type { BillingEntity, CurrencyCode } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  ADDRESS_HINT,
  CREDIT_NOTE_PREFIX_HINT,
  DISABLE_RATHER_THAN_DELETE,
  INVOICE_PREFIX_HINT,
  LEGAL_NAME_HINT,
  NOT_REGISTERED_HINT,
  PREFIXES_ARE_PERMANENT,
  PREFIX_IN_USE_MESSAGE,
  PREFIX_MAX_LENGTH,
  TAX_IDENTIFIER_HINT,
  entitiesInReadingOrder,
  entityRefusal,
} from "@/lib/billingEntities";
import { CURRENCY_CODES, CURRENCY_LABELS } from "@/lib/currency";
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
import Pagination from "@/components/Pagination";
import { PageHeader } from "@/components/PageHeader";
import { PolicySwitch } from "@/components/PolicySwitch";
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

const BillingEntities = () => {
  const t = useTranslation();
  const { allowed } = useRequirePermission(
    ["billing-entities:read", "platform:administer"],
    "/"
  );

  const router = useRouter();
  const { grants } = usePermissions();
  const submission = useIdempotencyKey();

  const canWrite = canWriteBilling(grants, "billing-entities:write");

  const [entities, setEntities] = useState<BillingEntity[]>([]);
  const [includeDisabled, setIncludeDisabled] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const [legalName, setLegalName] = useState("");
  const [address, setAddress] = useState("");
  const [countryCode, setCountryCode] = useState("GB");
  const [taxIdentifier, setTaxIdentifier] = useState("");
  const [invoiceNumberPrefix, setInvoiceNumberPrefix] = useState("INV");
  const [creditNoteNumberPrefix, setCreditNoteNumberPrefix] = useState("CRN");
  const [currency, setCurrency] = useState<CurrencyCode>("GBP");

  const fetchEntities = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/billing-entities?PageNumber=${pageNumber}&PageSize=25&IncludeDisabled=${includeDisabled}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<BillingEntity>(response);
        setEntities(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("billingEntities.errorOccurredFetchingBillingEntities"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-fetching-billing-entities",
        });
      }
    } catch {
      toast.error(t("billingEntities.errorOccurredFetchingBillingEntities"), {
        id: "error-fetching-billing-entities",
      });
    } finally {
      setLoading(false);
    }
  }, [pageNumber, includeDisabled, router]);

  useEffect(() => {
    if (allowed) fetchEntities();
  }, [allowed, fetchEntities]);

  const refusal = entityRefusal({
    legalName,
    address,
    countryCode,
    taxIdentifier,
    invoiceNumberPrefix,
    creditNoteNumberPrefix,
  });

  const create = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/billing-entities`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            legalName: legalName.trim(),
            address: address.trim(),
            countryCode: countryCode.trim().toUpperCase(),
            ...(taxIdentifier.trim().length > 0
              ? { taxIdentifier: taxIdentifier.trim() }
              : {}),
            invoiceNumberPrefix: invoiceNumberPrefix.trim().toUpperCase(),
            creditNoteNumberPrefix: creditNoteNumberPrefix
              .trim()
              .toUpperCase(),
            currency,
          }),
        },
        { idempotencyKey: submission.current("create") }
      );

      if (response.ok) {
        const created = await unwrap<BillingEntity>(response);
        submission.completed("create");

        toast.success(t("common.namedCreated", created.legalName), {
          description: t(
            "billingEntities.createdDescription",
            created.invoiceNumberPrefix,
            created.creditNoteNumberPrefix,
            created.currency
          ),
        });

        setCreateOpen(false);
        router.push(`/admin/billing-entities/${created.billingEntityId}`);
      } else if (response.status === 409) {
        toast.error(t("billingEntities.prefixTaken"), {
          description: PREFIX_IN_USE_MESSAGE,
        });
      } else {
        await reportApiError(response, {
          fallback: t("billingEntities.failedCreateBillingEntity"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-creating-billing-entity",
        });
      }
    } catch {
      toast.error(t("billingEntities.failedCreateBillingEntity"), {
        id: "error-creating-billing-entity",
      });
    } finally {
      setSaving(false);
    }
  };

  if (!allowed) return null;

  const ordered = entitiesInReadingOrder(entities);

  return (
    <div className="p-6">
      <PageHeader
        eyebrow={t("billingEntities.admin")}
        title={t("billingEntities.billingEntities")}
        description={t("billingEntities.theLegalEntitiesThisInstallation")}
      >
        {canWrite && (
          <Button size="sm" onClick={() => setCreateOpen(true)}>
            <Plus />
            {t("billingEntities.newEntity")}
          </Button>
        )}
      </PageHeader>

      <div className="mb-4">
        <PolicySwitch
          label={t("billingEntities.showWithdrawnEntities")}
          checked={includeDisabled}
          onChange={(next) => {
            setIncludeDisabled(next);
            setPageNumber(1);
          }}
        >
          Off by default. {DISABLE_RATHER_THAN_DELETE}
        </PolicySwitch>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("billingEntities.entity")}</TableHead>
              <TableHead className="w-40">{t("billingEntities.registeredIn")}</TableHead>
              <TableHead className="w-44">{t("billingEntities.registration")}</TableHead>
              <TableHead className="w-36">{t("billingEntities.numbering")}</TableHead>
              <TableHead className="w-24">{t("common.currency")}</TableHead>
              <TableHead className="w-28">{t("common.state")}</TableHead>
              <TableHead className="w-20">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 3 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 7 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : ordered.length === 0 ? (
              <TableRow>
                <TableCell colSpan={7} className="py-10 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {t("billingEntities.noBillingEntities")}
                  </p>
                  <p className="mx-auto mt-2 max-w-md text-sm text-slate-500">
                    Nothing can be invoiced until one exists — a customer&apos;s
                    billing profile has to name the entity that issues to them,
                    and there is nothing here to name.
                  </p>
                </TableCell>
              </TableRow>
            ) : (
              ordered.map((entity) => (
                <TableRow
                  key={entity.billingEntityId}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell className="text-sm font-medium text-slate-900">
                    {entity.legalName}
                    <span className="block text-xs font-normal text-slate-400">
                      {entity.address.split("\n")[0]}
                    </span>
                  </TableCell>

                  <TableCell className="text-xs text-slate-600">
                    {countryName(entity.countryCode)}
                  </TableCell>

                  <TableCell className="font-mono text-xs text-slate-600">
                    {entity.taxIdentifier ?? (
                      <span className="font-sans text-slate-400">
                        {NOT_REGISTERED_HINT}
                      </span>
                    )}
                  </TableCell>

                  <TableCell className="font-mono text-xs text-slate-600">
                    {entity.invoiceNumberPrefix}-…
                    <span className="block text-slate-400">
                      {entity.creditNoteNumberPrefix}-…
                    </span>
                  </TableCell>

                  <TableCell className="font-mono text-xs text-slate-600">
                    {entity.currency}
                  </TableCell>

                  <TableCell>
                    <StatusBadge
                      status={entity.isEnabled ? "Enabled" : "Disabled"}
                      label={entity.isEnabled ? "Offered" : "Withdrawn"}
                    />
                  </TableCell>

                  <TableCell>
                    <Button
                      asChild
                      variant="ghost"
                      size="sm"
                      className="h-8 px-2 text-xs"
                    >
                      <Link
                        href={`/admin/billing-entities/${entity.billingEntityId}`}
                      >
                        <Eye className="size-3.5" />
                      </Link>
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {ordered.length > 0 && totalPages > 1 && (
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

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("billingEntities.newBillingEntity")}</DialogTitle>
            <DialogDescription>
              The legal entity documents will be issued as, and the two series
              they will be numbered in.
            </DialogDescription>
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
            </div>

            <div className="space-y-3 rounded-lg border border-attention-200 bg-attention-50 p-3">
              <p className="text-xs text-attention-700">
                {PREFIXES_ARE_PERMANENT}
              </p>

              <div className="grid gap-3 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                    {t("billingEntities.invoicePrefix")}
                  </label>
                  <Input
                    value={invoiceNumberPrefix}
                    onChange={(event) =>
                      setInvoiceNumberPrefix(event.target.value.toUpperCase())
                    }
                    maxLength={PREFIX_MAX_LENGTH}
                    className="font-mono"
                    disabled={saving}
                  />
                  <p className="text-xs text-slate-500">
                    {INVOICE_PREFIX_HINT}
                  </p>
                </div>

                <div className="space-y-1.5">
                  <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                    {t("billingEntities.creditNotePrefix")}
                  </label>
                  <Input
                    value={creditNoteNumberPrefix}
                    onChange={(event) =>
                      setCreditNoteNumberPrefix(
                        event.target.value.toUpperCase()
                      )
                    }
                    maxLength={PREFIX_MAX_LENGTH}
                    className="font-mono"
                    disabled={saving}
                  />
                  <p className="text-xs text-slate-500">
                    {CREDIT_NOTE_PREFIX_HINT}
                  </p>
                </div>
              </div>
            </div>

            {refusal !== null && legalName.trim().length > 0 && (
              <p className="text-xs text-critical-500">{refusal}</p>
            )}
          </div>

          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => setCreateOpen(false)}
              disabled={saving}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={create} disabled={saving || refusal !== null}>
              {saving ? "Creating..." : "Create"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default BillingEntities;
