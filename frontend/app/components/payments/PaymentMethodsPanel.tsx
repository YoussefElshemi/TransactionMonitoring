"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { CreditCard, ExternalLink, Landmark, TriangleAlert } from "lucide-react";

import type { PaymentMethod, PaymentMethodSession } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import {
  ADDING_HAPPENS_AT_THE_PROVIDER,
  EXPIRED_MESSAGE,
  EXPIRING_SOON_MESSAGE,
  NOTHING_ON_FILE_DETAIL,
  NOTHING_ON_FILE_HEADLINE,
  REMOVING_HAPPENS_AT_THE_PROVIDER,
  SESSION_URLS_MUST_BE_HTTPS,
  daysUntilExpiry,
  hasExpired,
  isAbsoluteHttpsUrl,
  isExpiringSoon,
  methodsInReadingOrder,
  paymentMethodKindHintKey,
} from "@/lib/paymentMethods";
import { paymentMethodKindLabelKey } from "@/lib/payments";
import { usePermissions } from "@/context/PermissionsContext";
import { cn } from "@/lib/utils";
import { LocalTime } from "@/components/LocalTime";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useTranslation } from "@/context/LocalizationContext";

export function PaymentMethodsPanel({
  organizationId,
  returnPath = "/organization#Payment%20Methods",
  onUnauthorized,
}: {
  organizationId: string;
  returnPath?: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { holds, resolved } = usePermissions();

  const mayRead = holds("payment-methods:read");
  const mayWrite = holds("payment-methods:write");

  const [methods, setMethods] = useState<PaymentMethod[]>([]);
  const [loading, setLoading] = useState(true);
  const [starting, setStarting] = useState(false);

  const now = new Date();

  const fetchMethods = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/payment-methods`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        setMethods(await unwrap<PaymentMethod[]>(response));
      } else {
        await reportApiError(response, {
          fallback: t("payments.paymentMethodsCouldNotBeRead"),
          onUnauthorized,
          toastId: "error-fetching-payment-methods",
        });
      }
    } catch {
      toast.error(t("payments.paymentMethodsCouldNotBeRead"), {
        id: "error-fetching-payment-methods",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, onUnauthorized]);

  useEffect(() => {
    if (mayRead) fetchMethods();
    else setLoading(false);
  }, [mayRead, fetchMethods]);

  // Deliberately not idempotent: a replayed response would hand back an expired session URL.
  const addPaymentMethod = async () => {
    const returnUrl = new URL(returnPath, window.location.origin).toString();
    const cancelUrl = returnUrl;

    if (!isAbsoluteHttpsUrl(returnUrl)) {
      toast.error(t("payments.cannotStartAtTheProvider"), {
        description: t(SESSION_URLS_MUST_BE_HTTPS),
        id: "payment-method-session",
      });
      return;
    }

    setStarting(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/payment-methods/sessions`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ returnUrl, cancelUrl }),
        },
        { withoutIdempotencyKey: true }
      );

      if (response.ok) {
        const session = await unwrap<PaymentMethodSession>(response);
        window.location.href = session.url;
        return;
      }

      await reportApiError(response, {
        fallback: t("payments.cannotStartAtTheProvider"),
        onUnauthorized,
        toastId: "payment-method-session",
      });
    } catch {
      toast.error(t("payments.cannotStartAtTheProvider"), {
        id: "payment-method-session",
      });
    } finally {
      setStarting(false);
    }
  };

  if (!resolved) return null;

  if (!mayRead) {
    return (
      <div className="rounded-xl border border-slate-200 bg-card p-6">
        <p className="text-sm text-slate-500">
          {t("billing.needs")}{" "}
          <span className="font-mono text-xs">payment-methods:read</span>.
        </p>
      </div>
    );
  }

  const ordered = methodsInReadingOrder(methods, now);

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <CreditCard className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("payments.paymentMethods")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {t("payments.whatIsOnFileAtTheProvider")}
            </p>
          </div>
        </div>

        {mayWrite && (
          <Button size="sm" onClick={addPaymentMethod} disabled={starting}>
            <ExternalLink />
            {starting
              ? t("payments.openingTheProvider")
              : t("payments.addAPaymentMethod")}
          </Button>
        )}
      </div>

      {loading ? (
        <div className="space-y-2">
          {Array.from({ length: 2 }).map((_, row) => (
            <Skeleton key={row} className="h-20 w-full rounded-xl" />
          ))}
        </div>
      ) : ordered.length === 0 ? (
        <div className="rounded-xl border border-slate-200 bg-card p-6">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {t(NOTHING_ON_FILE_HEADLINE)}
          </p>
          <p className="mt-2 max-w-xl text-sm text-slate-500">
            {t(NOTHING_ON_FILE_DETAIL)}
          </p>
          {mayWrite && (
            <Button
              size="sm"
              className="mt-3"
              onClick={addPaymentMethod}
              disabled={starting}
            >
              <ExternalLink />
              {starting
                ? t("payments.openingTheProvider")
                : t("payments.addAPaymentMethod")}
            </Button>
          )}
        </div>
      ) : (
        <div className="space-y-2">
          {ordered.map((method) => (
            <PaymentMethodRow
              key={method.paymentMethodReference}
              method={method}
              now={now}
            />
          ))}
        </div>
      )}

      <div className="space-y-1 text-xs text-slate-400">
        <p>{t(REMOVING_HAPPENS_AT_THE_PROVIDER)}</p>
        {mayWrite && <p>{t(ADDING_HAPPENS_AT_THE_PROVIDER)}</p>}
      </div>
    </div>
  );
}

