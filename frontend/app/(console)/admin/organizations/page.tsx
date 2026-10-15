"use client";

import { useState, useEffect } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { toast } from "sonner";
import { Plus, Trash2, Eye } from "lucide-react";

import {
  Organization,
  ListOrganization,
  ProblemDetails,
  ValidationProblemDetails
} from "@/types";
import Pagination from "@/components/Pagination";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
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
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, unwrap } from "@/lib/api";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const Organizations = () => {
  const t = useTranslation();
  const { allowed } = useRequirePermission("platform:administer", "/");

  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const [organizations, setOrganizations] = useState<Organization[]>([]);
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

  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
  const [newFriendlyName, setNewFriendlyName] = useState<string>("");

  const fetchOrganizations = async (
    pageNumber: number,
    pageSize: number,
    friendlyName: string
  ) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations?PageNumber=${pageNumber}&PageSize=${pageSize}&friendlyName=${friendlyName}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = (await response.json()) as ListOrganization;
        setOrganizations(data.data);
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
          errorData.detail || t("organizations.errorOccurredFetchingOrganizations"),
          { id: "error-fetching" }
        );
      }
    } catch {
      toast.error(t("organizations.errorOccurredFetchingOrganizations"), {
        id: "error-fetching"
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    setIsModalOpen(true);
    setNewFriendlyName("");
  };

  const handleCreateOrganization = async () => {
    try {
      const response = await apiFetch(`${API_BASE}/organizations`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          friendlyName: newFriendlyName
        }),
        credentials: "include"
      });

      if (response.ok) {
        const data = await unwrap<Organization>(response);
        toast.success(t("organizations.successfullyCreatedOrganization"));

        setIsModalOpen(false);
        router.push(`organizations/${data.organizationId}#Billing`);
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
        toast.error(errorData.detail || t("organizations.failedCreateOrganization"));
      }
    } catch {
      toast.error(t("organizations.failedCreateOrganization"));
    }
  };

  const deleteOrganization = async (organizationId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}`,
        {
          method: "DELETE",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("organizations.successfullyDeletedOrganization"), {
          action: { label: t("common.undo"), onClick: () => recoverOrganization(organizationId) },
        });
        fetchOrganizations(pageNumber, pageSize, searchTerm);
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
        toast.error(errorData.detail || t("organizations.failedDeleteOrganization"));
      }
    } catch {
      toast.error(t("organizations.failedDeleteOrganization"));
    }
  };

  const recoverOrganization = async (organizationId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/recover`,
        {
          method: "POST",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("organizations.successfullyRecoveredOrganization"));
        fetchOrganizations(pageNumber, pageSize, searchTerm);
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
        toast.error(errorData.detail || t("organizations.failedRecoverOrganization"));
      }
    } catch {
      toast.error(t("organizations.failedRecoverOrganization"));
    }
  };

  useEffect(() => {
    if (!allowed) return;
    fetchOrganizations(pageNumber, pageSize, searchTerm);
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
        eyebrow={t("organizations.admin")}
        title={t("organizations.organizations")}
        description={t("organizations.everyOrganizationOnThePlatform")}
      >
        <Button size="sm" className="gap-1.5" onClick={handleCreate}>
          <Plus className="size-4" />
          {t("organizations.newOrganization")}
        </Button>
      </PageHeader>

      <div className="flex items-center justify-between gap-3">
        <Input
          value={searchTerm}
          onChange={(e) => {
            setSearchTerm(e.target.value);
            setPageNumber(1);
          }}
          placeholder={t("common.searchByName")}
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
              <TableHead>{t("common.name")}</TableHead>
              <TableHead>{t("common.status")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 3 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : organizations.length === 0 ? (
              <TableRow>
                <TableCell colSpan={3} className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.noResults")}
                </TableCell>
              </TableRow>
            ) : (
              organizations.map((org) => (
                <TableRow key={org.organizationId} className="hover:bg-slate-50/50">
                  <TableCell className="font-medium text-slate-900">{org.friendlyName}</TableCell>
                  <TableCell><StatusBadge status={org.status} /></TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button variant="outline" size="sm" className="gap-1.5" onClick={() => router.push(`/admin/organizations/${org.organizationId}`)}>
                        <Eye className="size-4" />
                        {t("common.view")}
                      </Button>
                      <Button variant="ghost" size="icon" className="h-8 w-8 text-critical-500 hover:text-critical-700" onClick={() => deleteOrganization(org.organizationId)}>
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

      {organizations.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
        />
      )}

      <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("organizations.createOrganization")}</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <Input
              placeholder={t("organizations.friendlyName2")}
              value={newFriendlyName}
              onChange={(e) => setNewFriendlyName(e.target.value)}
            />
            <p className="text-xs text-slate-500">
              A name is all an organization is created with. What it is charged
              is a plan it holds a subscription to, set on the Billing section
              of the organization — until then it is served, metered and not
              invoiced. You&apos;ll land there next.
            </p>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setIsModalOpen(false)}>{t("common.cancel")}</Button>
            <Button onClick={handleCreateOrganization}>{t("organizations.create")}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default Organizations;
