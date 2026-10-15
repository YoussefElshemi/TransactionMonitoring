"use client";

import { Clock, CircleCheck, CircleX, Undo2 } from "lucide-react";

import type { PaymentStatus } from "@/types";
import {
  paymentStatusHintKey,
  paymentStatusLabelKey,
  paymentStatusTone,
} from "@/lib/payments";
import { ToneBadge } from "@/components/status/StatusChips";
import { useTranslation } from "@/context/LocalizationContext";

const STATUS_ICONS: Record<PaymentStatus, typeof Clock> = {
  Pending: Clock,
  Settled: CircleCheck,
  Failed: CircleX,
  Returned: Undo2,
};

export function PaymentStatusBadge({
  status,
  className,
}: {
  status: PaymentStatus;
  className?: string;
}) {
  const t = useTranslation();

  const Icon = STATUS_ICONS[status] ?? Clock;
  const label = t(paymentStatusLabelKey(status));
  const hint = paymentStatusHintKey(status);

  return (
    <ToneBadge
      tone={paymentStatusTone(status)}
      title={hint ? t(hint) : undefined}
      className={className}
    >
      <span className="inline-flex items-center gap-1">
        <Icon className="size-3" aria-hidden />
        {label}
      </span>
    </ToneBadge>
  );
}
