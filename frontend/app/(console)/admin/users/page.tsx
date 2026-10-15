"use client";

import { useState, useEffect } from "react";
import { useRouter, useSearchParams, usePathname } from "next/navigation";
import {
  ValidationProblemDetails,
  ProblemDetails,
  Paged,
  UserResource
} from "@/types";
import { toast } from "sonner";
import Pagination from "@/components/Pagination";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { PageHeader } from "@/components/PageHeader";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import { Eye, Trash2 } from "lucide-react";
import { loginHref } from "@/lib/authRedirect";
import { relationText } from "@/lib/resources";
import { HomeUnitCell } from "@/components/subOrganizations/SubOrganizationParts";
import { PageSizeSelect } from "@/components/PageSizeSelect";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";
import { API_BASE } from "@/lib/api";

const OrganizationUsers = () => {
  const t = useTranslation();
  const { allowed } = useRequirePermission("platform:administer", "/");

  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [users, setUsers] = useState<UserResource[]>([]);
  const [pageNumber, setPageNumber] = useState<number>(
    parseInt(searchParams.get("pageNumber") || "1")
  );
  const [pageSize, setPageSize] = useState<number>(
    parseInt(searchParams.get("pageSize") || "10")
  );
  const [searchTerm, setSearchTerm] = useState<string>(
    searchParams.get("friendlyName") || ""
  );
  const [totalPages, setTotalPages] = useState<number>(1);
  const [loading, setLoading] = useState<boolean>(true);

  const router = useRouter();


  const fetchUsers = async (
    pageNumber: number,
    pageSize: number,
    searchTerm: string
  ) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/users?PageNumber=${pageNumber}&PageSize=${pageSize}&UserName=${searchTerm}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = (await response.json()) as Paged<UserResource>;
        setUsers(data.data);
        setTotalPages(data.metadata.pageCount);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginAccessResource"), {
          id: "unauthorized"
        });
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission"),
          {
            id: "forbidden"
          }
        );
        return router.push("/");
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("common.errorOccurredFetchingUsers"),
          { id: "error-fetching-users" }
        );
      }
    } catch {
      toast.error(t("common.errorOccurredFetchingUsers"), {
        id: "error-fetching-users"
      });
    } finally {
      setLoading(false);
    }
  };

  const deleteUser = async (userId: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(`${API_BASE}/users/${userId}`, {
        method: "DELETE",
        headers: {
          "Content-Type": "application/json"
        },
        credentials: "include"
      });

      if (response.ok) {
        toast.success(t("users.successfullyDeletedUser"), {
          action: { label: t("common.undo"), onClick: () => recoverUser(userId) },
        });
        fetchUsers(pageNumber, pageSize, searchTerm);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("users.errorOccurredDeletingUser"),
          { id: "error-deleting" }
        );
      }
    } catch {
      toast.error(t("users.errorOccurredDeletingUser"), {
        id: "error-deleting"
      });
    } finally {
      setLoading(false);
    }
  };

  const recoverUser = async (userId: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/users/${userId}/recover`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("users.successfullyRecoveredUser"));
        fetchUsers(pageNumber, pageSize, searchTerm);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("users.errorOccurredDeletingUser"),
          { id: "error-deleting" }
        );
      }
    } catch {
      toast.error(t("users.errorOccurredDeletingUser"), {
        id: "error-deleting"
      });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!allowed) return;
    fetchUsers(pageNumber, pageSize, searchTerm);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [allowed, pageNumber, pageSize, searchTerm]);

  useEffect(() => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    if (searchTerm.length) {
      params.set("friendlyName", searchTerm);
    }
    router.replace(`?${params.toString()}`);
  }, [pageNumber, pageSize, searchTerm, router]);

  if (!allowed) return null;

  return (
    <div className="space-y-4">
      <PageHeader
        eyebrow={t("users.admin")}
        title={t("users.users")}
        description={t("users.everyoneWithAnAccountAcross")}
      />

      <div className="flex items-center justify-between gap-3">
        <Input
          value={searchTerm}
          onChange={(e) => {
            setSearchTerm(e.target.value);
            setPageNumber(1);
          }}
          placeholder={t("users.searchByUsername")}
          className="w-64"
        />
        <PageSizeSelect
          value={pageSize}
          onChange={(next) => {
            setPageSize(next);
            setPageNumber(1);
          }}
        />
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("common.email")}</TableHead>
              <TableHead>{t("common.username")}</TableHead>
              <TableHead>{t("common.roles")}</TableHead>
              <TableHead>{t("common.organization")}</TableHead>
              <TableHead className="w-44">{t("users.businessUnit")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 6 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.noResults")}
                </TableCell>
              </TableRow>
            ) : (
              users.map(({ user, organization }) => (
                <TableRow key={user.userId} className="hover:bg-slate-50/50">
                  <TableCell className="text-sm text-slate-600">{user.email}</TableCell>
                  <TableCell className="font-medium text-slate-900">{user.userName}</TableCell>
                  <TableCell className="text-sm text-slate-600">{user.roles.join(", ")}</TableCell>
                  <TableCell className="text-sm text-slate-600">
                    {user.organizationId ? (
                      relationText(organization?.friendlyName, user.organizationId)
                    ) : (
                      <span className="text-slate-400">—</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <HomeUnitCell subOrganizationId={user.subOrganizationId} />
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="outline"
                        size="sm"
                        className="gap-1.5"
                        onClick={() => router.push(`/admin/users/${user.userId}`)}
                      >
                        <Eye className="size-4" />
                        {t("common.view")}
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-critical-500 hover:text-critical-700"
                        onClick={() => deleteUser(user.userId)}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {users.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
        />
      )}
    </div>
  );
};

export default OrganizationUsers;
