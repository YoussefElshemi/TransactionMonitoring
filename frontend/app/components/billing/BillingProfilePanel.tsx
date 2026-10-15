"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { BadgeCheck, ShieldAlert, TriangleAlert, Wallet } from "lucide-react";

import type { BillingEntity, BillingProfile, CurrencyCode } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  CONTACT_HINT,
  COUNTRY_HINT,
  CURRENCY_HINT,
  LEGAL_NAME_HINT,
  NO_PROFILE_DETAIL,
  NO_PROFILE_HEADLINE,
  PAYMENT_TERMS_HINT,
  REGISTRATION_STATE_HINTS,
  REGISTRATION_STATE_LABELS,
  SAVING_IS_AN_UPSERT,
  canVerify,
  expectedPrefixHint,
  isMissingProfile,
  profileRefusal,
  registrationState,
  wouldClearVerification,
} from "@/lib/billingProfiles";
import { selectableEntities } from "@/lib/billingEntities";
import { CURRENCY_CODES, CURRENCY_LABELS } from "@/lib/currency";
import {
  CHANGE_CLEARS_VERIFICATION,
  INVALID_TAX_IDENTIFIER_MESSAGE,
  JURISDICTION_LIST_HINT,
  TAX_JURISDICTIONS,
  TAX_TREATMENT_HINTS,
  TAX_TREATMENT_LABELS,
  VERIFICATION_IS_STRUCTURAL,
  VERIFICATION_MOVES_THE_TAX,
  predictRate,
  predictTreatment,
} from "@/lib/tax";
import { apiFetch } from "@/lib/http";
import { canWriteBilling } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { paymentTermsSentence } from "@/lib/invoiceDocument";
import { formatPercentage } from "@/lib/pricing";
import { cn } from "@/lib/utils";
import { LocalTime } from "@/components/LocalTime";
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