function PaymentMethodRow({
  method,
  now,
}: {
  method: PaymentMethod;
  now: Date;
}) {
  const t = useTranslation();

  const expired = hasExpired(method, now);
  const expiring = isExpiringSoon(method, now);
  const days = daysUntilExpiry(method, now);

  const Icon = method.paymentMethodKind === "Card" ? CreditCard : Landmark;

  return (
    <div
      className={cn(
        "rounded-xl border bg-card p-4",
        expired
          ? "border-critical-200 bg-critical-50"
          : expiring
            ? "border-attention-200 bg-attention-50"
            : "border-slate-200"
      )}
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="flex items-start gap-2">
          <Icon className="mt-0.5 size-4 flex-shrink-0 text-slate-400" />
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <p className="font-mono text-sm text-slate-900">{method.label}</p>

              {method.isDefault && (
                <span className="rounded-md border border-slate-200 bg-slate-100 px-2 py-0.5 font-mono text-[11px] text-slate-600">
                  {t("payments.default")}
                </span>
              )}
            </div>

            <p className="mt-1 text-xs text-slate-500">
              {t(paymentMethodKindLabelKey(method.paymentMethodKind))}
            </p>

            {method.mandateReference !== null && (
              <p className="mt-1 font-mono text-[11px] text-slate-400">
                {method.mandateReference}
              </p>
            )}
          </div>
        </div>

        {method.expiresAt !== null && (
          <div className="text-right">
            <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              {t("payments.expires")}
            </p>
            <p
              className={cn(
                "mt-0.5 text-xs",
                expired
                  ? "text-critical-700"
                  : expiring
                    ? "text-attention-700"
                    : "text-slate-600"
              )}
            >
              <LocalTime value={method.expiresAt} dateOnly />
            </p>
          </div>
        )}
      </div>

      {(expired || expiring) && (
        <p
          className={cn(
            "mt-3 flex items-start gap-2 text-xs",
            expired ? "text-critical-700" : "text-attention-700"
          )}
        >
          <TriangleAlert className="mt-0.5 size-3.5 flex-shrink-0" aria-hidden />
          <span>
            {t(expired ? EXPIRED_MESSAGE : EXPIRING_SOON_MESSAGE)}
            {expiring && days !== null && (
              <span className="mt-0.5 block text-[11px]">
                {t("payments.daysFromNow", days)}
              </span>
            )}
          </span>
        </p>
      )}

      <p className="mt-2 text-[11px] text-slate-400">
        {t(paymentMethodKindHintKey(method.paymentMethodKind))}
      </p>
    </div>
  );
}
