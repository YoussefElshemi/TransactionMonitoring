"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { Download, RefreshCw, Trash2 } from "lucide-react";

import { ATTACHMENT_SCAN_STATUSES, AttachmentResource } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  canDownload,
  canRescan,
  downloadBlockedReason,
  formatBytes,
  isPurged,
} from "@/lib/attachments";
import { relationText, userName } from "@/lib/resources";
import { AttachmentDetailDialog } from "@/components/attachments/AttachmentDetailDialog";
import { AttachmentUploadZone } from "@/components/attachments/AttachmentUploadZone";
import { ScanStatusBadge } from "@/components/attachments/ScanStatusBadge";
import { LocalTime } from "@/components/LocalTime";
import Pagination from "@/components/Pagination";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const ANY_STATUS = "All";
const PAGE_SIZE = 10;

export function AttachmentsPanel({
  caseId,
  onUnauthorized,
}: {
  caseId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [attachments, setAttachments] = useState<AttachmentResource[]>([]);
  const [fileName, setFileName] = useState("");
  const [scanStatus, setScanStatus] = useState<string>(ANY_STATUS);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [detail, setDetail] = useState<AttachmentResource | null>(null);
  const [rescanning, setRescanning] = useState<string | null>(null);

  const fetchAttachments = useCallback(async () => {
    setLoading(true);

    const params = new URLSearchParams({
      PageNumber: String(pageNumber),
      PageSize: String(PAGE_SIZE),
    });
    if (fileName.trim()) params.set("FileName", fileName.trim());
    if (scanStatus !== ANY_STATUS) params.set("ScanStatus", scanStatus);

    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/attachments?${params}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<AttachmentResource>(response);
        setAttachments(page.data);
        setTotalPages(page.metadata.pageCount || 1);
        setTotalCount(page.metadata.totalCount);
      } else {
        setAttachments([]);
        await reportApiError(response, {
          fallback: t("attachments.errorOccurredFetchingCaseS"),
          onUnauthorized,
          toastId: "error-fetching-attachments",
        });
      }
    } catch {
      setAttachments([]);
      toast.error(t("attachments.errorOccurredFetchingCaseS"), {
        id: "error-fetching-attachments",
      });
    } finally {
      setLoading(false);
    }
  }, [caseId, fileName, scanStatus, pageNumber, onUnauthorized]);

  useEffect(() => {
    const timer = setTimeout(fetchAttachments, fileName ? 300 : 0);
    return () => clearTimeout(timer);
  }, [fetchAttachments, fileName]);

  const handleDownload = async (attachmentId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/attachments/${attachmentId}/content`,
        { method: "GET", credentials: "include" }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("attachments.fileCouldNotDownloaded"),
          onUnauthorized,
          toastId: `download-${attachmentId}`,
        });
        if (response.status === 409) fetchAttachments();
        return;
      }

      // `Content-Disposition` is not exposed cross-origin, so the name comes
      // from the row rather than from the header. The row is where the analyst
      // read it anyway.
      const known = attachments.find(
        (resource) => resource.attachment.attachmentId === attachmentId
      );
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = known?.attachment.fileName ?? "attachment";
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(url);
    } catch {
      toast.error(t("attachments.fileCouldNotDownloaded"), {
        id: `download-${attachmentId}`,
      });
    }
  };

  const handleRescan = async (attachmentId: string) => {
    setRescanning(attachmentId);
    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/attachments/${attachmentId}/rescan`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const resource = await unwrap<AttachmentResource>(response);
        setAttachments((current) =>
          current.map((existing) =>
            existing.attachment.attachmentId === attachmentId
              ? resource
              : existing
          )
        );
        setDetail((current) =>
          current?.attachment.attachmentId === attachmentId ? resource : current
        );
        toast.success(
          t(
            "attachments.rescannedCameBack",
            t(
              `attachments.scanStatus.${String(
                resource.attachment.scanStatus
              ).toLowerCase()}`
            )
          )
        );
        return;
      }

      await reportApiError(response, {
        fallback: t("attachments.fileCouldNotRescanned"),
        onUnauthorized,
        toastId: `rescan-${attachmentId}`,
      });
      if (response.status === 409) fetchAttachments();
    } catch {
      toast.error(t("attachments.fileCouldNotRescanned"), {
        id: `rescan-${attachmentId}`,
      });
    } finally {
      setRescanning(null);
    }
  };

  const handleDelete = async (attachmentId: string, name: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/attachments/${attachmentId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("common.namedCouldNotRemoved", name),
          onUnauthorized,
          toastId: `delete-${attachmentId}`,
        });
        return;
      }

      setDetail((current) =>
        current?.attachment.attachmentId === attachmentId ? null : current
      );
      fetchAttachments();
      toast.success(t("attachments.namedRemoved", name), {
        description:
          t("attachments.fileItselfKeptSoCan"),
        action: {
          label: t("common.undo"),
          onClick: () => handleRecover(attachmentId, name),
        },
      });
    } catch {
      toast.error(t("common.namedCouldNotRemoved", name), {
        id: `delete-${attachmentId}`,
      });
    }
  };

  const handleRecover = async (attachmentId: string, name: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/cases/${caseId}/attachments/${attachmentId}/recover`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (!response.ok) {
        await reportApiError(response, {
          fallback: t("common.namedCouldNotRestored", name),
          onUnauthorized,
          toastId: `recover-${attachmentId}`,
        });
        return;
      }

      fetchAttachments();
      toast.success(t("common.namedRestored", name));
    } catch {
      toast.error(t("common.namedCouldNotRestored", name), {
        id: `recover-${attachmentId}`,
      });
    }
  };

  const filtered = fileName.trim() !== "" || scanStatus !== ANY_STATUS;

  return (
    <div className="space-y-4">
      <AttachmentUploadZone
        caseId={caseId}
        onUnauthorized={onUnauthorized}
        onUploaded={(resource) => {
          const verdict = String(resource.attachment.scanStatus);
          if (verdict === "Infected") {
            toast.error(t("attachments.namedIsInfected", resource.attachment.fileName), {
              description: resource.attachment.threatName
                ? t(
              "attachments.scannerIdentified",
              resource.attachment.threatName
            )
                : t("attachments.fileDestroyedRecordUploadKept"),
            });
          } else if (verdict === "Failed") {
            toast.warning(
              t(
            "attachments.storedScanUnfinished",
            resource.attachment.fileName
          ),
              {
                description:
                  t("attachments.notFindingAboutFileNobody"),
              }
            );
          } else {
            toast.success(t("attachments.namedAttached", resource.attachment.fileName));
          }

          setPageNumber(1);
          fetchAttachments();
        }}
      />

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Input
            value={fileName}
            onChange={(event) => {
              setFileName(event.target.value);
              setPageNumber(1);
            }}
            placeholder={t("attachments.searchFileNames")}
            aria-label={t("attachments.searchFileNames")}
            className="w-56"
          />
          <Select
            value={scanStatus}
            onValueChange={(value) => {
              setScanStatus(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-44" aria-label={t("attachments.filterByScanStatus")}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_STATUS}>{t("attachments.anyScanStatus")}</SelectItem>
              {ATTACHMENT_SCAN_STATUSES.map((candidate) => (
                <SelectItem key={candidate} value={candidate}>
                  {candidate}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        {!loading && totalCount > 0 && (
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            {totalCount} {totalCount === 1 ? "file" : "files"}
          </p>
        )}
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("attachments.file")}</TableHead>
              <TableHead className="w-24">{t("attachments.size")}</TableHead>
              <TableHead className="w-56">{t("attachments.uploadedBy")}</TableHead>
              <TableHead className="w-44">{t("attachments.uploaded")}</TableHead>
              <TableHead className="w-32">{t("attachments.scan")}</TableHead>
              <TableHead className="w-32 text-right">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 3 }).map((_, index) => (
                <TableRow key={index}>
                  <TableCell colSpan={6}>
                    <Skeleton className="h-8 w-full" />
                  </TableCell>
                </TableRow>
              ))
            ) : attachments.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="py-12 text-center">
                  <p className="text-sm text-slate-500">
                    {filtered
                      ? "No attachment matches that"
                      : "Nothing attached to this case yet"}
                  </p>
                  {!filtered && (
                    <p className="mt-1 text-xs text-slate-400">
                      Statements, screenshots and correspondence belong here
                      rather than in a note — a note can describe evidence, it
                      cannot be it.
                    </p>
                  )}
                </TableCell>
              </TableRow>
            ) : (
              attachments.map((resource) => {
                const { attachment } = resource;
                const blocked = downloadBlockedReason(attachment);

                return (
                  <TableRow
                    key={attachment.attachmentId}
                    className="align-top hover:bg-slate-50/50"
                  >
                    <TableCell className="max-w-xs">
                      <button
                        type="button"
                        onClick={() => setDetail(resource)}
                        className="break-all text-left text-sm text-brand-800 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-800"
                      >
                        {attachment.fileName}
                      </button>
                      {isPurged(attachment) && (
                        <p className="mt-0.5 text-[11px] text-critical-700">
                          {attachment.threatName
                            ? `${attachment.threatName} — file destroyed`
                            : "File destroyed"}
                        </p>
                      )}
                    </TableCell>
                    <TableCell className="font-mono text-xs text-slate-500">
                      {formatBytes(attachment.byteCount)}
                    </TableCell>
                    <TableCell className="break-all text-xs text-slate-500">
                      {relationText(
                        userName(resource.uploadedByUser),
                        attachment.uploadedByUserId
                      )}
                    </TableCell>
                    <TableCell className="font-mono text-xs text-slate-500">
                      <LocalTime value={attachment.createdAt} />
                    </TableCell>
                    <TableCell>
                      <ScanStatusBadge scanStatus={attachment.scanStatus} />
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center justify-end gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-slate-400 hover:text-slate-700"
                          disabled={!canDownload(attachment)}
                          title={blocked ?? `Download ${attachment.fileName}`}
                          aria-label={blocked ?? `Download ${attachment.fileName}`}
                          onClick={() => handleDownload(attachment.attachmentId)}
                        >
                          <Download className="size-4" />
                          <span className="sr-only">
                            {blocked
                              ? `Cannot download ${attachment.fileName}: ${blocked}`
                              : `Download ${attachment.fileName}`}
                          </span>
                        </Button>
                        {canRescan(attachment) && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-slate-400 hover:text-slate-700"
                            disabled={rescanning === attachment.attachmentId}
                            title={t("attachments.rescan")}
                            aria-label={t("attachments.rescan")}
                            onClick={() => handleRescan(attachment.attachmentId)}
                          >
                            <RefreshCw
                              className={
                                rescanning === attachment.attachmentId
                                  ? "size-4 animate-spin"
                                  : "size-4"
                              }
                            />
                            <span className="sr-only">
                              Rescan {attachment.fileName}
                            </span>
                          </Button>
                        )}
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-critical-500 hover:text-critical-700"
                          title={t("attachments.remove")}
                          aria-label={t("attachments.remove")}
                          onClick={() =>
                            handleDelete(
                              attachment.attachmentId,
                              attachment.fileName
                            )
                          }
                        >
                          <Trash2 className="size-4" />
                          <span className="sr-only">
                            Remove {attachment.fileName}
                          </span>
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {attachments.length > 0 && totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((p) => Math.min(totalPages, p + 1))
          }
        />
      )}

      <AttachmentDetailDialog
        resource={detail}
        open={detail !== null}
        onOpenChange={(open) => !open && setDetail(null)}
        onDownload={handleDownload}
        onRescan={handleRescan}
        rescanning={rescanning === detail?.attachment.attachmentId}
        onUnauthorized={onUnauthorized}
      />
    </div>
  );
}