export function BillingProfilePanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { grants, holds, resolved } = usePermissions();
  const submission = useIdempotencyKey();

  const mayRead = holds("billing-profiles:read");
  const canWrite = canWriteBilling(grants, "billing-profiles:write");
  const mayReadEntities = holds("billing-entities:read");

  const [profile, setProfile] = useState<BillingProfile | null>(null);
  const [missing, setMissing] = useState(false);
  const [loading, setLoading] = useState(true);

  const [entities, setEntities] = useState<BillingEntity[]>([]);

  const [editOpen, setEditOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [verifying, setVerifying] = useState(false);

  const [billingEntityId, setBillingEntityId] = useState("");
  const [legalName, setLegalName] = useState("");
  const [contactName, setContactName] = useState("");
  const [contactEmail, setContactEmail] = useState("");
  const [address, setAddress] = useState("");
  const [countryCode, setCountryCode] = useState("GB");
  const [taxIdentifier, setTaxIdentifier] = useState("");
  const [currency, setCurrency] = useState<CurrencyCode>("GBP");
  const [paymentTerms, setPaymentTerms] = useState("30");

  const fetchProfile = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/billing-profile`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        setProfile(await unwrap<BillingProfile>(response));
        setMissing(false);
      } else if (isMissingProfile(response)) {
        setProfile(null);
        setMissing(true);
      } else {
        await reportApiError(response, {
          fallback: t("billing.billingProfileCouldNotRead"),
          onUnauthorized,
          toastId: "error-fetching-billing-profile",
        });
      }
    } catch {
      toast.error(t("billing.billingProfileCouldNotRead"), {
        id: "error-fetching-billing-profile",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, onUnauthorized]);

  const fetchEntities = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/billing-entities?PageNumber=1&PageSize=100`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<BillingEntity>(response);
        setEntities(page.data);
      }
    } catch {
    }
  }, []);

  useEffect(() => {
    if (mayRead) fetchProfile();
    else setLoading(false);
  }, [mayRead, fetchProfile]);

  useEffect(() => {
    if (mayReadEntities) fetchEntities();
  }, [mayReadEntities, fetchEntities]);

  const openForm = () => {
    setBillingEntityId(profile?.billingEntityId ?? "");
    setLegalName(profile?.legalName ?? "");
    setContactName(profile?.contactName ?? "");
    setContactEmail(profile?.contactEmail ?? "");
    setAddress(profile?.address ?? "");
    setCountryCode(profile?.countryCode ?? "GB");
    setTaxIdentifier(profile?.taxIdentifier ?? "");
    setCurrency(profile?.currency ?? "GBP");
    setPaymentTerms(String(profile?.paymentTerms ?? 30));
    setEditOpen(true);
  };

  const refusal = profileRefusal({
    billingEntityId,
    legalName,
    contactName,
    contactEmail,
    address,
    countryCode,
    taxIdentifier,
    paymentTerms,
  });

  const clearsVerification = wouldClearVerification(profile, {
    taxIdentifier,
    countryCode,
  });

  const save = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/billing-profile`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            billingEntityId,
            legalName: legalName.trim(),
            contactName: contactName.trim(),
            contactEmail: contactEmail.trim(),
            address: address.trim(),
            countryCode: countryCode.trim().toUpperCase(),
            ...(taxIdentifier.trim().length > 0
              ? { taxIdentifier: taxIdentifier.trim() }
              : {}),
            currency,
            paymentTerms: Number(paymentTerms.trim()),
          }),
        },
        { idempotencyKey: submission.current("save") }
      );

      if (response.ok) {
        setProfile(await unwrap<BillingProfile>(response));
        setMissing(false);
        submission.completed("save");

        toast.success(t("billing.billingDetailsSaved"), {
          description: clearsVerification
            ? CHANGE_CLEARS_VERIFICATION
            : t("billing.organisationCanInvoicedNothingAlready"),
        });

        setEditOpen(false);
      } else {
        await reportApiError(response, {
          fallback: t("billing.billingDetailsCouldNotSaved"),
          onUnauthorized,
          toastId: "error-saving-billing-profile",
        });
      }
    } catch {
      toast.error(t("billing.billingDetailsCouldNotSaved"), {
        id: "error-saving-billing-profile",
      });
    } finally {
      setSaving(false);
    }
  };

  const verify = async () => {
    setVerifying(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/billing-profile/verify-tax-identifier`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: submission.current("verify") }
      );

      if (response.ok) {
        setProfile(await unwrap<BillingProfile>(response));
        submission.completed("verify");

        toast.success(t("billing.registrationVerified"), {
          description:
            t("billing.invoicesFromNowAreDecided"),
        });
      } else if (response.status === 422) {
        toast.error(t("billing.notRegistrationNumber"), {
          description: INVALID_TAX_IDENTIFIER_MESSAGE,
        });
      } else {
        await reportApiError(response, {
          fallback: t("billing.registrationCouldNotVerified"),
          onUnauthorized,
          toastId: "error-verifying-tax-identifier",
        });
      }
    } catch {
      toast.error(t("billing.registrationCouldNotVerified"), {
        id: "error-verifying-tax-identifier",
      });
    } finally {
      setVerifying(false);
    }
  };

  if (!resolved) return null;

  if (!mayRead) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-6">
        <p className="text-sm text-slate-500">
          Needs{" "}
          <span className="font-mono text-xs">billing-profiles:read</span> to
          see who this organisation is invoiced as.
        </p>
      </div>
    );
  }

  const state = profile === null ? "none" : registrationState(profile);
  const issuer = entities.find(
    (entity) => entity.billingEntityId === (profile?.billingEntityId ?? "")
  );

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <Wallet className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("billing.billingDetails")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              Who the invoice is addressed to, which of the platform&apos;s
              entities issues it, in what currency and on what terms. A
              precondition for invoicing this organisation at all.
            </p>
          </div>
        </div>

        {canWrite && !loading && (
          <Button
            variant="outline"
            size="sm"
            className="h-8 text-xs"
            onClick={openForm}
          >
            {missing ? "Record billing details" : "Edit"}
          </Button>
        )}
      </div>

      {loading ? (
        <div className="space-y-2 rounded-xl border border-slate-200 bg-card p-4">
          <Skeleton className="h-4 w-64" />
          <Skeleton className="h-4 w-48" />
          <Skeleton className="h-4 w-56" />
        </div>
      ) : profile === null ? (
        <div className="rounded-xl border border-slate-200 bg-card p-6">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {NO_PROFILE_HEADLINE}
          </p>
          <p className="mt-2 max-w-2xl text-sm text-slate-500">
            {NO_PROFILE_DETAIL}
          </p>
          {!canWrite && (
            <p className="mt-3 text-xs text-slate-400">
              Recording them needs{" "}
              <span className="font-mono">billing-profiles:write</span> and{" "}
              <span className="font-mono">platform:administer</span>.
            </p>
          )}
        </div>
      ) : (
        <div className="space-y-3">
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="rounded-xl border border-slate-200 bg-card p-4">
              <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.invoicedAs")}
              </p>
              <p className="mt-1 text-sm font-medium text-slate-900">
                {profile.legalName}
              </p>
              <p className="mt-1 whitespace-pre-line text-xs text-slate-600">
                {profile.address}
              </p>
              <p className="mt-1 text-xs text-slate-500">
                {profile.countryCode}
              </p>
              <p className="mt-2 text-xs text-slate-500">
                {profile.contactName} · {profile.contactEmail}
              </p>
            </div>

            <div className="rounded-xl border border-slate-200 bg-card p-4">
              <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.issuedBy")}
              </p>
              <p className="mt-1 text-sm font-medium text-slate-900">
                {profile.billingEntityLegalName ??
                  "An entity that could not be resolved"}
              </p>
              <p className="mt-2 text-xs text-slate-500">
                Billed in{" "}
                <span className="font-mono text-slate-700">
                  {profile.currency}
                </span>{" "}
                · {paymentTermsSentence(profile.paymentTerms).toLowerCase()}
              </p>
              {issuer !== undefined && (
                <p className="mt-1 text-xs text-slate-400">
                  Invoices numbered {issuer.invoiceNumberPrefix}-…, from{" "}
                  {issuer.countryCode}
                </p>
              )}
            </div>
          </div>

          <div
            className={cn(
              "rounded-xl border p-4",
              state === "unverified"
                ? "border-attention-200 bg-attention-50 ring-1 ring-inset ring-attention-200"
                : "border-slate-200 bg-card"
            )}
          >
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div className="flex items-start gap-2">
                {state === "verified" ? (
                  <BadgeCheck className="mt-0.5 size-4 flex-shrink-0 text-settled-700" />
                ) : state === "unverified" ? (
                  <ShieldAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
                ) : (
                  <ShieldAlert className="mt-0.5 size-4 flex-shrink-0 text-slate-400" />
                )}
                <div>
                  <p
                    className={cn(
                      "font-mono text-xs uppercase tracking-widest",
                      state === "unverified"
                        ? "text-attention-700"
                        : "text-slate-500"
                    )}
                  >
                    Tax registration · {REGISTRATION_STATE_LABELS[state]}
                  </p>
                  <p className="mt-1 font-mono text-sm text-slate-900">
                    {profile.taxIdentifier ?? "None on file"}
                  </p>
                  {profile.taxIdentifierVerifiedAt !== null && (
                    <p className="mt-0.5 text-xs text-slate-500">
                      Verified{" "}
                      <LocalTime
                        value={profile.taxIdentifierVerifiedAt}
                        dateOnly
                      />
                    </p>
                  )}
                </div>
              </div>

              {canWrite && canVerify(profile) && (
                <Button
                  variant="outline"
                  size="sm"
                  className="h-8 text-xs"
                  disabled={verifying}
                  onClick={verify}
                >
                  {verifying ? "Verifying..." : "Verify"}
                </Button>
              )}
            </div>

            <p
              className={cn(
                "mt-2 max-w-2xl text-xs",
                state === "unverified" ? "text-attention-700" : "text-slate-500"
              )}
            >
              {REGISTRATION_STATE_HINTS[state]}
            </p>

            {state === "unverified" && (
              <p className="mt-2 max-w-2xl text-xs text-attention-700">
                {VERIFICATION_MOVES_THE_TAX}
              </p>
            )}

            {canVerify(profile) && (
              <p className="mt-2 max-w-2xl text-xs text-slate-500">
                {VERIFICATION_IS_STRUCTURAL}
              </p>
            )}
          </div>

          {issuer !== undefined && (
            <div className="rounded-xl border border-slate-200 bg-card p-4">
              <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.nextInvoiceWillBe")}
              </p>
              <p className="mt-1 text-sm text-slate-900">
                {
                  TAX_TREATMENT_LABELS[
                    predictTreatment(
                      issuer.countryCode,
                      profile.countryCode,
                      state === "verified"
                    )
                  ]
                }
                <span className="ml-2 font-mono text-xs text-slate-500">
                  {formatPercentage(
                    predictRate(
                      issuer.countryCode,
                      profile.countryCode,
                      state === "verified"
                    )
                  )}
                </span>
              </p>
              <p className="mt-1 max-w-2xl text-xs text-slate-500">
                {
                  TAX_TREATMENT_HINTS[
                    predictTreatment(
                      issuer.countryCode,
                      profile.countryCode,
                      state === "verified"
                    )
                  ]
                }
              </p>
              <p className="mt-2 text-xs text-slate-400">
                Worked out here the way the API works it out, from where the two
                parties are and whether the registration has been confirmed.
                Every invoice carries its own answer, decided when it was
                generated.
              </p>
            </div>
          )}
        </div>
      )}

      <Dialog open={editOpen} onOpenChange={setEditOpen}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              {missing ? "Record billing details" : "Edit billing details"}
            </DialogTitle>
            <DialogDescription>{SAVING_IS_AN_UPSERT}</DialogDescription>
          </DialogHeader>

          <div className="max-h-[60vh] space-y-4 overflow-y-auto pr-1">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.issuedBy")}
              </label>
              <Select
                value={billingEntityId}
                onValueChange={setBillingEntityId}
                disabled={saving}
              >
                <SelectTrigger>
                  <SelectValue placeholder={t("billing.pickAnIssuingEntity")} />
                </SelectTrigger>
                <SelectContent>
                  {selectableEntities(entities).map((entity) => (
                    <SelectItem
                      key={entity.billingEntityId}
                      value={entity.billingEntityId}
                    >
                      {entity.legalName} · {entity.countryCode} ·{" "}
                      {entity.invoiceNumberPrefix}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-500">
                Which of the platform&apos;s legal entities invoices this
                customer. It decides the numbering series their invoices come
                out of and half of how their tax is worked out.
              </p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.legalName")}
              </label>
              <Input
                value={legalName}
                onChange={(event) => setLegalName(event.target.value)}
                disabled={saving}
              />
              <p className="text-xs text-slate-500">{LEGAL_NAME_HINT}</p>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("billing.contactName")}
                </label>
                <Input
                  value={contactName}
                  onChange={(event) => setContactName(event.target.value)}
                  disabled={saving}
                />
              </div>

              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("billing.contactEmail")}
                </label>
                <Input
                  value={contactEmail}
                  onChange={(event) => setContactEmail(event.target.value)}
                  inputMode="email"
                  disabled={saving}
                />
              </div>
            </div>
            <p className="text-xs text-slate-500">{CONTACT_HINT}</p>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.address")}
              </label>
              <Textarea
                value={address}
                onChange={(event) => setAddress(event.target.value)}
                rows={4}
                disabled={saving}
              />
              <p className="text-xs text-slate-500">
                {t("billing.printedOnTheInvoiceExactly")}
              </p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.country")}
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
              <p className="text-xs text-slate-500">{COUNTRY_HINT}</p>
              <p className="text-xs text-slate-400">
                {JURISDICTION_LIST_HINT}
              </p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("billing.taxRegistration")}
              </label>
              <Input
                value={taxIdentifier}
                onChange={(event) => setTaxIdentifier(event.target.value)}
                className="font-mono"
                placeholder={t("billing.none")}
                disabled={saving}
              />
              <p className="text-xs text-slate-500">
                {expectedPrefixHint(countryCode)} Recorded here, and it does not
                move the tax until somebody verifies it — which is a separate
                act on the panel behind this form.
              </p>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
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
                <p className="text-xs text-slate-500">{CURRENCY_HINT}</p>
              </div>

              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("billing.paymentTermsDays")}
                </label>
                <Input
                  value={paymentTerms}
                  onChange={(event) => setPaymentTerms(event.target.value)}
                  inputMode="numeric"
                  className="font-mono"
                  disabled={saving}
                />
                <p className="text-xs text-slate-500">{PAYMENT_TERMS_HINT}</p>
              </div>
            </div>

            {clearsVerification && (
              <p className="flex items-start gap-2 rounded-lg border border-attention-200 bg-attention-50 p-3 text-xs text-attention-700">
                <TriangleAlert className="mt-0.5 size-3.5 flex-shrink-0" />
                <span>{CHANGE_CLEARS_VERIFICATION}</span>
              </p>
            )}

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
    </div>
  );
}
