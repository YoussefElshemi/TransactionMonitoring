"use client";

import {
  WebhookConfiguration,
  WebhookConfigurationResource,
  WebhookConfigurationSecret
} from "@/types";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import {
  Save,
  FlaskConical,
  History,
  KeyRound,
  ScrollText,
  TriangleAlert
} from "lucide-react";
import { toast } from "sonner";
import { HistoryDialog, historyPath } from "@/components/history";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { SecretRevealDialog } from "@/components/SecretRevealDialog";
import { ToneBadge } from "@/components/status/StatusChips";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import { WebhookEventsCard } from "./WebhookEventsCard";
import { WebhookHealthCard } from "./WebhookHealthCard";
import { WebhookSendingFromCard } from "./WebhookSendingFromCard";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { usePermissions } from "@/context/PermissionsContext";
import { useWebhookCatalogue } from "@/lib/useWebhookCatalogue";
import {
  DISABLE_STATE_LABELS,
  PAYLOAD_VERSION_PINS,
  disableState,
  disableStateTone,
  eventsPatchValue
} from "@/lib/webhooks";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

export function WebhookConfigurationDetail({
  organizationSegment,
  webhookConfigurationId,
  historyOrganizationId,
  deliveriesHref
}: {
  organizationSegment: string;
  webhookConfigurationId: string;
  historyOrganizationId?: string;
  deliveriesHref?: (webhookConfigurationId: string) => string;
}) {
  const t = useTranslation();
  const [loading, setLoading] = useState<boolean>(true);
  const [saveLoading, setSaveLoading] = useState<boolean>(false);
  const [enableLoading, setEnableLoading] = useState<boolean>(false);
  const [webhookConfiguration, setWebhookConfiguration] =
    useState<WebhookConfiguration | null>(null);
  const [endpoint, setEndpoint] = useState<string>("");
  const [isEnabled, setIsEnabled] = useState<boolean>(false);
  const [events, setEvents] = useState<string[]>([]);
  const [payloadVersion, setPayloadVersion] = useState<number | null>(null);
  const [historyOpen, setHistoryOpen] = useState<boolean>(false);
  const [rotateConfirmOpen, setRotateConfirmOpen] = useState<boolean>(false);
  const [rotateLoading, setRotateLoading] = useState<boolean>(false);
  const [rotatedSecret, setRotatedSecret] = useState<string | null>(null);
  const [healthKey, setHealthKey] = useState<number>(0);

  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { holds } = usePermissions();
  const { catalogue } = useWebhookCatalogue();
  const canWrite = holds("webhook-configurations:write");

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const fetchWebhookConfiguration = async (
    organizationId: string,
    webhookConfigurationId: string
  ) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/webhook-configurations/${webhookConfigurationId}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const { webhookConfiguration } =
          await unwrap<WebhookConfigurationResource>(response);
        setWebhookConfiguration(webhookConfiguration);
        setEndpoint(webhookConfiguration.endpointUrl);
        setIsEnabled(webhookConfiguration.isEnabled);
        setEvents(webhookConfiguration.events);
        setPayloadVersion(webhookConfiguration.payloadVersion);
      } else {
        if (response.status === 403) {
          await reportApiError(response, {
            fallback: t("common.youDoNotHavePermission"),
            onUnauthorized: goToLogin,
            toastId: "forbidden"
          });
          return router.push("/");
        }

        await reportApiError(response, {
          fallback: t("webhooks.errorOccurredFetchingWebhookConfiguration"),
          onUnauthorized: goToLogin,
          toastId: "error-fetching"
        });
      }
    } catch {
      toast.error(
        t("webhooks.errorOccurredFetchingWebhookConfiguration"),
        {
          id: "error-fetching"
        }
      );
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async (
    organizationId: string,
    webhookConfigurationId: string
  ) => {
    if (!webhookConfiguration) return;

    setSaveLoading(true);

    const payload: {
      endpointUrl?: string;
      isEnabled?: boolean;
      events?: string[];
      payloadVersion?: number;
    } = {};

    if (endpoint !== webhookConfiguration.endpointUrl) {
      payload.endpointUrl = endpoint;
    }

    if (isEnabled !== webhookConfiguration.isEnabled) {
      payload.isEnabled = isEnabled;
    }

    const nextEvents = eventsPatchValue(webhookConfiguration.events, events);
    if (nextEvents !== undefined) {
      payload.events = nextEvents;
    }

    if (
      payloadVersion !== null &&
      payloadVersion !== webhookConfiguration.payloadVersion
    ) {
      payload.payloadVersion = payloadVersion;
    }

    if (Object.keys(payload).length === 0) {
      setSaveLoading(false);
      toast.info(t("common.nothingSave"), {
        description: t("webhooks.configurationAlreadyScreen")
      });
      return;
    }

    await patch(organizationId, webhookConfigurationId, payload, {
      success: "Successfully updated webhook configuration",
      fallback: t("common.errorOccurredSavingChanges")
    });

    setSaveLoading(false);
  };

  const handleEnable = async (
    organizationId: string,
    webhookConfigurationId: string
  ) => {
    setEnableLoading(true);

    await patch(
      organizationId,
      webhookConfigurationId,
      { isEnabled: true },
      {
        success: "Endpoint enabled",
        fallback: t("webhooks.errorOccurredEnablingEndpoint")
      }
    );

    setEnableLoading(false);
  };

  const patch = async (
    organizationId: string,
    webhookConfigurationId: string,
    body: Record<string, unknown>,
    messages: { success: string; fallback: string }
  ) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/webhook-configurations/${webhookConfigurationId}`,
        {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include",
          body: JSON.stringify(body)
        }
      );

      if (response.ok) {
        toast.success(messages.success);
        setHealthKey((key) => key + 1);
        await fetchWebhookConfiguration(organizationSegment, webhookConfigurationId);
      } else {
        await reportApiError(response, {
          fallback: messages.fallback,
          onUnauthorized: goToLogin
        });
      }
    } catch {
      toast.error(messages.fallback);
    }
  };

  const handleTest = async (
    organizationId: string,
    webhookConfigurationId: string
  ) => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/webhook-configurations/${webhookConfigurationId}/test`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("webhooks.successfullySentTestWebhook"), {
          description:
            t("webhooks.testEventDeliveredWhateverEndpoint")
        });
        setHealthKey((key) => key + 1);
      } else if (response.status === 400) {
        toast.error(t("webhooks.testWebhookReturnedUnsuccessfulResponse"));
        setHealthKey((key) => key + 1);
      } else {
        await reportApiError(response, {
          fallback: t("webhooks.errorOccurredTestingWebhook"),
          onUnauthorized: goToLogin
        });
      }
    } catch {
      toast.error(t("webhooks.errorOccurredTestingWebhook"));
    } finally {
      setLoading(false);
    }
  };

  const handleRotateSecret = async (
    organizationId: string,
    webhookConfigurationId: string
  ) => {
    setRotateLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/webhook-configurations/${webhookConfigurationId}/rotate-secret`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const { secretKey, ...rotated } =
          await unwrap<WebhookConfigurationSecret>(response);
        setRotateConfirmOpen(false);
        setRotatedSecret(secretKey);
        setWebhookConfiguration((previous) =>
          previous === null
            ? null
            : {
                ...rotated,
                disabledAt: previous.disabledAt,
                disabledReason: previous.disabledReason
              }
        );
      } else {
        await reportApiError(response, {
          fallback: t("webhooks.errorOccurredRotatingSecret"),
          onUnauthorized: goToLogin
        });
      }
    } catch {
      toast.error(t("webhooks.errorOccurredRotatingSecret"));
    } finally {
      setRotateLoading(false);
    }
  };

  useEffect(() => {
    fetchWebhookConfiguration(organizationSegment, webhookConfigurationId);
  }, []);

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Card>
          <CardContent className="pt-6 space-y-3">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-4 w-full" />
            ))}
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!webhookConfiguration) {
    return <NotFound
        title={t("webhooks.webhookNotFound")}
        description={t("common.notFound.description")}
      />;
  }

  const state = disableState(webhookConfiguration);
  const supportedVersions = catalogue?.supportedPayloadVersions ?? [
    webhookConfiguration.payloadVersion
  ];

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("webhooks.webhook")} title={t("webhooks.webhookConfiguration")}>
        {deliveriesHref && (
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5"
            onClick={() => router.push(deliveriesHref(webhookConfigurationId))}
          >
            <ScrollText className="size-4" />
            {t("webhooks.deliveries")}
          </Button>
        )}
        {historyOrganizationId && (
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5"
            onClick={() => setHistoryOpen(true)}
          >
            <History className="size-4" />
            {t("common.history")}
          </Button>
        )}
        {canWrite && (
          <>
            <Button
              variant="outline"
              size="sm"
              onClick={() =>
                handleTest(organizationSegment, webhookConfigurationId)
              }
            >
              <FlaskConical className="size-4" />
              {t("webhooks.test")}
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5"
              onClick={() => setRotateConfirmOpen(true)}
            >
              <KeyRound className="size-4" />
              {t("webhooks.rotateSecret")}
            </Button>
            <Button
              size="sm"
              onClick={() =>
                handleSave(organizationSegment, webhookConfigurationId)
              }
              disabled={saveLoading}
            >
              <Save className="size-4" />
              {saveLoading ? "Saving..." : "Save"}
            </Button>
          </>
        )}
      </PageHeader>

      <Card>
        <CardContent className="pt-6">
          <div className="grid grid-cols-2 gap-x-8 gap-y-4">
            <div className="col-span-2 space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("webhooks.endpointUrl")}
              </label>
              <Input
                id="endpoint"
                type="text"
                value={endpoint}
                disabled={!canWrite}
                onChange={(e) => setEndpoint(e.target.value)}
              />
            </div>
            <div className="col-span-2 space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("webhooks.signingSecret")}
              </label>
              <p className="text-sm text-slate-600">
                <span className="font-mono text-slate-400">
                  {"•".repeat(32)}
                </span>{" "}
                <span className="text-slate-500">
                  Set when this webhook was created. Rotate it to get a new one
                  — the value cannot be read back.
                </span>
              </p>
            </div>
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.status")}
              </label>
              <div className="mt-1">
                <ToneBadge tone={disableStateTone(state)}>
                  {DISABLE_STATE_LABELS[state]}
                </ToneBadge>
              </div>
            </div>
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.enabled")}
              </label>
              <div className="flex items-center mt-1">
                <input
                  type="checkbox"
                  checked={isEnabled}
                  disabled={!canWrite}
                  onChange={(e) => setIsEnabled(e.target.checked)}
                  className="h-4 w-4 rounded border-slate-300 accent-brand-800"
                />
              </div>
            </div>
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("webhooks.payloadVersion")}
              </label>
              {canWrite && supportedVersions.length > 1 ? (
                <Select
                  value={String(payloadVersion ?? webhookConfiguration.payloadVersion)}
                  onValueChange={(value) => setPayloadVersion(parseInt(value))}
                >
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {supportedVersions.map((version) => (
                      <SelectItem key={version} value={String(version)}>
                        v{version}
                        {version === catalogue?.currentPayloadVersion
                          ? " · current"
                          : ""}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              ) : (
                <p className="mt-0.5 font-mono text-sm text-slate-900">
                  v{webhookConfiguration.payloadVersion}
                </p>
              )}
              <p className="max-w-prose text-xs leading-relaxed text-slate-500">
                {PAYLOAD_VERSION_PINS}
              </p>
            </div>
            <div>
              <span className="text-sm font-medium text-slate-500">
                {t("webhooks.createdAt")}
              </span>
              <p className="mt-0.5 text-sm text-slate-900">
                <LocalTime value={webhookConfiguration.createdAt} />
              </p>
            </div>
            <div>
              <span className="text-sm font-medium text-slate-500">
                {t("webhooks.updatedAt")}
              </span>
              <p className="mt-0.5 text-sm text-slate-900">
                <LocalTime value={webhookConfiguration.updatedAt} />
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <WebhookHealthCard
        organizationSegment={organizationSegment}
        webhookConfigurationId={webhookConfigurationId}
        canWrite={canWrite}
        refreshKey={healthKey}
        enabling={enableLoading}
        onEnable={() =>
          handleEnable(organizationSegment, webhookConfigurationId)
        }
        onUnauthorized={goToLogin}
      />

      <WebhookEventsCard
        selected={events}
        original={webhookConfiguration.events}
        onChange={canWrite ? setEvents : undefined}
      />

      <WebhookSendingFromCard
        pinnedVersion={payloadVersion ?? webhookConfiguration.payloadVersion}
      />

      <Dialog open={rotateConfirmOpen} onOpenChange={setRotateConfirmOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("webhooks.rotateSigningSecret")}</DialogTitle>
          </DialogHeader>
          <div className="flex items-start gap-2.5 rounded-lg border border-attention-200 bg-attention-50 px-3 py-2.5">
            <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
            <p className="text-sm text-attention-700">
              The current secret stops working immediately. Deliveries to{" "}
              <span className="font-mono text-xs">
                {webhookConfiguration.endpointUrl}
              </span>{" "}
              will fail signature verification until the new secret is deployed
              there.
            </p>
          </div>
          <p className="text-sm text-slate-600">
            {t("webhooks.theNewSecretIsShown")}
          </p>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setRotateConfirmOpen(false)}
              disabled={rotateLoading}
            >
              {t("common.cancel")}
            </Button>
            <Button
              onClick={() =>
                handleRotateSecret(
                  organizationSegment,
                  webhookConfigurationId
                )
              }
              disabled={rotateLoading}
            >
              {rotateLoading ? "Rotating..." : "Rotate secret"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <SecretRevealDialog
        secret={rotatedSecret}
        title={t("webhooks.yourNewSigningSecret")}
        description={t("webhooks.thePreviousSecretIsNo")}
        onClose={() => setRotatedSecret(null)}
      />

      {historyOrganizationId && (
        <HistoryDialog
          open={historyOpen}
          onOpenChange={setHistoryOpen}
          title={webhookConfiguration.endpointUrl}
          entityType="WebhookConfiguration"
          path={historyPath.WebhookConfiguration(
            historyOrganizationId,
            webhookConfigurationId
          )}
          entityId={webhookConfigurationId}
          onUnauthorized={goToLogin}
        />
      )}
    </div>
  );
};
