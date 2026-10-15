"use client";

import { useState, useEffect } from "react";
import { useRouter, useSearchParams, usePathname } from "next/navigation";
import {
  ValidationProblemDetails,
  ProblemDetails,
  WebhookConfigurationSecret,
  Paged,
  WebhookConfigurationResource,
  WebhookConfiguration
} from "@/types";
import { toast } from "sonner";
import Pagination from "@/components/Pagination";
import { PageHeader } from "@/components/PageHeader";
import { SecretRevealDialog } from "@/components/SecretRevealDialog";
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
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import { Eye, KeyRound, Plus, Trash2 } from "lucide-react";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, unwrap } from "@/lib/api";
import { ToneBadge } from "@/components/status/StatusChips";
import { WebhookEventsCard } from "@/components/webhooks/WebhookEventsCard";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import { useWebhookCatalogue } from "@/lib/useWebhookCatalogue";
import {
  DISABLE_STATE_LABELS,
  disableState,
  disableStateTone,
  subscriptionSummary
} from "@/lib/webhooks";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

export function WebhookConfigurationsPanel({
  organizationSegment,
  detailHref,
  description = "Where this organization hears about cases the rules open.",
  embedded = false
}: {
  organizationSegment: string;
  detailHref: (webhookConfigurationId: string) => string;
  description?: string;
  embedded?: boolean;
}) {
  const t = useTranslation();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [newIsEnabled, setNewIsEnabled] = useState<boolean>(false);
  const [newEndpoint, setNewEndpoint] = useState<string>("");
  const [newEvents, setNewEvents] = useState<string[]>([]);

  const { catalogue } = useWebhookCatalogue();

  const [webhookConfigurations, setWebhookConfigurations] = useState<
    WebhookConfiguration[]
  >([]);
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

  const [
    createWebhookConfigurationModalOpen,
    setCreateWebhookConfigurationModalOpen
  ] = useState<boolean>(false);
  const [modalLoading, setModalLoading] = useState<boolean>(false);

  const [createdSecret, setCreatedSecret] =
    useState<WebhookConfigurationSecret | null>(null);

  const router = useRouter();

  const handleCreate = () => {
    setCreateWebhookConfigurationModalOpen(true);
  };

  const handleCloseModal = () => {
    setCreateWebhookConfigurationModalOpen(false);
    setNewEndpoint("");
    setNewIsEnabled(false);
    setNewEvents([]);
  };

  const fetchWebhookConfigurations = async (
    organizationId: string,
    pageNumber: number,
    pageSize: number,
    searchTerm: string
  ) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/webhook-configurations?PageNumber=${pageNumber}&PageSize=${pageSize}&FriendlyName=${searchTerm}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = (await response.json()) as Paged<WebhookConfigurationResource>;
        setWebhookConfigurations(
          data.data.map((resource) => resource.webhookConfiguration)
        );
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
          errorData.detail || t("webhooks.errorOccurredFetchingWebhookConfigurations"),
          { id: "error-fetching-webhook-configurations" }
        );
      }
    } catch {
      toast.error(t("webhooks.errorOccurredFetchingWebhookConfigurations"), {
        id: "error-fetching-webhook-configurations"
      });
    } finally {
      setLoading(false);
    }
  };

  const handleCreateWebhookConfiguration = async (organizationId: string) => {
    setModalLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/webhook-configurations`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            endpointUrl: newEndpoint,
            isEnabled: newIsEnabled,
            events: newEvents
          }),
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = await unwrap<WebhookConfigurationSecret>(response);
        toast.success(t("webhooks.successfullyCreatedWebhookConfiguration"));

        handleCloseModal();
        setCreatedSecret(data);
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
          errorData.detail || t("webhooks.failedCreateWebhookConfiguration")
        );
      }
    } catch {
      toast.error(t("webhooks.failedCreateWebhookConfiguration"));
    } finally {
      setModalLoading(false);
    }
  };

  const deleteWebhookConfiguration = async (
    organizationId: string,
    webhookConfigurationId: string
  ) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/webhook-configurations/${webhookConfigurationId}`,
        {
          method: "DELETE",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("webhooks.successfullyDeletedWebhookConfiguration"), {
          action: { label: t("common.undo"), onClick: () => recoverWebhookConfiguration(organizationId, webhookConfigurationId) },
        });
        fetchWebhookConfigurations(
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
        toast.error(
          errorData.detail || t("webhooks.failedDeleteWebhookConfiguration")
        );
      }
    } catch {
      toast.error(t("webhooks.failedDeleteWebhookConfiguration"));
    }
  };

  const recoverWebhookConfiguration = async (
    organizationId: string,
    webhookConfigurationId: string
  ) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/webhook-configurations/${webhookConfigurationId}/recover`,
        {
          method: "POST",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("webhooks.successfullyRecoveredWebhookConfiguration"));
        fetchWebhookConfigurations(
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
        toast.error(
          errorData.detail || t("webhooks.failedRecoverWebhookConfiguration")
        );
      }
    } catch {
      toast.error(t("webhooks.failedRecoverWebhookConfiguration"));
    }
  };

  useEffect(() => {
    fetchWebhookConfigurations(organizationSegment, pageNumber, pageSize, searchTerm);
  }, [organizationSegment, pageNumber, pageSize, searchTerm]);

  useEffect(() => {
    if (embedded) return;

    const params = new URLSearchParams();
    params.set("pageNumber", pageNumber.toString());
    params.set("pageSize", pageSize.toString());
    if (searchTerm.length) {
      params.set("friendlyName", searchTerm);
    }
    router.replace(`?${params.toString()}`);
  }, [embedded, pageNumber, pageSize, searchTerm, router]);

  return (
    <div className="space-y-4">
      {!embedded && (
        <PageHeader
          eyebrow={t("webhooks.organization")}
          title={t("webhooks.webhookConfigurations")}
          description={description}
        >
          <Button size="sm" className="gap-1.5" onClick={handleCreate}>
            <Plus className="size-4" />
            {t("webhooks.newWebhookConfiguration")}
          </Button>
        </PageHeader>
      )}

      <div className="flex items-center justify-between gap-3">
        <Input
          value={searchTerm}
          onChange={(e) => {
            setSearchTerm(e.target.value);
            setPageNumber(1);
          }}
          placeholder={t("webhooks.searchByFriendlyName")}
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
          {embedded && (
            <Button size="sm" className="gap-1.5" onClick={handleCreate}>
              <Plus className="size-4" />
              {t("webhooks.newWebhookConfiguration")}
            </Button>
          )}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("webhooks.endpointUrl")}</TableHead>
              <TableHead>{t("webhooks.subscription")}</TableHead>
              <TableHead>{t("webhooks.signingSecret")}</TableHead>
              <TableHead>{t("common.status")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
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
            ) : webhookConfigurations.length === 0 ? (
              <TableRow>
                <TableCell colSpan={5} className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.noResults")}
                </TableCell>
              </TableRow>
            ) : (
              webhookConfigurations.map((webhookConfiguration) => (
                <TableRow
                  key={webhookConfiguration.webhookConfigurationId}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell className="font-medium text-slate-900">
                    {webhookConfiguration.endpointUrl}
                  </TableCell>
                  <TableCell>
                    <span className="font-mono text-xs text-slate-600">
                      {subscriptionSummary(
                        webhookConfiguration,
                        catalogue?.events.filter((e) => e.isSubscribable).length
                      )}
                    </span>
                  </TableCell>
                  <TableCell>
                    <span className="inline-flex items-center gap-1.5 font-mono text-xs uppercase tracking-widest text-slate-400">
                      <KeyRound className="size-3.5" />
                      {t("webhooks.set")}
                    </span>
                  </TableCell>
                  <TableCell>
                    <ToneBadge
                      tone={disableStateTone(disableState(webhookConfiguration))}
                      title={webhookConfiguration.disabledReason ?? undefined}
                    >
                      {DISABLE_STATE_LABELS[disableState(webhookConfiguration)]}
                    </ToneBadge>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="outline"
                        size="sm"
                        className="gap-1.5"
                        onClick={() =>
                          router.push(
                            detailHref(webhookConfiguration.webhookConfigurationId)
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
                        onClick={() =>
                          deleteWebhookConfiguration(
                            organizationSegment,
                            webhookConfiguration.webhookConfigurationId
                          )
                        }
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

      {webhookConfigurations.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() => setPageNumber((p) => Math.min(totalPages, p + 1))}
        />
      )}

      <SecretRevealDialog
        secret={createdSecret?.secretKey ?? null}
        description={t("webhooks.signVerifyThisWebhookS")}
        onClose={() => {
          const created = createdSecret;
          setCreatedSecret(null);
          if (created) {
            router.push(detailHref(created.webhookConfigurationId));
          }
        }}
      />

      <Dialog
        open={createWebhookConfigurationModalOpen}
        onOpenChange={setCreateWebhookConfigurationModalOpen}
      >
        <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("webhooks.createWebhookConfiguration")}</DialogTitle>
          </DialogHeader>
          <div className="space-y-3">
            <Input
              placeholder={t("webhooks.endpointUrl")}
              value={newEndpoint}
              onChange={(e) => setNewEndpoint(e.target.value)}
            />
            <label className="flex items-center gap-2 font-mono text-sm text-slate-700">
              <input
                type="checkbox"
                checked={newIsEnabled}
                onChange={(e) => setNewIsEnabled(e.target.checked)}
                className="h-4 w-4 rounded border-slate-300 accent-brand-800"
              />
              {t("common.enabled")}
            </label>
            <WebhookEventsCard
              selected={newEvents}
              onChange={setNewEvents}
              className="border-slate-200 shadow-none"
            />
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={handleCloseModal}
              disabled={modalLoading}
            >
              {t("common.cancel")}
            </Button>
            <Button
              onClick={() => handleCreateWebhookConfiguration(organizationSegment)}
              disabled={modalLoading}
            >
              {modalLoading ? "Creating..." : "Create"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};
