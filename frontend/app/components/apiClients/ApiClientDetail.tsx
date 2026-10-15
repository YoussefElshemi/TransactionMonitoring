"use client";

import { useCallback, useEffect, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Ban, CircleCheck, Save, Trash2, TriangleAlert } from "lucide-react";
import { toast } from "sonner";

import { ApiClientResource } from "@/types";
import { organizationScopedPath, reportApiError, unwrap } from "@/lib/api";
import { relationText, userName } from "@/lib/resources";
import { useApiScopes } from "@/lib/apiScopes";
import { usePermissions } from "@/context/PermissionsContext";
import { useEnvironment } from "@/context/ModeContext";
import { HistoryPanel, historyPath } from "@/components/history";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import { TabBar } from "@/components/TabBar";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";

import { ApiClientSecretsPanel } from "./ApiClientSecretsPanel";
import { CopyableText } from "./CopyableText";
import { ScopeSelector } from "./ScopeSelector";
import { loginHref } from "@/lib/authRedirect";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const TABS = ["Details", "Secrets", "History"];

const sameScopes = (left: string[], right: string[]): boolean => {
  if (left.length !== right.length) return false;

  const sortedRight = [...right].sort();
  return [...left].sort().every((scope, index) => scope === sortedRight[index]);
};

export function ApiClientDetail({
  apiClientId,
  organizationId,
  listHref = "/organization#API%20Clients",
  detailHref = (id: string) => `/organization/api-clients/${id}`,
}: {
  apiClientId: string;
  organizationId?: string;
  listHref?: string;
  detailHref?: (apiClientId: string) => string;
}) {
  const t = useTranslation();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { holds } = usePermissions();
  const { mode } = useEnvironment();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const [resource, setResource] = useState<ApiClientResource | null>(null);
  const apiClient = resource?.apiClient ?? null;
  const [loading, setLoading] = useState<boolean>(true);
  const [activeTab, setActiveTab] = useState<string>("Details");

  const [friendlyName, setFriendlyName] = useState<string>("");
  const [scopes, setScopes] = useState<string[]>([]);
  const [saveLoading, setSaveLoading] = useState<boolean>(false);

  const [disableConfirmOpen, setDisableConfirmOpen] = useState<boolean>(false);
  const [enabledLoading, setEnabledLoading] = useState<boolean>(false);

  const canManage = holds("api-clients:write");
  const clientPath = `${organizationScopedPath(
    "api-clients",
    organizationId
  )}/${apiClientId}`;
  const scopeOptions = useApiScopes(apiClient?.scopes ?? []);

  const fetchApiClient = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(clientPath, {
        credentials: "include"
      });

      if (response.ok) {
        const data = await unwrap<ApiClientResource>(response);
        setResource(data);
        setFriendlyName(data.apiClient.friendlyName);
        setScopes(data.apiClient.scopes);
      } else if (response.status !== 404) {
        await reportApiError(response, {
          fallback: t("apiClients.errorOccurredFetchingApiClient"),
          onUnauthorized: goToLogin,
          toastId: "error-fetching-api-client"
        });
      }
    } catch {
      toast.error(t("apiClients.errorOccurredFetchingApiClient"), {
        id: "error-fetching-api-client"
      });
    } finally {
      setLoading(false);
    }
  }, [apiClientId, clientPath, goToLogin]);

  useEffect(() => {
    fetchApiClient();
  }, [fetchApiClient]);

  const handleSave = async () => {
    if (!apiClient) return;

    const payload: {
      friendlyName?: string;
      scopes?: string[];
    } = {};

    if (friendlyName !== apiClient.friendlyName) {
      payload.friendlyName = friendlyName;
    }
    if (!sameScopes(scopes, apiClient.scopes)) {
      payload.scopes = scopes;
    }

    if (Object.keys(payload).length === 0) return;

    setSaveLoading(true);

    try {
      const response = await apiFetch(clientPath, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(payload)
      });

      if (response.ok) {
        toast.success(t("apiClients.successfullyUpdatedApiClient"));
        await fetchApiClient();
      } else {
        await reportApiError(response, {
          fallback: t("common.errorOccurredSavingChanges"),
          onUnauthorized: goToLogin
        });
      }
    } catch {
      toast.error(t("common.errorOccurredSavingChanges"));
    } finally {
      setSaveLoading(false);
    }
  };

  const setEnabled = async (isEnabled: boolean) => {
    setEnabledLoading(true);

    try {
      const response = await apiFetch(clientPath, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ isEnabled })
      });

      if (response.ok) {
        setDisableConfirmOpen(false);
        toast.success(
          isEnabled ? t("apiClients.aPIClientEnabled") : t("apiClients.aPIClientDisabled")
        );
        await fetchApiClient();
      } else {
        await reportApiError(response, {
          fallback: isEnabled
            ? t("apiClients.failedEnableApiClient")
            : t("apiClients.failedDisableApiClient"),
          onUnauthorized: goToLogin
        });
      }
    } catch {
      toast.error(t("common.errorOccurredSavingChanges"));
    } finally {
      setEnabledLoading(false);
    }
  };

  const deleteApiClient = async () => {
    try {
      const response = await apiFetch(clientPath, {
        method: "DELETE",
        credentials: "include"
      });

      if (response.ok) {
        toast.success(t("apiClients.successfullyDeletedApiClient"), {
          action: { label: t("common.undo"), onClick: () => recoverApiClient() }
        });
        router.push(listHref);
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

  const recoverApiClient = async () => {
    try {
      const response = await apiFetch(
        `${clientPath}/recover`,
        { method: "POST", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("apiClients.successfullyRecoveredApiClient"));
        router.push(detailHref(apiClientId));
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

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Card>
          <CardContent className="space-y-3 pt-6">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-4 w-full" />
            ))}
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!apiClient) {
    return (
      <NotFound
        title={t("apiClients.apiClientNotFound")}
        description={t("common.notFound.description")}
        backHref={listHref}
        backLabel={t("apiClients.allApiClients")}
      />
    );
  }

  const dirty =
    friendlyName !== apiClient.friendlyName ||
    !sameScopes(scopes, apiClient.scopes);

  return (
    <div className="space-y-6">
      <div className={mode === "test" ? "rounded-md border border-attention-300 bg-attention-50 px-3 py-2 text-sm text-attention-900" : "rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-600"}>{mode === "test" ? "Test API client · Sandbox only" : "Live API client · production environment"}</div>
      <PageHeader eyebrow={t("apiClients.apiClient")} title={apiClient.friendlyName}>
        {canManage && (
          <>
            {apiClient.isEnabled ? (
              <Button
                variant="outline"
                size="sm"
                className="gap-1.5"
                onClick={() => setDisableConfirmOpen(true)}
                disabled={enabledLoading}
              >
                <Ban className="size-4" />
                {t("apiClients.disable")}
              </Button>
            ) : (
              <Button
                variant="outline"
                size="sm"
                className="gap-1.5"
                onClick={() => setEnabled(true)}
                disabled={enabledLoading}
              >
                <CircleCheck className="size-4" />
                {t("apiClients.enable")}
              </Button>
            )}
            <Button
              size="sm"
              className="gap-1.5"
              onClick={handleSave}
              disabled={saveLoading || !dirty}
            >
              <Save className="size-4" />
              {saveLoading ? "Saving..." : "Save"}
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-critical-500 hover:text-critical-700"
              onClick={deleteApiClient}
              title={t("apiClients.deleteThisClient")}
              aria-label={t("apiClients.deleteThisClient")}
            >
              <Trash2 className="size-4" />
            </Button>
          </>
        )}
      </PageHeader>

      {!apiClient.isEnabled && (
        <div className="flex items-start gap-2.5 rounded-lg border border-attention-200 bg-attention-50 px-3 py-2.5">
          <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
          <p className="text-sm text-attention-700">
            This client is disabled. Its credentials are refused at the token
            endpoint and any token already issued to it stops working — the
            client and its secrets are otherwise untouched.
          </p>
        </div>
      )}

      <TabBar tabs={TABS} activeTab={activeTab} onChange={setActiveTab} />

      {activeTab === "Details" && (
        <Card>
          <CardContent className="space-y-6 pt-6">
            <div className="grid gap-x-8 gap-y-4 sm:grid-cols-2">
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.name")}
                </label>
                <Input
                  value={friendlyName}
                  onChange={(e) => setFriendlyName(e.target.value)}
                  disabled={!canManage}
                />
              </div>
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.status")}
                </label>
                <div className="pt-1.5">
                  <StatusBadge
                    status={apiClient.isEnabled ? "Enabled" : "Disabled"}
                  />
                </div>
              </div>
              <div className="space-y-1.5 sm:col-span-2">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("apiClients.clientId")}
                </label>
                <div>
                  <CopyableText value={apiClient.clientId} />
                </div>
                <p className="text-xs text-slate-500">
                  The public half of the credentials — safe to share with the
                  service that calls the API.
                </p>
              </div>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("apiClients.scopes")}
              </label>
              <ScopeSelector
                options={scopeOptions}
                selected={scopes}
                onChange={setScopes}
                disabled={!canManage}
              />
              <p className="text-xs text-slate-500">
                Saving replaces the client&apos;s scopes with exactly what is
                ticked here. A call outside them is answered with 403.
              </p>
            </div>

            <div className="grid gap-x-8 gap-y-4 border-t border-slate-100 pt-4 sm:grid-cols-3">
              <div>
                <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("apiClients.createdBy")}
                </span>
                <p className="mt-0.5 text-sm text-slate-900">
                  {relationText(
                    userName(resource?.createdByUser),
                    apiClient.createdByUserId
                  )}
                </p>
              </div>
              <div>
                <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.created")}
                </span>
                <p className="mt-0.5 text-sm text-slate-900">
                  <LocalTime value={apiClient.createdAt} />
                </p>
              </div>
              <div>
                <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.updated")}
                </span>
                <p className="mt-0.5 text-sm text-slate-900">
                  <LocalTime value={apiClient.updatedAt} />
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      {activeTab === "Secrets" && (
        <ApiClientSecretsPanel
          apiClientId={apiClientId}
          organizationId={organizationId}
          clientId={apiClient.clientId}
          canManage={canManage}
          onUnauthorized={goToLogin}
        />
      )}

      {activeTab === "History" && (
        <HistoryPanel
          entityType="ApiClient"
          path={historyPath.ApiClient(apiClientId, organizationId)}
          entityId={apiClientId}
          organizationId={organizationId}
          onUnauthorized={goToLogin}
          scopeNote="Names, the enabled flag and deletion are recorded here. Adding and revoking secrets is audited against the secret itself, and scopes are not audited."
        />
      )}

      <Dialog open={disableConfirmOpen} onOpenChange={setDisableConfirmOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("apiClients.disableThisClient")}</DialogTitle>
          </DialogHeader>

          <div className="flex items-start gap-2.5 rounded-lg border border-attention-200 bg-attention-50 px-3 py-2.5">
            <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
            <p className="text-sm text-attention-700">
              Every call{" "}
              <span className="font-medium">{apiClient.friendlyName}</span>{" "}
              makes starts failing within one request, including with tokens it
              already holds.
            </p>
          </div>

          <p className="text-sm text-slate-600">
            Nothing is lost — the client keeps its id, its scopes and its
            secrets, and enabling it again restores access immediately.
          </p>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setDisableConfirmOpen(false)}
              disabled={enabledLoading}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={() => setEnabled(false)} disabled={enabledLoading}>
              {enabledLoading ? "Disabling..." : "Disable client"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
