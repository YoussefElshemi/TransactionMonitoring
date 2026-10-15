import { Attachment, AttachmentScanStatus } from "@/types";

export const canDownload = (attachment: Attachment): boolean =>
  attachment.scanStatus === "Clean";

export const canRescan = (attachment: Attachment): boolean =>
  attachment.scanStatus === "Pending" || attachment.scanStatus === "Failed";

export const isPurged = (attachment: Attachment): boolean =>
  attachment.scanStatus === "Infected";

export const downloadBlockedReason = (attachment: Attachment): string | null => {
  switch (attachment.scanStatus) {
    case "Clean":
      return null;
    case "Pending":
      return "Not scanned yet, so nobody has cleared it to be opened.";
    case "Infected":
      return "The scanner found a threat and the file itself was destroyed. There is nothing left to download.";
    case "Failed":
      return "The scan could not finish, so this has not been cleared. Rescan it to try again.";
    default:
      return "This file has not been cleared for download.";
  }
};

export const SCAN_STATUS_BADGE: Record<AttachmentScanStatus, string> = {
  Clean: "Approved",
  Pending: "Assigned",
  Failed: "Undeployed",
  Infected: "Rejected",
};

export const SCAN_STATUS_LABEL: Record<AttachmentScanStatus, string> = {
  Pending: "Not scanned",
  Clean: "Clean",
  Infected: "Infected",
  Failed: "Scan failed",
};

export const SCAN_STATUS_HINT: Record<AttachmentScanStatus, string> = {
  Pending: "Stored, but no verdict has been recorded. Rescan to ask again.",
  Clean: "The scanner found nothing. This is the only state the file can be opened in.",
  Infected:
    "The scanner found a threat and the bytes were purged. The row is kept so the upload itself stays on the record.",
  Failed:
    "The scanner could not reach a verdict. This is not a finding about the file — rescan to ask again.",
};

export const MAX_ATTACHMENT_BYTES = 26214400;

export const MAX_FILE_NAME_LENGTH = 255;

export const ALLOWED_CONTENT_TYPES: readonly string[] = [
  "application/pdf",
  "image/png",
  "image/jpeg",
  "image/gif",
  "text/plain",
  "text/csv",
  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  "message/rfc822",
];

export const ALLOWED_EXTENSIONS =
  "PDF, PNG, JPEG, GIF, TXT, CSV, XLSX, DOCX, EML";

export const FILE_ACCEPT = [...ALLOWED_CONTENT_TYPES, ".eml"].join(",");

export const rejectionReason = (file: File): string | null => {
  if (file.size === 0) {
    return `${file.name} is empty, so there is nothing to attach.`;
  }

  if (file.size > MAX_ATTACHMENT_BYTES) {
    return `${file.name} is ${formatBytes(file.size)}. The limit is ${formatBytes(
      MAX_ATTACHMENT_BYTES
    )}.`;
  }

  if (file.name.length > MAX_FILE_NAME_LENGTH) {
    return `That file name is ${file.name.length} characters. The limit is ${MAX_FILE_NAME_LENGTH}.`;
  }

  if (
    !file.type ||
    !ALLOWED_CONTENT_TYPES.some(
      (allowed) => allowed.toLowerCase() === file.type.toLowerCase()
    )
  ) {
    return `${file.name} is ${
      file.type || "of a type your browser could not identify"
    }. Accepted types are ${ALLOWED_EXTENSIONS}.`;
  }

  return null;
};

export const formatBytes = (bytes: number): string => {
  if (bytes < 1024) return `${bytes} B`;

  const units = ["KiB", "MiB", "GiB"];
  let value = bytes / 1024;
  let unit = 0;

  while (value >= 1024 && unit < units.length - 1) {
    value /= 1024;
    unit += 1;
  }

  return `${value < 10 ? value.toFixed(1) : Math.round(value)} ${units[unit]}`;
};

export const shortChecksum = (checksum: string): string =>
  checksum.length <= 20
    ? checksum
    : `${checksum.slice(0, 10)}…${checksum.slice(-10)}`;

export const UPLOAD_FIELD_LABELS: Record<string, string> = {
  File: "File",
  "File.Length": "Size",
  "File.ContentType": "File type",
  "File.FileName": "File name",
};
