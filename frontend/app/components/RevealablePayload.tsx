"use client";

import { useState } from "react";
import { toast } from "sonner";

import { TransactionResource } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { usePermissions } from "@/context/PermissionsContext";
import { PayloadView } from "@/components/PayloadView";
import { RevealPayloadDialog } from "@/components/RevealPayloadDialog";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

export function RevealablePayload({
  transactionId,
  payload,
  masked,
  onUnauthorized,
  label,
}: {
  transactionId: string;
  payload: Record<string, unknown>;
  masked: boolean;
  onUnauthorized: () => void;
  label?: string;
}) {
  const t = useTranslation();
  const { holds } = usePermissions();
  const [revealed, setRevealed] = useState<Record<string, unknown> | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [revealing, setRevealing] = useState(false);

  const canReveal = holds("transactions:reveal");

  const handleReveal = async (reason: string) => {
    setRevealing(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/transactions/${transactionId}/reveal`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ reason }),
        }
      );

      if (response.ok) {
        const resource = await unwrap<TransactionResource>(response);
        setRevealed(resource.transaction.payload);
        setDialogOpen(false);
        return;
      }

      await reportApiError(response, {
        fallback: t("payloadFields.payloadCouldNotRevealed"),
        onUnauthorized,
        toastId: "error-revealing-payload",
        fieldLabels: { "Body.Reason": "Reason" },
      });
    } catch {
      toast.error(t("payloadFields.payloadCouldNotRevealed"), {
        id: "error-revealing-payload",
      });
    } finally {
      setRevealing(false);
    }
  };

  return (
    <>
      <PayloadView
        payload={revealed ?? payload}
        label={label}
        masked={masked && revealed === null}
        onReveal={
          masked && revealed === null && canReveal
            ? () => setDialogOpen(true)
            : undefined
        }
        revealing={revealing}
      />

      <RevealPayloadDialog
        open={dialogOpen}
        onOpenChange={(open) => !revealing && setDialogOpen(open)}
        submitting={revealing}
        onSubmit={handleReveal}
      />
    </>
  );
}
