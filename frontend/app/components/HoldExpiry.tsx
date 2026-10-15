"use client";

import { useEffect, useState } from "react";
import { Clock, Infinity as InfinityIcon, TimerOff } from "lucide-react";

import { Decision } from "@/types";
import {
  holdExpiryVerdictLabel,
  holdExpiryVerdictText,
  isSystemExpiry,
} from "@/lib/decisions";
import { cn } from "@/lib/utils";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const formatRemaining = (milliseconds: number): string => {
  const totalSeconds = Math.floor(milliseconds / 1000);
  if (totalSeconds < 60) return `${totalSeconds}s`;

  const totalMinutes = Math.floor(totalSeconds / 60);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;

  if (hours === 0) return `${minutes}m`;
  if (minutes === 0) return `${hours}h`;
  return `${hours}h ${minutes}m`;
};

const useTimeRemaining = (expiresAt: string | null): number | null => {
  const [remaining, setRemaining] = useState<number | null>(() =>
    expiresAt === null ? null : new Date(expiresAt).getTime() - Date.now()
  );

  useEffect(() => {
    if (expiresAt === null) {
      setRemaining(null);
      return;
    }

    const deadline = new Date(expiresAt).getTime();
    const read = () => deadline - Date.now();

    setRemaining(read());

    const interval = Math.abs(read()) < 60 * 60 * 1000 ? 1000 : 15000;
    const timer = window.setInterval(() => setRemaining(read()), interval);

    return () => window.clearInterval(timer);
    // Re-subscribing on the value (not the object) keeps the timer stable
    // across the refetches the case page does after every write.
  }, [expiresAt]);

  return remaining;
};

export function HoldCountdown({
  decision,
  className,
}: {
  decision: Decision;
  className?: string;
}) {
  const t = useTranslation();
  const remaining = useTimeRemaining(decision.expiresAt);

  if (decision.status !== "Held") return null;

  const verdict = decision.holdExpiryVerdict;

  if (verdict === null) return null;

  if (verdict === "Never") {
    return (
      <div
        className={cn(
          "flex items-start gap-2 rounded-md border border-inflight-200 bg-inflight-50 px-3 py-2",
          className
        )}
      >
        <InfinityIcon className="mt-0.5 size-4 flex-shrink-0 text-inflight-700" />
        <div className="min-w-0">
          <p className="text-sm font-medium text-inflight-700">
            {t("holdExpiry.noDeadlineThisHoldWaits")}
          </p>
          <p className="mt-0.5 text-xs text-inflight-700">
            Nothing will release or block this transaction on its own. It stays
            held until someone resolves the case.
          </p>
        </div>
      </div>
    );
  }

  const label = holdExpiryVerdictLabel(verdict);
  const meaning = holdExpiryVerdictText(verdict);

  const overdue = remaining !== null && remaining <= 0;

  return (
    <div
      className={cn(
        "flex items-start gap-2 rounded-md border border-attention-200 bg-attention-50 px-3 py-2",
        className
      )}
    >
      <Clock className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
      <div className="min-w-0">
        <p className="text-sm font-medium text-attention-700">
          {overdue ? (
            <>{label} — due now</>
          ) : (
            <>
              {label} in{" "}
              <span className="font-mono tabular-nums">
                {remaining === null ? "—" : formatRemaining(remaining)}
              </span>
            </>
          )}
        </p>
        <p className="mt-0.5 text-xs text-attention-700">
          {overdue
            ? "The deadline has passed. The transaction settles as soon as the expiry job next runs."
            : meaning}
          {decision.expiresAt && (
            <>
              {" "}
              <span className="text-attention-700">
                (<LocalTime value={decision.expiresAt} />)
              </span>
            </>
          )}
        </p>
      </div>
    </div>
  );
}

export function HoldOutcome({
  decision,
  className,
}: {
  decision: Decision;
  className?: string;
}) {
  const t = useTranslation();
  if (!isSystemExpiry(decision)) return null;

  return (
    <div
      className={cn(
        "flex items-start gap-2 rounded-md border border-slate-200 bg-slate-50 px-3 py-2",
        className
      )}
    >
      <TimerOff className="mt-0.5 size-4 flex-shrink-0 text-slate-500" />
      <div className="min-w-0">
        <p className="text-sm font-medium text-slate-800">
          {t("holdExpiry.settledByTheHoldExpiring")}
        </p>
        <p className="mt-0.5 text-xs text-slate-600">
          {decision.status === "Expired"
            ? "The hold ran out with no automatic verdict, so nothing was decided."
            : `The hold ran out and the ${decision.status.toLowerCase()} verdict applied automatically.`}
          {decision.resolvedAt && (
            <> <LocalTime value={decision.resolvedAt} />.</>
          )}
        </p>
      </div>
    </div>
  );
}
