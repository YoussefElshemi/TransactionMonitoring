import { format } from "date-fns";
import type { DateRange } from "react-day-picker";

import { AuditChainVerification, AuditEntityType } from "@/types";
import { organizationScopedPath } from "@/lib/api";

export const auditVerificationPath = (organizationId?: string): string =>
  organizationScopedPath("audit-logs/verification", organizationId);

export const auditExportPath = (organizationId?: string): string =>
  organizationScopedPath("audit-logs/export", organizationId);

const asDateParam = (date: Date): string => format(date, "yyyy-MM-dd");

export interface AuditExportScope {
  range?: DateRange;
  entityType?: AuditEntityType;
  entityId?: string;
}

export const auditRangeParams = (range?: DateRange): URLSearchParams => {
  const params = new URLSearchParams();
  if (range?.from) params.set("fromDate", asDateParam(range.from));
  if (range?.to) params.set("toDate", asDateParam(range.to));
  return params;
};

export const auditExportParams = ({
  range,
  entityType,
  entityId,
}: AuditExportScope): URLSearchParams => {
  const params = auditRangeParams(range);
  if (entityType && entityId) {
    params.set("entityType", entityType);
    params.set("entityId", entityId);
  }
  return params;
};

export type AuditBreakKind = "entry" | "anchor" | "both";

export const auditBreakKind = (
  verification: AuditChainVerification
): AuditBreakKind | null => {
  const entry = verification.firstBrokenAuditLogId !== null;
  const anchor = verification.firstBrokenAuditAnchorId !== null;

  if (entry && anchor) return "both";
  if (entry) return "entry";
  if (anchor) return "anchor";
  return null;
};

export const AUDIT_BREAK_COPY: Record<
  AuditBreakKind,
  { title: string; body: string }
> = {
  entry: {
    title: "This chain has been broken",
    body: "An entry's contents no longer match the digest recorded beside it, or it no longer names the entry before it — a field was changed, or an entry was removed or inserted. This is where the break starts, not where it ends: everything after a break re-hashes consistently on its own, so nothing here says how much of the trail beyond this point can be trusted.",
  },
  anchor: {
    title: "A signed anchor disagrees with this chain",
    body: "An anchor's signature failed to verify, or the chain no longer agrees with a position an anchor witnessed. Anchors are signed nightly and independently of the entries, so this is the check that catches a rewrite thorough enough to have re-closed every link it touched — the entries can be internally consistent and still not be what was witnessed at the time.",
  },
  both: {
    title: "This chain has been broken, and a signed anchor disagrees with it",
    body: "Both checks failed independently. An entry no longer matches the digest recorded beside it, and an anchor signed before that entry was written does not agree with the chain either. Treat the entry break as where the damage starts rather than where it ends, and the anchor as evidence that what is recorded now is not what was witnessed at the time.",
  },
};

export const auditIntactCaveat = (
  verification: AuditChainVerification
): string | null => {
  if (verification.entryCount === 0) {
    return "No entries fell in this range, so nothing was checked. An intact verdict over nothing is not evidence about anything.";
  }
  if (verification.anchorCount === 0) {
    return "No anchors covered this range, so the chain was checked only against itself. A rewrite that re-closed every link would pass this; it is the signed anchors that catch one, and the nightly job writes the first for a chain only after something has been written to it.";
  }
  return null;
};
