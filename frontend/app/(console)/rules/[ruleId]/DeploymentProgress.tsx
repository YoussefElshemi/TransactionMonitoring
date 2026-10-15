import { cn } from "@/lib/utils";

export function DeploymentProgress({
  percentage,
  muted = false,
  className,
}: {
  percentage: number;
  muted?: boolean;
  className?: string;
}) {
  const clamped = Math.min(100, Math.max(0, percentage));
  return (
    <div className={cn("flex items-center gap-2", className)}>
      <div className="h-2 flex-1 overflow-hidden rounded-full bg-slate-200">
        <div
          className={cn(
            "h-full rounded-full transition-all duration-500",
            muted ? "bg-slate-400" : "bg-brand-600"
          )}
          style={{ width: `${clamped}%` }}
        />
      </div>
      <span
        className={cn(
          "w-10 shrink-0 text-right text-xs tabular-nums",
          muted ? "text-slate-400" : "text-slate-600"
        )}
      >
        {clamped}%
      </span>
    </div>
  );
}
