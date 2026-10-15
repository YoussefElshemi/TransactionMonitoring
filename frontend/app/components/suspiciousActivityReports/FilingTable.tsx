"use client";

import { useRouter } from "next/navigation";
import { Eye } from "lucide-react";

import { SuspiciousActivityReportResource } from "@/types";
import { filingHref } from "@/lib/suspiciousActivityReports";
import { relationText, userName } from "@/lib/resources";
import {
  FilingCaseLink,
  FilingDeadline,
  FilingReference,
  FilingStatusBadge,
  overdueRowClasses,
} from "@/components/suspiciousActivityReports/FilingParts";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useTranslation } from "@/context/LocalizationContext";

export function FilingTable({
  reports,
  loading,
  emptyMessage = "No filings found",
}: {
  reports: SuspiciousActivityReportResource[];
  loading: boolean;
  emptyMessage?: React.ReactNode;
}) {
  const t = useTranslation();
  const router = useRouter();

  const columnCount = 7;

  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
      <Table>
        <TableHeader>
          <TableRow className="bg-slate-50 hover:bg-slate-50">
            <TableHead className="w-40">{t("suspiciousActivityReports.reference")}</TableHead>
            <TableHead className="w-28">{t("common.case")}</TableHead>
            <TableHead className="w-36">{t("common.status")}</TableHead>
            <TableHead className="w-64">{t("suspiciousActivityReports.deadline")}</TableHead>
            <TableHead>{t("suspiciousActivityReports.openedBy")}</TableHead>
            <TableHead>{t("suspiciousActivityReports.filedBy")}</TableHead>
            <TableHead className="w-24">{t("common.actions")}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {loading ? (
            Array.from({ length: 5 }).map((_, row) => (
              <TableRow key={row}>
                {Array.from({ length: columnCount }).map((_, cell) => (
                  <TableCell key={cell}>
                    <Skeleton className="h-4 w-full" />
                  </TableCell>
                ))}
              </TableRow>
            ))
          ) : reports.length === 0 ? (
            <TableRow>
              <TableCell
                colSpan={columnCount}
                className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
              >
                {emptyMessage}
              </TableCell>
            </TableRow>
          ) : (
            reports.map((resource) => {
              const {
                suspiciousActivityReport: report,
                openedByUser,
                filedByUser,
              } = resource;

              return (
                <TableRow
                  key={report.suspiciousActivityReportId}
                  className={overdueRowClasses(report.isOverdue)}
                >
                  <TableCell>
                    <FilingReference report={report} />
                  </TableCell>
                  <TableCell>
                    <FilingCaseLink caseId={report.caseId} />
                  </TableCell>
                  <TableCell>
                    <FilingStatusBadge status={report.status} />
                  </TableCell>
                  <TableCell>
                    <FilingDeadline report={report} />
                  </TableCell>
                  <TableCell className="max-w-[14rem] truncate text-sm text-slate-700">
                    {relationText(
                      userName(openedByUser),
                      report.openedByUserId
                    )}
                  </TableCell>
                  <TableCell className="max-w-[14rem] truncate text-sm text-slate-700">
                    {report.filedByUserId ? (
                      relationText(userName(filedByUser), report.filedByUserId)
                    ) : (
                      <span className="text-sm text-slate-400">
                        {report.status === "NotFiled"
                          ? "Not filed"
                          : "Nobody yet"}
                      </span>
                    )}
                  </TableCell>
                  <TableCell>
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      onClick={() =>
                        router.push(
                          filingHref(report.suspiciousActivityReportId)
                        )
                      }
                    >
                      <Eye className="size-4" />
                      {t("common.view")}
                    </Button>
                  </TableCell>
                </TableRow>
              );
            })
          )}
        </TableBody>
      </Table>
    </div>
  );
}
