"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { Eye } from "lucide-react";

import { AlertResource } from "@/types";
import { useParties } from "@/lib/useParties";
import { relationText, shortId } from "@/lib/resources";
import {
  AlertStatusBadge,
  AlertSubject,
  RaisedAt,
  SuppressionChip,
} from "@/components/alerts/AlertParts";
import { SeverityBadge } from "@/components/SeverityBadge";
import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Skeleton } from "@/components/ui/skeleton";
import { useTranslation } from "@/context/LocalizationContext";

export function AlertTable({
  alerts,
  loading,
  showCase = true,
  emptyMessage = "No alerts found",
}: {
  alerts: AlertResource[];
  loading: boolean;
  showCase?: boolean;
  emptyMessage?: React.ReactNode;
}) {
  const t = useTranslation();
  const router = useRouter();

  const parties = useParties(alerts.map(({ alert }) => alert.partyId));

  const columnCount = showCase ? 9 : 8;

  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
      <Table>
        <TableHeader>
          <TableRow className="bg-slate-50 hover:bg-slate-50">
            <TableHead>{t("alerts.alert2")}</TableHead>
            <TableHead className="w-28">{t("common.severity")}</TableHead>
            <TableHead className="w-28">{t("common.status")}</TableHead>
            <TableHead>{t("common.rule")}</TableHead>
            <TableHead>{t("alerts.subject")}</TableHead>
            <TableHead>{t("alerts.transaction")}</TableHead>
            <TableHead>{t("alerts.raised")}</TableHead>
            {showCase && <TableHead className="w-28">{t("common.case")}</TableHead>}
            <TableHead className="w-24">{t("common.actions")}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {loading ? (
            Array.from({ length: 5 }).map((_, i) => (
              <TableRow key={i}>
                {Array.from({ length: columnCount }).map((_, j) => (
                  <TableCell key={j}>
                    <Skeleton className="h-4 w-full" />
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : alerts.length === 0 ? (
            <TableRow>
              <TableCell
                colSpan={columnCount}
                className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
              >
                {emptyMessage}
              </TableCell>
            </TableRow>
          ) : (
            alerts.map(({ alert, rule, transaction }) => (
              <TableRow key={alert.alertId} className="hover:bg-slate-50/50">
                <TableCell className="font-medium text-slate-900">
                  <Link
                    href={`/alerts/${alert.alertId}`}
                    className="hover:text-brand-800"
                  >
                    {alert.friendlyName}
                  </Link>
                  <SuppressionChip alert={alert} />
                </TableCell>
                <TableCell>
                  <SeverityBadge severity={alert.severity} emphasis />
                </TableCell>
                <TableCell>
                  <AlertStatusBadge status={alert.status} />
                </TableCell>
                <TableCell className="max-w-[14rem]">
                  {rule?.isDeleted ? (
                    <span className="truncate text-sm text-slate-400">
                      {relationText(rule.friendlyName, alert.ruleId)} (Deleted)
                    </span>
                  ) : (
                    <Link
                      href={`/rules/${alert.ruleId}`}
                      className="block truncate text-sm text-brand-800 hover:underline"
                    >
                      {relationText(rule?.friendlyName, alert.ruleId)}
                    </Link>
                  )}
                </TableCell>
                <TableCell className="max-w-[12rem]">
                  <AlertSubject
                    partyId={alert.partyId}
                    partyRole={alert.partyRole}
                    party={
                      alert.partyId
                        ? parties.get(alert.partyId)
                        : undefined
                    }
                  />
                </TableCell>
                <TableCell>
                  <Link
                    href={`/transactions/${alert.transactionId}`}
                    className="font-mono text-xs text-brand-800 hover:underline"
                  >
                    {transaction?.friendlyName ??
                      shortId(alert.transactionId)}
                  </Link>
                </TableCell>
                <TableCell>
                  <RaisedAt alert={alert} />
                </TableCell>
                {showCase && (
                  <TableCell>
                    {alert.caseId ? (
                      <Link
                        href={`/cases/${alert.caseId}`}
                        className="font-mono text-xs text-brand-800 hover:underline"
                      >
                        {shortId(alert.caseId)}
                      </Link>
                    ) : (
                      <span className="font-mono text-xs text-slate-400">
                        none
                      </span>
                    )}
                  </TableCell>
                )}
                <TableCell>
                  <Button
                    variant="outline"
                    size="sm"
                    className="gap-1.5"
                    onClick={() => router.push(`/alerts/${alert.alertId}`)}
                  >
                    <Eye className="size-4" />
                    {t("common.view")}
                  </Button>
                </TableCell>
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </div>
  );
}
