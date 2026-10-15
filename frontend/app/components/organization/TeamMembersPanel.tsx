"use client";

import { ReactNode, useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Eye, UserMinus } from "lucide-react";

import { ApplicationUser, Paged, UserResource } from "@/types";
import { API_BASE, reportApiError } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { useAuth } from "@/context/AuthContext";
import { usePermissions } from "@/context/PermissionsContext";
import { useTranslation } from "@/context/LocalizationContext";
import Pagination from "@/components/Pagination";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { HomeUnitCell } from "@/components/subOrganizations/SubOrganizationParts";

import { OffboardMemberDialog } from "./OffboardMemberDialog";

const COLUMNS = 5;

export function TeamMembersPanel({
  organizationId,
  organizationSegment = "my",
  detailHref,
  actions,
  rowActions,
  refreshToken,
  onUnauthorized,
}: {
  organizationId: string;
  organizationSegment?: string;
  detailHref?: (userId: string) => string | null;
  actions?: ReactNode;
  rowActions?: (member: ApplicationUser) => ReactNode;
  refreshToken?: number;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();
  const { user } = useAuth();
  const { holds } = usePermissions();

  const canOffboard = holds("users:offboard");

  const [members, setMembers] = useState<ApplicationUser[]>([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [leaver, setLeaver] = useState<ApplicationUser | null>(null);

  const fetchMembers = useCallback(
    async (page: number, size: number, term: string) => {
      setLoading(true);

      try {
        const params = new URLSearchParams({
          PageNumber: String(page),
          PageSize: String(size),
        });
        if (term.length) params.set("UserName", term);

        const response = await apiFetch(
          `${API_BASE}/organizations/${organizationSegment}/users?${params}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );

        if (response.ok) {
          const listing = (await response.json()) as Paged<UserResource>;
          setMembers(listing.data.map((resource) => resource.user));
          setTotalPages(listing.metadata.pageCount);
        } else {
          await reportApiError(response, {
            fallback: t("common.errorOccurredFetchingUsers"),
            onUnauthorized,
            toastId: "error-fetching-members",
          });
        }
      } catch {
        toast.error(t("common.errorOccurredFetchingUsers"), {
          id: "error-fetching-members",
        });
      } finally {
        setLoading(false);
      }
    },
    [organizationSegment, onUnauthorized, t]
  );

  useEffect(() => {
    void fetchMembers(pageNumber, pageSize, searchTerm);
  }, [fetchMembers, pageNumber, pageSize, searchTerm, refreshToken]);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <Input
          value={searchTerm}
          onChange={(event) => {
            setSearchTerm(event.target.value);
            setPageNumber(1);
          }}
          placeholder={t("users.searchByUsername")}
          className="w-64"
        />
        <div className="flex items-center gap-2">
          <PageSizeSelect
            value={pageSize}
            onChange={(next) => {
              setPageSize(next);
              setPageNumber(1);
            }}
          />
          {actions}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("common.username")}</TableHead>
              <TableHead>{t("common.email")}</TableHead>
              <TableHead>{t("common.roles")}</TableHead>
              <TableHead className="w-44">{t("users.businessUnit")}</TableHead>
              <TableHead className="w-44">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: COLUMNS }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : members.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={COLUMNS}
                  className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {t("common.noResults")}
                </TableCell>
              </TableRow>
            ) : (
              members.map((member) => {
                const isSelf = member.userId === user?.userId;
                const href = detailHref?.(member.userId) ?? null;

                return (
                  <TableRow
                    key={member.userId}
                    className="hover:bg-slate-50/50"
                  >
                    <TableCell className="font-medium text-slate-900">
                      {member.userName}
                    </TableCell>
                    <TableCell className="text-sm text-slate-600">
                      {member.email}
                    </TableCell>
                    <TableCell className="text-sm text-slate-600">
                      {member.roles.join(", ")}
                    </TableCell>
                    <TableCell>
                      <HomeUnitCell subOrganizationId={member.subOrganizationId} />
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-1">
                        {href && (
                          <Button
                            variant="outline"
                            size="sm"
                            className="gap-1.5"
                            onClick={() => router.push(href)}
                          >
                            <Eye className="size-4" />
                            {t("common.view")}
                          </Button>
                        )}
                        {canOffboard && !isSelf && (
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-critical-500 hover:text-critical-700"
                            title={t("offboard.action")}
                            onClick={() => setLeaver(member)}
                          >
                            <UserMinus className="size-4" />
                          </Button>
                        )}
                        {rowActions?.(member)}
                      </div>
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {members.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      <OffboardMemberDialog
        organizationId={organizationId}
        organizationSegment={organizationSegment}
        member={leaver}
        onClose={() => setLeaver(null)}
        onOffboarded={() =>
          void fetchMembers(pageNumber, pageSize, searchTerm)
        }
        onUnauthorized={onUnauthorized}
      />
    </div>
  );
}
