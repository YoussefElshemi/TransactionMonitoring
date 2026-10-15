"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Download } from "lucide-react";

import { API_BASE, reportApiError } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { saveResponseAsFile } from "@/lib/download";
import {
  RECOGNITION_EXPLAINED,
  type MonthPoint,
  isForwardRange,
  monthsInRange,
  recognitionFileName,
  recognitionQuery,
} from "@/lib/revenue";
import { canReadRevenue } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useTranslation } from "@/context/LocalizationContext";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  MonthRangeFields,
  lastCompleteMonth,
  monthsBefore,
} from "@/components/revenue/MonthPicker";

export function RecognitionExportDialog({
  onUnauthorized,
}: {
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { grants } = usePermissions();

  const [open, setOpen] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [to, setTo] = useState<MonthPoint>(() => lastCompleteMonth());
  const [from, setFrom] = useState<MonthPoint>(() =>
    monthsBefore(lastCompleteMonth(), 12)
  );

  const mayExport = canReadRevenue(grants);
  const rangeIsForward = isForwardRange(from, to);

  if (!mayExport) return null;

  const download = async () => {
    setExporting(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/revenue/recognition?${recognitionQuery(from, to)}`,
        { method: "GET", credentials: "include" }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("revenue.recognitionCouldNotBeExported"),
          onUnauthorized,
          toastId: "revenue-recognition-export",
        });
        return;
      }

      await saveResponseAsFile(response, recognitionFileName(from, to));
      setOpen(false);
    } catch {
      toast.error(t("revenue.recognitionCouldNotBeExported"), {
        id: "revenue-recognition-export",
      });
    } finally {
      setExporting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          <Download />
          {t("revenue.exportRecognition")}
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("revenue.exportRecognition")}</DialogTitle>
          <DialogDescription>{t(RECOGNITION_EXPLAINED)}</DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          <MonthRangeFields
            from={from}
            to={to}
            onFrom={setFrom}
            onTo={setTo}
            disabled={exporting}
            invalid={!rangeIsForward}
          />

          {rangeIsForward && (
            <p className="text-xs text-slate-500">
              {t("revenue.monthsInTheFile", monthsInRange(from, to))}
            </p>
          )}
        </div>

        <DialogFooter>
          <Button
            onClick={download}
            disabled={exporting || !rangeIsForward}
          >
            {exporting ? t("revenue.preparing") : t("revenue.download")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
