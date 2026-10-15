"use client";

import { useState, useEffect } from "react";
import {
  useRouter,
  useParams,
  useSearchParams,
  usePathname
} from "next/navigation";
import {
  ValidationProblemDetails,
  ProblemDetails,
  ApplicationUser,
  Paged,
  UserResource
} from "@/types";
import { toast } from "sonner";
import Pagination from "@/components/Pagination";
import { UserCombobox } from "@/components/UserCombobox";
import { PageHeader } from "@/components/PageHeader";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import { UserMinus, UserPlus, Eye } from "lucide-react";
import { loginHref } from "@/lib/authRedirect";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";
import { API_BASE } from "@/lib/api";

const OrganizationUsers = () => {
  const t = useTranslation();
  const { id } = useParams();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [selectedUser, setSelectedUser] = useState<ApplicationUser | null>(null);
  const [userSearchTerm, setUserSearchTerm] = useState<string>("");
  const [candidates, setCandidates] = useState<ApplicationUser[]>([]);
  const [candidatesLoading, setCandidatesLoading] = useState<boolean>(false);

  const [users, setUsers] = useState<ApplicationUser[]>([]);
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
  const [addUserModalOpen, setAddUserModalOpen] = useState<boolean>(false);
  const [modalLoading, setModalLoading] = useState<boolean>(false);

  const router = useRouter();


  const handleCreate = () => {
    setAddUserModalOpen(true);
  };

  const handleCloseModal = () => {
    setAddUserModalOpen(false);
    setSelectedUser(null);
    setUserSearchTerm("");
  };

  const fetchOrganizationUsers = async (
    organizationId: string,
    pageNumber: number,
    pageSize: number,
    searchTerm: string
  ) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/users?PageNumber=${pageNumber}&PageSize=${pageSize}&UserName=${searchTerm}`,
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
        setUsers(data.data.map((resource) => resource.user));
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

  const handleAddUser = async (organizationId: string, userId: string) => {
    setModalLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/users/${userId}`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("organizations.successfullyAddedUser"));
        fetchOrganizationUsers(
          organizationId,
          pageNumber,
          pageSize,
          searchTerm
        );
        handleCloseModal();
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
        toast.error(errorData.detail || t("organizations.errorOccurredAddingUser"), {
          id: "error-adding-user"
        });
      }
    } catch {
      toast.error(t("organizations.errorOccurredAddingUser"), {
        id: "error-adding-user"
      });
    } finally {
      setModalLoading(false);
    }
  };

  const fetchUsers = async (searchTerm: string) => {
    setCandidatesLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/users?UserName=${searchTerm}`,
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
        setCandidates(data.data.map((resource) => resource.user));
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
      setCandidatesLoading(false);
    }
  };

  const removeUser = async (organizationId: string, userId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/users/${userId}`,
        {
          method: "DELETE",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("organizations.successfullyRemovedUser"));
        fetchOrganizationUsers(
          organizationId,
          pageNumber,
          pageSize,
          searchTerm
        );
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
        toast.error(errorData.detail || t("organizations.failedDeleteUser"));
      }
    } catch {
      toast.error(t("organizations.failedDeleteUser"));
    }
  };

  useEffect(() => {
    if (addUserModalOpen) fetchUsers(userSearchTerm);
  }, [addUserModalOpen, userSearchTerm]);

  useEffect(() => {
    fetchOrganizationUsers(id as string, pageNumber, pageSize, searchTerm);
  }, [id, pageNumber, pageSize, searchTerm]);

  useEffect(() => {
    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    if (searchTerm.length) {
      params.set("friendlyName", searchTerm);
    }
    router.replace(`?${params.toString()}`);
  }, [pageNumber, pageSize, searchTerm, router]);

  return (
    <div className="space-y-4">
      <PageHeader
        eyebrow={t("organizations.organization")}
        title={t("organizations.organizationUsers")}
        description={t("organizations.whoBelongsToThisOrganization")}
      >
        <Button size="sm" className="gap-1.5" onClick={handleCreate}>
          <UserPlus className="size-4" />
          {t("organizations.addUser")}
        </Button>
      </PageHeader>

      <div className="flex items-center justify-between gap-3">
        <Input
          value={searchTerm}
          onChange={(e) => {
            setSearchTerm(e.target.value);
            setPageNumber(1);
          }}
          placeholder={t("organizations.searchByUsername")}
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
              <TableHead>{t("common.username")}</TableHead>
              <TableHead>{t("common.email")}</TableHead>
              <TableHead>{t("common.roles")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 4 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : users.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.noResults")}
                </TableCell>
              </TableRow>
            ) : (
              users.map((user) => (
                <TableRow key={user.userId} className="hover:bg-slate-50/50">
                  <TableCell className="font-medium text-slate-900">{user.userName}</TableCell>
                  <TableCell className="text-sm text-slate-600">{user.email}</TableCell>
                  <TableCell className="text-sm text-slate-600">{user.roles.join(", ")}</TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="outline"
                        size="sm"
                        className="gap-1.5"
                        onClick={() =>
                          router.push(
                            `/admin/organizations/${id}/users/${user.userId}`
                          )
                        }
                      >
                        <Eye className="size-4" />
                        {t("common.view")}
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-critical-500 hover:text-critical-700"
                        onClick={() => removeUser(id as string, user.userId)}
                      >
                        <UserMinus className="size-4" />
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

      <Dialog open={addUserModalOpen} onOpenChange={setAddUserModalOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("organizations.addUser")}</DialogTitle>
          </DialogHeader>
          <UserCombobox
            users={candidates}
            loading={candidatesLoading}
            value={selectedUser}
            onChange={setSelectedUser}
            onSearchTermChange={setUserSearchTerm}
            onSubmit={() =>
              selectedUser && handleAddUser(id as string, selectedUser.userId)
            }
            disabled={modalLoading}
            emptyLabel={t("organizations.noUsersToAdd")}
          />
          <DialogFooter>
            <Button variant="outline" onClick={handleCloseModal} disabled={modalLoading}>
              {t("common.cancel")}
            </Button>
            <Button
              onClick={() =>
                selectedUser && handleAddUser(id as string, selectedUser.userId)
              }
              disabled={modalLoading || !selectedUser}
              className="max-w-[15rem]"
            >
              <span className="min-w-0 truncate">
                {modalLoading
                  ? "Adding..."
                  : selectedUser
                    ? `Add ${selectedUser.userName}`
                    : "Add"}
              </span>
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default OrganizationUsers;
