import {
  Approval,
  ApprovalResource,
  ApprovalStatus,
  ApplicationUser,
} from "@/types";

export const REVIEW_NOTE_MAX = 2048;

const SUBJECT_LABELS: Record<string, string> = {
  RuleDeployment: "Rule deployment",
  CaseResolution: "Case decision",
  SuspiciousActivityReport: "Filing",
};

export const approvalSubjectLabel = (subject: string): string =>
  SUBJECT_LABELS[subject] ?? subject;

const STATUS_LABELS: Record<string, string> = {
  Pending: "Pending",
  Approved: "Approved",
  Rejected: "Rejected",
  Withdrawn: "Withdrawn",
};

export const approvalStatusLabel = (status: string): string =>
  STATUS_LABELS[status] ?? status;

export const APPROVAL_STATUS_TEXT: Record<ApprovalStatus, string> = {
  Pending: "Waiting for somebody other than the requester to answer it.",
  Approved: "Agreed, and the action it was holding went ahead.",
  Rejected: "A reviewer read this and turned it down. Their note says why.",
  Withdrawn:
    "The requester took this back. Nobody turned it down — this is not a rejection and must never be counted as one.",
};

export const approvalStatusText = (status: string): string | null =>
  APPROVAL_STATUS_TEXT[status as ApprovalStatus] ?? null;

export const isPendingApproval = (approval: { status: string }): boolean =>
  approval.status === "Pending";

export const approvalOutcomeTextKey = (subject: string): string => {
  switch (subject) {
    case "RuleDeployment":
      return "approvals.outcomeApprovingStartsRollout";
    case "SuspiciousActivityReport":
      return "approvals.outcomeApprovingMarksReadyToFile";
    default:
      return "approvals.outcomeApprovingClosesCase";
  }
};

export const isRequester = (
  approval: Pick<Approval, "requestedByUserId">,
  user: Pick<ApplicationUser, "userId"> | null | undefined
): boolean => Boolean(user) && approval.requestedByUserId === user!.userId;

export interface ApprovalPermissions {
  canReview: boolean;
  canWithdraw: boolean;
  reviewBlockedReason: string | null;
}

export const approvalPermissions = (
  approval: Pick<Approval, "requestedByUserId" | "status">,
  user: Pick<ApplicationUser, "userId"> | null | undefined,
  canDecide: boolean
): ApprovalPermissions => {
  if (!isPendingApproval(approval)) {
    return { canReview: false, canWithdraw: false, reviewBlockedReason: null };
  }

  if (isRequester(approval, user)) {
    return {
      canReview: false,
      canWithdraw: true,
      reviewBlockedReason:
        "You raised this request — somebody else has to review it. Withdrawing it is the action available to you.",
    };
  }

  if (!canDecide) {
    return {
      canReview: false,
      canWithdraw: false,
      reviewBlockedReason:
        "Answering a request needs the approvals:decide permission, which is separate from being able to read one. You can follow where this stands, but not settle it.",
    };
  }

  return { canReview: true, canWithdraw: false, reviewBlockedReason: null };
};

export const ANY_APPROVAL_STATUS = "All";
export const ANY_APPROVAL_SUBJECT = "All";

export interface ApprovalQuery {
  pageNumber: number;
  pageSize: number;
  status: string;
  subject: string;
  fromDate: string;
  toDate: string;
}

export const approvalParams = (query: ApprovalQuery): URLSearchParams => {
  const params = new URLSearchParams({
    pageNumber: String(query.pageNumber),
    pageSize: String(query.pageSize),
  });
  if (query.status !== ANY_APPROVAL_STATUS) params.set("status", query.status);
  if (query.subject !== ANY_APPROVAL_SUBJECT) {
    params.set("subject", query.subject);
  }
  if (query.fromDate) params.set("fromDate", query.fromDate);
  if (query.toDate) params.set("toDate", query.toDate);
  return params;
};

export const approvalSearch = (query: ApprovalQuery): string =>
  approvalParams(query).toString();

export const approvalHref = (approvalId: string): string =>
  `/approvals/${approvalId}`;

export const findCaseApproval = (
  approvals: ApprovalResource[],
  caseId: string
): ApprovalResource | null =>
  approvals.find(({ approval }) => approval.caseId === caseId) ?? null;

export const findFilingApproval = (
  approvals: ApprovalResource[],
  suspiciousActivityReportId: string
): ApprovalResource | null =>
  approvals.find(
    ({ approval }) =>
      approval.suspiciousActivityReportId === suspiciousActivityReportId
  ) ?? null;

export const findDeploymentApproval = (
  approvals: ApprovalResource[],
  ruleDeploymentId: string
): ApprovalResource | null =>
  approvals.find(
    ({ approval }) => approval.ruleDeploymentId === ruleDeploymentId
  ) ?? null;

export const approvalAgeMs = (approval: Pick<Approval, "createdAt">): number =>
  Date.now() - new Date(approval.createdAt).getTime();

export const STALE_APPROVAL_MS = 24 * 60 * 60 * 1000;

export const isStaleApproval = (approval: Approval): boolean =>
  isPendingApproval(approval) && approvalAgeMs(approval) >= STALE_APPROVAL_MS;
