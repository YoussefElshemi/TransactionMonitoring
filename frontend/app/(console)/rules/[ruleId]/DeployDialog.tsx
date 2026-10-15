"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";

import {
  DeploymentStrategy,
  ProblemDetails,
  RuleDeployment,
  RuleDeploymentResource,
  RuleVersion,
} from "@/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { API_BASE, isPendingApprovalDeployment, reportApiError } from "./api";
import { unwrap } from "@/lib/api";
import { approvalHref } from "@/lib/approvals";
import { readDeploymentApproval } from "@/lib/useApprovals";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

interface DeployDialogProps {
  ruleId: string;
  version: RuleVersion | null;
  isRollback: boolean;
  requiresApproval: boolean | null;
  onOpenChange: (open: boolean) => void;
  onDeployed: (deployment: RuleDeployment | null) => void;
  onUnauthorized: () => void;
}

const DEFAULT_STEP_PERCENTAGE = "10";
const DEFAULT_STEP_INTERVAL = "60";

export function DeployDialog({
  ruleId,
  version,
  isRollback,
  requiresApproval,
  onOpenChange,
  onDeployed,
  onUnauthorized,
}: DeployDialogProps) {
  const t = useTranslation();
  const [strategy, setStrategy] = useState<DeploymentStrategy>("AllAtOnce");
  const [stepPercentage, setStepPercentage] = useState<string>(
    DEFAULT_STEP_PERCENTAGE
  );
  const [stepIntervalSeconds, setStepIntervalSeconds] = useState<string>(
    DEFAULT_STEP_INTERVAL
  );
  const [deploying, setDeploying] = useState<boolean>(false);
  const router = useRouter();

  useEffect(() => {
    if (version) {
      setStrategy("AllAtOnce");
      setStepPercentage(DEFAULT_STEP_PERCENTAGE);
      setStepIntervalSeconds(DEFAULT_STEP_INTERVAL);
    }
  }, [version]);

  const deploy = async () => {
    if (!version) return;

    const payload: {
      versionNumber: number;
      strategy: DeploymentStrategy;
      stepPercentage?: number;
      stepIntervalSeconds?: number;
    } = { versionNumber: version.versionNumber, strategy };

    if (strategy === "Gradual") {
      const percentage = Number(stepPercentage);
      const interval = Number(stepIntervalSeconds);
      if (!Number.isInteger(percentage) || percentage < 1 || percentage > 100) {
        return toast.error(t("rules.stepPercentageMustWholeNumber"));
      }
      if (!Number.isInteger(interval) || interval < 1) {
        return toast.error(t("rules.stepIntervalMustLeast1"));
      }
      payload.stepPercentage = percentage;
      payload.stepIntervalSeconds = interval;
    }

    setDeploying(true);
    try {
      const response = await apiFetch(`${API_BASE}/rules/${ruleId}/deployments`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
        credentials: "include",
      });

      if (response.ok) {
        const data = await unwrap<RuleDeploymentResource>(response);
        const deployment = data.ruleDeployment;

        if (isPendingApprovalDeployment(deployment)) {
          const approval = await readDeploymentApproval(
            deployment.ruleDeploymentId
          );
          toast.success(
            t("rules.versionSubmittedForApproval", version.versionNumber),
            {
              description:
                t("rules.notDeployedRuleStaysCurrent"),
              action: approval
                ? {
                    label: t("rules.openRequest"),
                    onClick: () =>
                      router.push(approvalHref(approval.approval.approvalId)),
                  }
                : undefined,
            }
          );
        } else {
          toast.success(
            strategy === "AllAtOnce"
              ? t("rules.versionNowLive", version.versionNumber)
              : t("rules.rollingOutVersion", version.versionNumber)
          );
        }

        onDeployed(deployment);
        onOpenChange(false);
      } else if (response.status === 409) {
        const errorData = (await response.json().catch(() => null)) as
          | ProblemDetails
          | null;
        toast.error(
          errorData?.detail ||
            t("rules.deploymentAlreadyProgressRuleWait")
        );
        onDeployed(null);
        onOpenChange(false);
      } else if (response.status === 404) {
        toast.error(t("rules.versionNoLongerExists"));
        onOpenChange(false);
      } else {
        await reportApiError(response, {
          fallback: t("rules.failedStartDeployment"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("rules.failedStartDeployment"));
    } finally {
      setDeploying(false);
    }
  };

  return (
    <Dialog open={version !== null} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {requiresApproval
              ? isRollback
                ? `Submit a rollback to version ${version?.versionNumber} for approval`
                : `Submit version ${version?.versionNumber} for approval`
              : `${isRollback ? "Roll back to" : "Deploy"} version ${version?.versionNumber}`}
          </DialogTitle>
          <DialogDescription>
            {requiresApproval
              ?
                isRollback
                ? "This does not roll anything back yet. It records what you are asking for and waits for somebody else to agree."
                : "This does not deploy anything yet. It records what you are asking for and waits for somebody else to agree."
              : isRollback
                ? "This replaces the live rule logic with an earlier version."
                : "This replaces the live rule logic. Name and enabled state are unaffected."}
          </DialogDescription>
        </DialogHeader>

        {requiresApproval && (
          <p className="rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs text-attention-700">
            Your organisation requires four eyes on rule deployments. The
            settings below are what a reviewer will see and what will run once
            they agree — and it cannot be you: whoever raises a request may not
            review it.
          </p>
        )}

        <div className="space-y-4 py-2">
          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("rules.strategy")}
            </label>
            <Select
              value={strategy}
              onValueChange={(v) => setStrategy(v as DeploymentStrategy)}
              disabled={deploying}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="AllAtOnce">
                  {t("rules.allAtOnceInstant100")}
                </SelectItem>
                <SelectItem value="Gradual">
                  {t("rules.gradualRampTrafficOverTime")}
                </SelectItem>
              </SelectContent>
            </Select>
          </div>

          {strategy === "Gradual" && (
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <label
                  htmlFor="stepPercentage"
                  className="font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {t("rules.stepPercentage")}
                </label>
                <Input
                  id="stepPercentage"
                  type="number"
                  min={1}
                  max={100}
                  value={stepPercentage}
                  disabled={deploying}
                  onChange={(e) => setStepPercentage(e.target.value)}
                />
              </div>
              <div className="space-y-1.5">
                <label
                  htmlFor="stepIntervalSeconds"
                  className="font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {t("rules.stepIntervalSeconds")}
                </label>
                <Input
                  id="stepIntervalSeconds"
                  type="number"
                  min={1}
                  value={stepIntervalSeconds}
                  disabled={deploying}
                  onChange={(e) => setStepIntervalSeconds(e.target.value)}
                />
              </div>
              <p className="col-span-2 text-xs text-slate-400">
                {requiresApproval && "Once approved, t"}
                {!requiresApproval && "T"}raffic shifts by{" "}
                {stepPercentage || "…"}% every {stepIntervalSeconds || "…"}s. The
                live rule only flips to this version once the rollout reaches
                100%.
              </p>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={deploying}
          >
            {t("common.cancel")}
          </Button>
          <Button onClick={deploy} disabled={deploying}>
            {requiresApproval
              ? deploying
                ? "Submitting..."
                : "Submit for approval"
              : deploying
                ? "Starting..."
                : isRollback
                  ? "Roll back"
                  : "Deploy"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
