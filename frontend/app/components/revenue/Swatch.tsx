"use client";

import type { RevenueFill, RevenueTone } from "@/lib/revenue";
import { cn } from "@/lib/utils";

export const TONE_FILL_CLASS: Record<RevenueTone, string> = {
  settled: "fill-settled-700",
  inflight: "fill-inflight-700",
  attention: "fill-attention-700",
  elevated: "fill-elevated-700",
  critical: "fill-critical-700",
  inert: "fill-slate-500",
};

export const TONE_STROKE_CLASS: Record<RevenueTone, string> = {
  settled: "stroke-settled-700",
  inflight: "stroke-inflight-700",
  attention: "stroke-attention-700",
  elevated: "stroke-elevated-700",
  critical: "stroke-critical-700",
  inert: "stroke-slate-500",
};

export const TONE_BG_CLASS: Record<RevenueTone, string> = {
  settled: "bg-settled-700",
  inflight: "bg-inflight-700",
  attention: "bg-attention-700",
  elevated: "bg-elevated-700",
  critical: "bg-critical-700",
  inert: "bg-slate-500",
};

export const hatchId = (prefix: string, tone: RevenueTone): string =>
  `${prefix}-hatch-${tone}`;

export const markFill = (
  prefix: string,
  tone: RevenueTone,
  fill: RevenueFill
): { className?: string; fill?: string } =>
  fill === "hatch"
    ? { fill: `url(#${hatchId(prefix, tone)})` }
    : { className: TONE_FILL_CLASS[tone] };

export function RevenueHatchDefs({
  prefix,
  tones,
}: {
  prefix: string;
  tones: readonly RevenueTone[];
}) {
  return (
    <defs>
      {tones.map((tone) => (
        <pattern
          key={tone}
          id={hatchId(prefix, tone)}
          width={7}
          height={7}
          patternUnits="userSpaceOnUse"
          patternTransform="rotate(45)"
        >
          <rect
            width={7}
            height={7}
            className={TONE_FILL_CLASS[tone]}
            opacity={0.35}
          />
          <line
            x1={0}
            y1={0}
            x2={0}
            y2={7}
            strokeWidth={3.5}
            className={TONE_STROKE_CLASS[tone]}
          />
        </pattern>
      ))}
    </defs>
  );
}

export function RevenueSwatch({
  tone,
  fill,
  className,
}: {
  tone: RevenueTone;
  fill: RevenueFill;
  className?: string;
}) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "inline-block size-3 flex-shrink-0 rounded-[3px] align-[-1px]",
        TONE_BG_CLASS[tone],
        fill === "hatch" &&
          "bg-[repeating-linear-gradient(45deg,transparent_0_2px,rgba(255,255,255,0.75)_2px_4px)]",
        className
      )}
    />
  );
}

export function RevenueLegendItem({
  tone,
  fill,
  label,
  title,
}: {
  tone: RevenueTone;
  fill: RevenueFill;
  label: string;
  title?: string;
}) {
  return (
    <span
      className="inline-flex items-center gap-1.5 text-xs text-slate-600"
      title={title}
    >
      <RevenueSwatch tone={tone} fill={fill} />
      <span>{label}</span>
    </span>
  );
}
