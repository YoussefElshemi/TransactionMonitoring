import type {
  ComponentStatus,
  IncidentImpact,
  IncidentStatus,
  MaintenanceStatus,
} from "@/types";
import { Badge } from "@/components/ui/badge";
import { componentStatusLabel, componentStatusTone } from "@/lib/status";
import { incidentImpactTone, incidentStatusTone } from "@/lib/incidents";
import { MAINTENANCE_STATUS_LABELS } from "@/lib/maintenanceWindows";
import { cn } from "@/lib/utils";

export type Tone =
  | "settled"
  | "inflight"
  | "attention"
  | "elevated"
  | "critical"
  | "inert";

const TONE_CLASSES: Record<Tone, string> = {
  settled: "border-transparent bg-settled-50 text-settled-700",
  inflight: "border-inflight-200 bg-inflight-50 text-inflight-700",
  attention:
    "border-attention-200 bg-attention-50 text-attention-700 ring-1 ring-inset ring-attention-200",
  elevated:
    "border-elevated-200 bg-elevated-100 text-elevated-700 ring-1 ring-inset ring-elevated-200",
  critical:
    "border-critical-solid bg-critical-solid text-critical-solid-foreground",
  inert: "border-slate-200 bg-slate-100 text-slate-500",
};

export function ToneBadge({
  tone,
  children,
  title,
  className,
}: {
  tone: Tone;
  children: React.ReactNode;
  title?: string;
  className?: string;
}) {
  return (
    <Badge
      variant="outline"
      title={title}
      className={cn(
        "rounded-md px-2 py-0.5 font-mono text-[11px] font-medium",
        TONE_CLASSES[tone],
        className
      )}
    >
      {children}
    </Badge>
  );
}

export function ComponentStatusChip({
  status,
  className,
}: {
  status: ComponentStatus;
  className?: string;
}) {
  return (
    <ToneBadge tone={componentStatusTone(status)} className={className}>
      {componentStatusLabel(status)}
    </ToneBadge>
  );
}

export function IncidentStatusChip({
  status,
  className,
}: {
  status: IncidentStatus;
  className?: string;
}) {
  return (
    <ToneBadge tone={incidentStatusTone(status)} className={className}>
      {status}
    </ToneBadge>
  );
}

export function IncidentImpactChip({
  impact,
  className,
}: {
  impact: IncidentImpact;
  className?: string;
}) {
  return (
    <ToneBadge tone={incidentImpactTone(impact)} className={className}>
      {impact} impact
    </ToneBadge>
  );
}

export function MaintenanceStatusChip({
  status,
  className,
}: {
  status: MaintenanceStatus;
  className?: string;
}) {
  const tone: Tone =
    status === "InProgress"
      ? "inflight"
      : status === "Scheduled"
        ? "attention"
        : status === "Completed"
          ? "settled"
          : "inert";

  return (
    <ToneBadge tone={tone} className={className}>
      {MAINTENANCE_STATUS_LABELS[status] ?? status}
    </ToneBadge>
  );
}
