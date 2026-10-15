"use client";

import { useEffect, useState } from "react";

import {
  RateLimitStatus,
  subscribeToRateLimit,
} from "@/lib/rateLimit";
import { useTranslation } from "@/context/LocalizationContext";

const ANNOUNCE_AFTER_MS = 3_000;

const SHOW_COUNTDOWN_ABOVE_MS = 5_000;

const secondsUntil = (at: number): number =>
  Math.max(0, Math.ceil((at - Date.now()) / 1000));

export default function RateLimitBanner() {
  const t = useTranslation();
  const [status, setStatus] = useState<RateLimitStatus | null>(null);
  const [now, setNow] = useState<number>(() => Date.now());

  useEffect(() => subscribeToRateLimit(setStatus), []);

  const throttled = status?.throttled ?? false;
  const resumesAt = status?.resumesAt ?? null;

  useEffect(() => {
    if (!throttled || resumesAt === null) return;

    const timer = setInterval(() => setNow(Date.now()), 500);
    return () => clearInterval(timer);
  }, [throttled, resumesAt]);

  if (!status) return null;

  const waitingLongEnough = status.longestWaitMs >= ANNOUNCE_AFTER_MS;
  const remainingMs = resumesAt === null ? 0 : resumesAt - now;

  if (!status.throttled && !waitingLongEnough) return null;
  if (status.waiting === 0 && remainingMs <= 0) return null;

  const seconds = resumesAt === null ? 0 : secondsUntil(resumesAt);
  const showCountdown = remainingMs > SHOW_COUNTDOWN_ABOVE_MS;

  return (
    <div
      role="status"
      aria-live="polite"
      className="sticky top-0 z-50 flex items-center justify-center gap-3 border-b border-brand-200 bg-brand-50 px-4 py-2 text-sm text-brand-900"
    >
      <span
        aria-hidden
        className="size-3.5 shrink-0 animate-spin rounded-full border-2 border-brand-800 border-t-transparent"
      />

      <span>
        <span className="font-medium">{t("rateLimitBanner.theApiIsBusy")}</span>{" "}
        {showCountdown ? (
          <>
            Waiting {seconds} second{seconds === 1 ? "" : "s"} before retrying —
            nothing has been lost.
          </>
        ) : (
          <>Retrying automatically — nothing has been lost.</>
        )}
      </span>
    </div>
  );
}
