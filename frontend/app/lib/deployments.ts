export const versionLabel = (
  versionId: string | null,
  index: Record<string, number>
): string => {
  if (!versionId) return "—";
  const number = index[versionId];
  return number === undefined ? `${versionId.slice(0, 8)}…` : `v${number}`;
};

export const STRATEGY_LABELS: Record<string, string> = {
  AllAtOnce: "All at once",
  Gradual: "Gradual",
};

export const DEPLOYMENT_STATUS_LABELS: Record<string, string> = {
  PendingApproval: "Pending approval",
  InProgress: "In progress",
  Completed: "Completed",
  Cancelled: "Cancelled",
  Failed: "Failed",
};

export const isPendingApprovalDeployment = (deployment: {
  status: string;
}): boolean => deployment.status === "PendingApproval";
