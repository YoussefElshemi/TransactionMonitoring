"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Download } from "lucide-react";

import { ProblemDetails } from "@/types";
import { reportApiError } from "@/lib/api";
import {
  auditExportPath,
  AuditExportScope,
  auditExportParams,
} from "@/lib/audit";
import { saveResponseAsFile } from "@/lib/download";
import { Button } from "@/components/ui/button";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const fallbackFileName = (): string =>
  `audit-log-${new Date().toISOString().replace(/[-:]/g, "").replace(/\.\d+Z$/, "Z")}.csv`;

export function AuditExportButton({
  scope,
  organizationId,
  label = "Export CSV",
  variant = "outline",
  size = "sm",
  className,
  onRefused,
  onUnauthorized,
}: {
  scope: AuditExportScope;
  organizationId?: string;
  label?: string;
  variant?: "outline" | "ghost" | "default" | "secondary";
  size?: "sm" | "default";
  className?: string;
  onRefused?: (detail: string | null) => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [exporting, setExporting] = useState(false);

  const handleExport = async () => {
    setExporting(true);
    onRefused?.(null);

    try {
      const params = auditExportParams(scope);
      const query = params.toString();
      const response = await apiFetch(
        `${auditExportPath(organizationId)}${query ? `?${query}` : ""}`,
        { method: "GET", credentials: "include" }
      );

      if (!response.ok) {
        if (response.status === 409) {
          const problem = (await response.json().catch(() => null)) as
            | ProblemDetails
            | null;
          const detail =
            problem?.detail ||
            "This range holds more entries than one export can carry. Narrow the dates or name an entity.";
          onRefused?.(detail);
          toast.error(t("audit.tooManyEntriesToExport"), {
            id: "audit-export",
            description: t("audit.nothingTruncated", detail),
            duration: 15_000,
          });
          return;
        }

        await reportApiError(response, {
          fallback: t("audit.auditLogCouldNotExported"),
          onUnauthorized,
          toastId: "audit-export",
        });
        return;
      }

      await saveResponseAsFile(response, fallbackFileName());
    } catch {
      toast.error(t("audit.auditLogCouldNotExported"), {
        id: "audit-export",
      });
    } finally {
      setExporting(false);
    }
  };

  return (
    <Button
      variant={variant}
      size={size}
      className={className}
      disabled={exporting}
      onClick={handleExport}
    >
      <Download />
      {exporting ? "Preparing..." : label}
    </Button>
  );
}
