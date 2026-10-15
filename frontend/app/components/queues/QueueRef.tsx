import Link from "next/link";

import { cn } from "@/lib/utils";
import { shortId } from "@/lib/resources";
import { useTranslation } from "@/context/LocalizationContext";

export function QueueRef({
  queueId,
  queueName,
  href,
  className,
}: {
  queueId: string | null;
  queueName: string | null;
  href?: (queueId: string) => string;
  className?: string;
}) {
  const t = useTranslation();
  if (!queueId) {
    return (
      <span
        className={cn(
          "font-mono text-[11px] uppercase tracking-widest text-slate-400",
          className
        )}
        title={t("queues.noQueueSCriteriaMatched")}
      >
        {t("queues.unrouted")}
      </span>
    );
  }

  const label = queueName || shortId(queueId);

  if (!href) {
    return <span className={cn("text-sm text-slate-900", className)}>{label}</span>;
  }

  return (
    <Link
      href={href(queueId)}
      className={cn("text-sm text-brand-800 hover:underline", className)}
    >
      {label}
    </Link>
  );
}
