import { ConditionMatch, RuleVersion } from "@/types";
import {
  ConditionRow,
  conditionRows,
  describeCondition,
  describeQuantifier,
  formatJsonValue,
} from "@/lib/conditions";
import { StatusBadge } from "@/components/StatusBadge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function ConditionTable({
  conditionMatches,
  ruleVersion,
  payload,
}: {
  conditionMatches: ConditionMatch[];
  ruleVersion: RuleVersion;
  payload: Record<string, unknown> | null | undefined;
}) {
  const t = useTranslation();
  const rows = conditionRows(
    conditionMatches,
    ruleVersion.ruleBody,
    ruleVersion.matchPolarity,
    payload
  );

  const unaligned = rows.some((row) => row.negated === null);

  return (
    <div className="space-y-2">
      <div className="overflow-hidden rounded-lg border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("evidence.field")}</TableHead>
              <TableHead>{unaligned ? "Condition" : "Required"}</TableHead>
              <TableHead>{t("evidence.payloadValue")}</TableHead>
              <TableHead className="w-36">{t("evidence.contributed")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.map((row, index) => (
              <ConditionRowCells key={`${row.field}-${index}`} row={row} />
            ))}
          </TableBody>
        </Table>
      </div>

      {unaligned && (
        <p className="text-xs text-slate-500">
          This trace has more or fewer conditions than the version it is shown
          against, so the rule&apos;s structure could not be matched to it. The
          comparisons are listed as recorded, without saying which way round
          each one reads.
        </p>
      )}
    </div>
  );
}

function ConditionRowCells({ row }: { row: ConditionRow }) {
  const t = useTranslation();
  const requirement = describeCondition(row);
  const quantifierNote = describeQuantifier(row);

  return (
    <TableRow className="hover:bg-slate-50/50 align-top">
      <TableCell
        className={cn(
          "font-mono text-xs",
          row.matched ? "text-slate-900" : "text-slate-400"
        )}
      >
        {row.field}
        {row.isExpression && (
          <span className="mt-0.5 block font-sans text-[11px] text-slate-400">
            computed
          </span>
        )}
        {row.isParty && (
          <span className="mt-0.5 block font-sans text-[11px] text-slate-400">
            about a party, not the payload
          </span>
        )}
        {row.isVelocity && (
          <span className="mt-0.5 block font-sans text-[11px] text-slate-400">
            aggregated over a window, not read from the payload
          </span>
        )}
      </TableCell>

      <TableCell
        className={cn("text-sm", row.matched ? "text-slate-700" : "text-slate-400")}
      >
        {requirement ?? (
          <span className="font-mono text-xs">
            {row.quantifier ? `${row.quantifier} ` : ""}
            {row.condition ?? "—"} {formatJsonValue(row.expected)}
          </span>
        )}
      </TableCell>

      <TableCell className="font-mono text-xs">
        {row.weighed ? (
          <WeighedValues row={row} />
        ) : row.isExpression && row.actual === null ? (
          <span className="font-sans text-slate-400">
            computed nothing, so this could not match
          </span>
        ) : row.isVelocity && row.actual === null ? (
          <span className="font-sans text-slate-400">
            nothing — the window held no observations, and this aggregate
            answers nothing rather than zero over an empty one
          </span>
        ) : row.isParty && row.actual === null ? (
          <span className="font-sans text-slate-400">
            nothing — either no party stood in that role, or the fact is unset
            for them
          </span>
        ) : row.actual === null && !row.present ? (
          <span className="font-sans text-slate-400">not in the payload</span>
        ) : (
          <span className={row.matched ? "text-slate-900" : "text-slate-400"}>
            {formatJsonValue(row.actual)}
          </span>
        )}
        {quantifierNote && (
          <span className="mt-1 block font-sans text-[11px] leading-snug text-slate-400">
            {quantifierNote}
          </span>
        )}
      </TableCell>

      <TableCell>
        <StatusBadge
          status={row.matched ? "Pending" : "Disabled"}
          label={t(row.matched ? "evidence.contributed" : "evidence.didNot")}
        />
      </TableCell>
    </TableRow>
  );
}

function WeighedValues({ row }: { row: ConditionRow }) {
  const values = row.weighed ?? [];

  if (values.length === 0) {
    return (
      <span className="font-sans text-slate-400">
        {row.isParty || row.isVelocity
          ? "nothing — no party stood in that role on this payment"
          : "nothing — the path resolved to an empty list"}
      </span>
    );
  }

  return (
    <ul className="space-y-0.5">
      {values.map((value, index) => (
        <li key={index} className="flex items-baseline gap-2">
          <span className="shrink-0 text-[10px] text-slate-300 tabular-nums">
            [{index}]
          </span>
          <span className={row.matched ? "text-slate-900" : "text-slate-400"}>
            {row.isParty || row.isVelocity
              ? describeWeighedParty(value)
              : formatJsonValue(value)}
          </span>
        </li>
      ))}
    </ul>
  );
}

const describeWeighedParty = (value: unknown): string => {
  if (value === null || typeof value !== "object" || Array.isArray(value)) {
    return formatJsonValue(value);
  }

  const record = value as Record<string, unknown>;
  const pick = (...keys: string[]): unknown => {
    for (const key of keys) {
      if (record[key] !== undefined && record[key] !== null) return record[key];
    }
    return undefined;
  };

  const identifier = pick("identifier", "Identifier");
  const name = pick("displayName", "DisplayName");
  const fact = pick("value", "Value", "riskScore", "RiskScore");

  if (identifier === undefined) return formatJsonValue(value);

  const who = name ? `${String(name)} (${String(identifier)})` : String(identifier);
  return fact === undefined ? who : `${who} — ${formatJsonValue(fact)}`;
};
