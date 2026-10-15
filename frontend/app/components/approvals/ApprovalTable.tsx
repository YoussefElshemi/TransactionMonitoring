"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { Eye } from "lucide-react";

import { ApprovalResource } from "@/types";
import { approvalHref } from "@/lib/approvals";
import { shortId } from "@/lib/resources";
import {
  ApprovalAge,
  ApprovalProposalSummary,
  ApprovalStatusBadge,
  ApprovalSubjectChip,
  ApprovalTargetLink,
  RequestedBy,
} from "@/components/approvals/ApprovalParts";
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

export function ApprovalTable({
  approvals,
  loading,
  showRequester = true,
  emptyMessage = "No requests found",
}: {
  approvals: ApprovalResource[];
  loading: boolean;
  showRequester?: boolean;
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
            <TableHead>{t("approvals.about")}</TableHead>
            <TableHead className="w-32">{t("common.status")}</TableHead>
            <TableHead>{t("approvals.proposed")}</TableHead>
            <TableHead className="w-28">{t("approvals.subjectOf")}</TableHead>
            <TableHead>{showRequester ? "Raised by" : "Answered by"}</TableHead>
            <TableHead className="w-32">{t("approvals.waiting")}</TableHead>
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
          ) : approvals.length === 0 ? (
            <TableRow>
              <TableCell
                colSpan={columnCount}
                className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
              >
                {emptyMessage}
              </TableCell>
            </TableRow>
          ) : (
            approvals.map((resource) => {
              const { approval, reviewedByUser } = resource;
              return (
                <TableRow
                  key={approval.approvalId}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell>
                    <Link
                      href={approvalHref(approval.approvalId)}
                      className="hover:text-brand-800"
                    >
                      <ApprovalSubjectChip subject={approval.subject} />
                    </Link>
                  </TableCell>
                  <TableCell>
                    <ApprovalStatusBadge status={approval.status} />
                  </TableCell>
                  <TableCell>
                    <ApprovalProposalSummary resource={resource} />
                  </TableCell>
                  <TableCell>
                    <ApprovalTargetLink approval={approval} />
                  </TableCell>
                  <TableCell className="max-w-[14rem]">
                    {showRequester ? (
                      <RequestedBy resource={resource} />
                    ) : approval.reviewedByUserId ? (
                      <span className="truncate text-sm text-slate-700">
                        {reviewedByUser?.email ??
                          reviewedByUser?.userName ??
                          shortId(approval.reviewedByUserId)}
                      </span>
                    ) : (
                      <span className="text-sm text-slate-400">
                        {t("approvals.nobodyYet")}
                      </span>
                    )}
                  </TableCell>
                  <TableCell>
                    <ApprovalAge approval={approval} />
                  </TableCell>
                  <TableCell>
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      onClick={() =>
                        router.push(approvalHref(approval.approvalId))
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
