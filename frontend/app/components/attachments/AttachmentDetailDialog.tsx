"use client";

import { AttachmentResource, AttachmentScanStatus } from "@/types";
import {
  SCAN_STATUS_HINT,
  canDownload,
  canRescan,
  downloadBlockedReason,
  formatBytes,
  isPurged,
  shortChecksum,
} from "@/lib/attachments";
import { relationText, userName } from "@/lib/resources";
import { CopyableText } from "@/components/apiClients/CopyableText";
import { Field, FieldList } from "@/components/FieldList";
import { ScanStatusBadge } from "@/components/attachments/ScanStatusBadge";
import { HistoryPanel, historyPath } from "@/components/history";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Download, RefreshCw } from "lucide-react";
import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

export function AttachmentDetailDialog({
  resource,
  open,
  onOpenChange,
  onDownload,
  onRescan,
  rescanning,
  onUnauthorized,
}: {
  resource: AttachmentResource | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onDownload: (attachmentId: string) => void;
  onRescan: (attachmentId: string) => void;
  rescanning: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const attachment = resource?.attachment ?? null;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle className="break-all pr-6">
            {attachment?.fileName ?? "Attachment"}
          </DialogTitle>
          <DialogDescription>
            {t("attachments.whatWasAttachedWhoAttached")}
          </DialogDescription>
        </DialogHeader>

        {attachment && (
          <div className="space-y-4">
            <div className="flex flex-wrap items-center gap-2">
              <ScanStatusBadge scanStatus={attachment.scanStatus} />
              <span className="text-xs text-slate-500">
                {SCAN_STATUS_HINT[attachment.scanStatus as AttachmentScanStatus] ??
                  "This verdict is not one this console knows about, so the file is treated as uncleared."}
              </span>
            </div>

            {isPurged(attachment) && (
              <div className="rounded-md border border-critical-200 bg-critical-50 px-3 py-2">
                <p className="text-sm font-medium text-critical-700">
                  {attachment.threatName
                    ? `The scanner identified ${attachment.threatName}.`
                    : "The scanner found a threat."}
                </p>
                <p className="mt-0.5 text-xs text-critical-700">
                  The file itself was destroyed. This record is kept on purpose:
                  it is the account of somebody having uploaded this file, on
                  this day, and of what was found in it.
                </p>
              </div>
            )}

            <div className="flex flex-wrap items-center gap-2">
              <Button
                size="sm"
                variant="outline"
                className="gap-1.5"
                disabled={!canDownload(attachment)}
                title={downloadBlockedReason(attachment) ?? undefined}
                onClick={() => onDownload(attachment.attachmentId)}
              >
                <Download className="size-4" />
                {t("attachments.download")}
              </Button>
              {canRescan(attachment) && (
                <Button
                  size="sm"
                  variant="outline"
                  className="gap-1.5"
                  disabled={rescanning}
                  onClick={() => onRescan(attachment.attachmentId)}
                >
                  <RefreshCw className="size-4" />
                  {rescanning ? "Scanning…" : "Rescan"}
                </Button>
              )}
              {!canDownload(attachment) && (
                <span className="text-xs text-slate-500">
                  {downloadBlockedReason(attachment)}
                </span>
              )}
            </div>

            <div className="overflow-hidden rounded-lg border border-slate-200">
              <FieldList>
                <Field label={t("attachments.size")} mono>
                  {formatBytes(attachment.byteCount)}
                </Field>
                <Field label={t("attachments.type")} mono>
                  {attachment.contentType}
                </Field>
                <Field label={t("attachments.checksum")}>
                  <CopyableText
                    value={attachment.checksum}
                    display={shortChecksum(attachment.checksum)}
                  />
                </Field>
                <Field label={t("attachments.uploadedBy")} mono>
                  {relationText(
                    userName(resource?.uploadedByUser),
                    attachment.uploadedByUserId
                  )}
                </Field>
                <Field label={t("attachments.uploaded")} mono>
                  <LocalTime value={attachment.createdAt} />
                </Field>
                <Field label={t("attachments.scanned")} mono>
                  {attachment.scannedAt ? (
                    <LocalTime value={attachment.scannedAt} />
                  ) : (
                    <span className="text-slate-400">{t("attachments.notYet")}</span>
                  )}
                </Field>
                {attachment.threatName && (
                  <Field label={t("attachments.threat")} mono>
                    {attachment.threatName}
                  </Field>
                )}
                <Field label={t("attachments.attachmentId")} mono>
                  <span className="break-all text-xs text-slate-500">
                    {attachment.attachmentId}
                  </span>
                </Field>
              </FieldList>
            </div>

            <div>
              <h3 className="mb-2 font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.history")}
              </h3>
              <HistoryPanel
                entityType="Attachment"
                path={historyPath.Attachment(
                  attachment.caseId,
                  attachment.attachmentId
                )}
                entityId={attachment.attachmentId}
                onUnauthorized={onUnauthorized}
                enabled={open}
                scopeNote="The scan verdict is written by the scanner rather than by a person, so the entries that change it have no author. A removal and a recovery are somebody's, and both are here."
              />
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
