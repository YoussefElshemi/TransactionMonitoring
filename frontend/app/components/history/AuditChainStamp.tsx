"use client";

import { useState } from "react";
import { ChevronRight } from "lucide-react";

import { CopyableText } from "@/components/apiClients/CopyableText";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

const ABBREVIATED_CHARS = 8;

const abbreviate = (hash: string): string =>
  `${hash.slice(0, ABBREVIATED_CHARS)}…`;

export function AuditChainStamp({
  sequence,
  hash,
  previousHash,
  className,
}: {
  sequence: number;
  hash: string;
  previousHash: string | null;
  className?: string;
}) {
  const t = useTranslation();
  const [expanded, setExpanded] = useState(false);

  return (
    <div className={cn("mt-2", className)}>
      <button
        type="button"
        onClick={() => setExpanded((open) => !open)}
        aria-expanded={expanded}
        className="group inline-flex items-center gap-1 font-mono text-[0.6875rem] text-slate-400 transition-colors hover:text-slate-600"
        title={`Entry ${sequence} in this organization's audit chain — SHA-256 ${hash}`}
      >
        <ChevronRight
          aria-hidden
          className={cn(
            "size-3 transition-transform",
            expanded && "rotate-90"
          )}
        />
        <span>#{sequence}</span>
        <span aria-hidden className="text-slate-300">
          ·
        </span>
        <span>{abbreviate(hash)}</span>
      </button>

      {expanded && (
        <dl className="mt-1.5 space-y-1 border-l border-slate-200 pl-3">
          <StampRow label={t("history.digest")} value={hash} />
          {previousHash ? (
            <StampRow label={t("history.previous")} value={previousHash} />
          ) : (
            <div className="flex flex-wrap items-baseline gap-x-2">
              <dt className="font-mono text-[0.6875rem] uppercase tracking-widest text-slate-400">
                {t("history.previous")}
              </dt>
              <dd className="text-xs text-slate-400">
                {t("history.noneThisIsTheFirst")}
              </dd>
            </div>
          )}
        </dl>
      )}
    </div>
  );
}

function StampRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-wrap items-baseline gap-x-2">
      <dt className="font-mono text-[0.6875rem] uppercase tracking-widest text-slate-400">
        {label}
      </dt>
      <dd className="min-w-0">
        <CopyableText value={value} />
      </dd>
    </div>
  );
}
