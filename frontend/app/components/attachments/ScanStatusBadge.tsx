import { AttachmentScanStatus } from "@/types";
import {
  SCAN_STATUS_BADGE,
  SCAN_STATUS_HINT,
  SCAN_STATUS_LABEL,
} from "@/lib/attachments";
import { StatusBadge } from "@/components/StatusBadge";

export function ScanStatusBadge({
  scanStatus,
}: {
  scanStatus: AttachmentScanStatus | (string & {});
}) {
  const known = scanStatus as AttachmentScanStatus;

  return (
    <span title={SCAN_STATUS_HINT[known]}>
      <StatusBadge
        status={SCAN_STATUS_BADGE[known] ?? scanStatus}
        label={SCAN_STATUS_LABEL[known] ?? scanStatus}
      />
    </span>
  );
}
