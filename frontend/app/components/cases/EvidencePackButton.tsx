"use client";

import { useState } from "react";
import { toast } from "sonner";
import { FileText } from "lucide-react";

import { API_BASE, reportApiError } from "@/lib/api";
import { saveResponseAsFile } from "@/lib/download";
import { Button } from "@/components/ui/button";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

export function EvidencePackButton({
  caseId,
  caseFriendlyName,
  onUnauthorized,
}: {
  caseId: string;
  caseFriendlyName: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [preparing, setPreparing] = useState(false);

  const handleDownload = async () => {
    setPreparing(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/evidence-pack`,
        { method: "GET", credentials: "include" }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("cases.evidencePackCouldNotProduced"),
          onUnauthorized,
          toastId: `evidence-pack-${caseId}`,
        });
        return;
      }

      await saveResponseAsFile(
        response,
        `evidence-pack-${caseFriendlyName}.pdf`
      );
    } catch {
      toast.error(t("cases.evidencePackCouldNotProduced"), {
        id: `evidence-pack-${caseId}`,
      });
    } finally {
      setPreparing(false);
    }
  };

  return (
    <Button
      variant="outline"
      size="sm"
      disabled={preparing}
      onClick={handleDownload}
    >
      <FileText />
      {preparing ? "Preparing..." : "Evidence pack"}
    </Button>
  );
}
