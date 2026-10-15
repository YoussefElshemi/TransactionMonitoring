import { cn } from "@/lib/utils";
import { TransactionSummary } from "@/types";
import { useTranslation } from "@/context/LocalizationContext";

export function TransactionReference({
  transaction,
  className,
}: {
  transaction: Pick<TransactionSummary, "friendlyName" | "externalReference">;
  className?: string;
}) {
  const t = useTranslation();
  const { friendlyName, externalReference } = transaction;

  return (
    <div className={cn("min-w-0", className)}>
      <p
        className="truncate font-mono text-sm font-medium text-slate-900"
        title={friendlyName}
      >
        {friendlyName}
      </p>
      {externalReference ? (
        <p
          className="truncate font-mono text-[11px] text-slate-400"
          title={`Provider reference: ${externalReference}`}
        >
          {externalReference}
        </p>
      ) : (
        <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
          {t("transactionReference.noProviderReference")}
        </p>
      )}
    </div>
  );
}
