"use client";

import { useCallback, useEffect, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { toast } from "sonner";
import { Eye, Plus, Trash2 } from "lucide-react";

import {
  ApiClient,
  ApiClientResource,
  CreatedApiClient,
  CreatedApiClientResource,
  Paged,
} from "@/types";
import { organizationScopedPath, reportApiError, unwrap } from "@/lib/api";
import { useApiScopes } from "@/lib/apiScopes";
import Pagination from "@/components/Pagination";
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
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";

import { ClientCredentialsDialog } from "./ClientCredentialsDialog";
import { CopyableText } from "./CopyableText";
import { ScopeBadges, ScopeSelector } from "./ScopeSelector";
import { loginHref } from "@/lib/authRedirect";

import { apiFetch } from "@/lib/http";
import { useEnvironment } from "@/context/ModeContext";
import { useTranslation } from "@/context/LocalizationContext";

export const apiClientHref = (apiClientId: string): string =>
  `/organization/api-clients/${apiClientId}`;

export function ApiClientsPanel({
  organizationId,
  detailHref = apiClientHref,
}: {
  organizationId?: string;
  detailHref?: (apiClientId: string) => string;
} = {}) {
  const t = useTranslation();
  const { mode } = useEnvironment();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const collectionPath = organizationScopedPath("api-clients", organizationId);
  const canCreate = organizationId === undefined;

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const [apiClients, setApiClients] = useState<ApiClient[]>([]);
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(10);
  const [searchTerm, setSearchTerm] = useState<string>("");

  const [totalPages, setTotalPages] = useState<number>(1);
  const [loading, setLoading] = useState<boolean>(true);

  const [createModalOpen, setCreateModalOpen] = useState<boolean>(false);
  const [createLoading, setCreateLoading] = useState<boolean>(false);
  const [newFriendlyName, setNewFriendlyName] = useState<string>("");
  const [newScopes, setNewScopes] = useState<string[]>([]);

  const [created, setCreated] = useState<CreatedApiClient | null>(null);

  const scopeOptions = useApiScopes();

  const fetchApiClients = useCallback(async (
    pageNumber: number,
    pageSize: number,
    friendlyName: string
  ) => {
    setLoading(true);

    try {
      const params = new URLSearchParams({
        pageNumber: String(pageNumber),
        pageSize: String(pageSize)
      });
      if (friendlyName.length) params.set("friendlyName", friendlyName);

      const response = await apiFetch(`${collectionPath}?${params}`, {
        credentials: "include"
      });

      if (response.ok) {
        const data = (await response.json()) as Paged<ApiClientResource>;
        setApiClients(
          data.data
            .filter((resource) => !resource.apiClient.mode || resource.apiClient.mode.toLowerCase() === mode)
            .map((resource) => resource.apiClient)
        );
        setTotalPages(data.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("apiClients.errorOccurredFetchingApiClients"),
          onUnauthorized: goToLogin,
          toastId: "error-fetching-api-clients"
        });
      }
    } catch {
      toast.error(t("apiClients.errorOccurredFetchingApiClients"), {
        id: "error-fetching-api-clients"
      });
    } finally {
      setLoading(false);
    }
  }, [collectionPath, goToLogin, mode]);

  const handleOpenCreate = () => {
    setNewFriendlyName("");
    setNewScopes([]);
    setCreateModalOpen(true);
  };

  const handleCreateApiClient = async () => {
    setCreateLoading(true);

    try {
      const response = await apiFetch(collectionPath, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          friendlyName: newFriendlyName,
          scopes: newScopes
        }),
        credentials: "include"
      });

      if (response.ok) {
        const data = (await unwrap<CreatedApiClientResource>(response)).apiClient;
        toast.success(t("apiClients.successfullyCreatedApiClient"));
        setCreateModalOpen(false);
        // Hold the redirect until the secret has been acknowledged —
        // navigating away would throw the only copy of it away with the page.
        setCreated(data);
      } else {
        await reportApiError(response, {
          fallback: t("apiClients.failedCreateApiClient"),
          onUnauthorized: goToLogin
        });
      }
    } catch {
      toast.error(t("apiClients.failedCreateApiClient"));
    } finally {
      setCreateLoading(false);
    }
  };

  const deleteApiClient = async (apiClient: ApiClient) => {
    try {
      const response = await apiFetch(
        `${collectionPath}/${apiClient.apiClientId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("apiClients.deletedNamed", apiClient.friendlyName), {
          action: {
            label: t("common.undo"),
            onClick: () => recoverApiClient(apiClient.apiClientId)
          }
        });
        fetchApiClients(pageNumber, pageSize, searchTerm);
      } else {
        await reportApiError(response, {
          fallback: t("apiClients.failedDeleteApiClient"),
          onUnauthorized: goToLogin
        });
      }
    } catch {
      toast.error(t("apiClients.failedDeleteApiClient"));
    }
  };

  const recoverApiClient = async (apiClientId: string) => {
    try {
      const response = await apiFetch(
        `${collectionPath}/${apiClientId}/recover`,
        { method: "POST", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("apiClients.successfullyRecoveredApiClient"));
        fetchApiClients(pageNumber, pageSize, searchTerm);
      } else {
        await reportApiError(response, {
          fallback: t("apiClients.failedRecoverApiClient"),
          onUnauthorized: goToLogin
        });
      }
    } catch {
      toast.error(t("apiClients.failedRecoverApiClient"));
    }
  };

  useEffect(() => {
    fetchApiClients(pageNumber, pageSize, searchTerm);
  }, [fetchApiClients, pageNumber, pageSize, searchTerm]);

  return (
    <div className="space-y-4">
      <div className={mode === "test" ? "rounded-md border border-attention-300 bg-attention-50 px-3 py-2 text-sm text-attention-900" : "rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600"}>
        {mode === "test" ? "Test API keys · Sandbox only · never use in production" : "Live API keys · production environment"}
      </div>
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
        <div className="flex items-center gap-3">
          <PageSizeSelect
            value={pageSize}
            onChange={(next) => {
              setPageSize(next);
              setPageNumber(1);
            }}
          />
          {canCreate && (
            <Button size="sm" className="gap-1.5" onClick={handleOpenCreate}>
              <Plus className="size-4" />
              {t("apiClients.newApiClient")}
            </Button>
          )}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("common.name")}</TableHead>
              <TableHead>{t("apiClients.clientId")}</TableHead>
              <TableHead>{t("apiClients.scopes")}</TableHead>
              <TableHead>{t("common.status")}</TableHead>
              <TableHead className="w-36">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 5 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : apiClients.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={5}
                  className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {t("common.noResults")}
                </TableCell>
              </TableRow>
            ) : (
              apiClients.map((apiClient) => (
                <TableRow
                  key={apiClient.apiClientId}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell
                    className={
                      apiClient.isEnabled
                        ? "font-medium text-slate-900"
                        : "font-medium text-slate-400"
                    }
                  >
                    {apiClient.friendlyName}
                  </TableCell>
                  <TableCell>
                    <CopyableText value={apiClient.clientId} />
                  </TableCell>
                  <TableCell>
                    <ScopeBadges scopes={apiClient.scopes} />
                  </TableCell>
                  <TableCell>
                    <StatusBadge
                      status={apiClient.isEnabled ? "Enabled" : "Disabled"}
                    />
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="outline"
                        size="sm"
                        className="gap-1.5"
                        onClick={() =>
                          router.push(detailHref(apiClient.apiClientId))
                        }
                      >
                        <Eye className="size-4" />
                        {t("common.view")}
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-critical-500 hover:text-critical-700"
                        onClick={() => deleteApiClient(apiClient)}
                        title={t("apiClients.deleteThisClient")}
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

      {apiClients.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((p) => Math.min(totalPages, p + 1))
          }
        />
      )}

      <Dialog open={createModalOpen} onOpenChange={setCreateModalOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("apiClients.createApiClient")}</DialogTitle>
            <DialogDescription>
              The client secret is shown once, on the screen straight after
              this one.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.name")}
              </label>
              <Input
                placeholder={t("apiClients.eGTransactionsService")}
                value={newFriendlyName}
                onChange={(e) => setNewFriendlyName(e.target.value)}
                disabled={createLoading}
              />
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("apiClients.scopes")}
              </label>
              <ScopeSelector
                options={scopeOptions}
                selected={newScopes}
                onChange={setNewScopes}
                disabled={createLoading}
              />
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setCreateModalOpen(false)}
              disabled={createLoading}
            >
              {t("common.cancel")}
            </Button>
            <Button
              onClick={handleCreateApiClient}
              disabled={
                createLoading ||
                newFriendlyName.trim() === "" ||
                newScopes.length === 0
              }
            >
              {createLoading ? "Creating..." : "Create"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ClientCredentialsDialog
        clientId={created?.clientId ?? ""}
        clientSecret={created?.clientSecret ?? null}
        title={mode === "test" ? t("apiClients.yourNewTestClientCredentials") : t("apiClients.yourNewLiveClientCredentials")}
        description={mode === "test" ? t("apiClients.theseCredentialsAreIsolatedSandbox") : t("apiClients.giveTheseLiveCredentialsService")}
        onAcknowledge={() => {
          const client = created;
          setCreated(null);
          if (client) router.push(detailHref(client.apiClientId));
        }}
      />
    </div>
  );
}
