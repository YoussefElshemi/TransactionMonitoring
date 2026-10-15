"use client";

import { useCallback, useEffect, useState } from "react";
import { KeyRound, Plus, TriangleAlert } from "lucide-react";
import { toast } from "sonner";

import { ApiClientSecret, CreatedApiClientSecret } from "@/types";
import { organizationScopedPath, reportApiError, unwrap } from "@/lib/api";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import { DateTimePicker } from "@/components/DateTimePicker";
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
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";

import { ClientCredentialsDialog } from "./ClientCredentialsDialog";
import {
  isUsable,
  secretState,
  usableCount,
  withoutPlaintext
} from "./secrets";

import { apiFetch } from "@/lib/http";
import { useEnvironment } from "@/context/ModeContext";

import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";

export function ApiClientSecretsPanel({
  apiClientId,
  organizationId,
  clientId,
  canManage,
  onUnauthorized
}: {
  apiClientId: string;
  organizationId?: string;
  clientId: string;
  canManage: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { mode } = useEnvironment();

  const secretsPath = `${organizationScopedPath(
    "api-clients",
    organizationId
  )}/${apiClientId}/secrets`;
  const canIssue = canManage && organizationId === undefined;
  const [secrets, setSecrets] = useState<ApiClientSecret[]>([]);
  const [loading, setLoading] = useState<boolean>(true);

  const [addOpen, setAddOpen] = useState<boolean>(false);
  const [addLoading, setAddLoading] = useState<boolean>(false);
  const [expiresAt, setExpiresAt] = useState<string>("");

  const [revokeTarget, setRevokeTarget] = useState<ApiClientSecret | null>(null);
  const [revokeLoading, setRevokeLoading] = useState<boolean>(false);

  const [createdSecret, setCreatedSecret] = useState<string | null>(null);

  const fetchSecrets = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(secretsPath, { credentials: "include" });

      if (response.ok) {
        const values = await unwrap<ApiClientSecret[]>(response);
        setSecrets(values.filter((secret) => !secret.mode || secret.mode.toLowerCase() === mode));
      } else {
        await reportApiError(response, {
          fallback: t("apiClients.errorOccurredFetchingSecrets"),
          onUnauthorized,
          toastId: "error-fetching-secrets"
        });
      }
    } catch {
      toast.error(t("apiClients.errorOccurredFetchingSecrets"), {
        id: "error-fetching-secrets"
      });
    } finally {
      setLoading(false);
    }
  }, [apiClientId, secretsPath, mode, onUnauthorized]);

  useEffect(() => {
    fetchSecrets();
  }, [fetchSecrets]);

  const handleAddSecret = async () => {
    setAddLoading(true);

    try {
      const response = await apiFetch(
        secretsPath,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            expiresAt: expiresAt ? new Date(expiresAt).toISOString() : null
          })
        }
      );

      if (response.ok) {
        const data = await unwrap<CreatedApiClientSecret>(response);
        setAddOpen(false);
        setExpiresAt("");
        setCreatedSecret(data.clientSecret);
        // Fold the new row in from the response rather than refetching: a
        // refetch flips `loading`, which would swap the panel — and the reveal
        // dialog with it — for the skeleton.
        setSecrets((current) => [withoutPlaintext(data), ...current]);
      } else {
        await reportApiError(response, {
          fallback: t("apiClients.failedAddSecret"),
          onUnauthorized
        });
      }
    } catch {
      toast.error(t("apiClients.failedAddSecret"));
    } finally {
      setAddLoading(false);
    }
  };

  const handleRevoke = async (secret: ApiClientSecret) => {
    setRevokeLoading(true);

    try {
      const response = await apiFetch(
        `${secretsPath}/${secret.apiClientSecretId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("apiClients.revokedSecret", secret.secretPrefix));
        setRevokeTarget(null);
        await fetchSecrets();
      } else {
        await reportApiError(response, {
          fallback: t("apiClients.failedRevokeSecret"),
          onUnauthorized
        });
      }
    } catch {
      toast.error(t("apiClients.failedRevokeSecret"));
    } finally {
      setRevokeLoading(false);
    }
  };

  const usable = usableCount(secrets);
  const revokingLastUsable =
    revokeTarget !== null && isUsable(revokeTarget) && usable === 1;

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-slate-500">
          {loading
            ? "Loading secrets…"
            : `${usable} usable ${usable === 1 ? "secret" : "secrets"}${
                secrets.length > usable
                  ? `, ${secrets.length - usable} revoked or expired`
                  : ""
              }`}
        </p>
        {canIssue && (
          <Button
            size="sm"
            className="gap-1.5"
            onClick={() => {
              setExpiresAt("");
              setAddOpen(true);
            }}
          >
            <Plus className="size-4" />
            {t("apiClients.addSecret")}
          </Button>
        )}
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("apiClients.secret")}</TableHead>
              <TableHead>{t("common.status")}</TableHead>
              <TableHead>{t("common.created")}</TableHead>
              <TableHead>{t("apiClients.lastUsed")}</TableHead>
              <TableHead>{t("apiClients.expires")}</TableHead>
              {canManage && <TableHead className="w-28">{t("common.actions")}</TableHead>}
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 2 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: canManage ? 6 : 5 }).map((_, j) => (
                    <TableCell key={j}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : secrets.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={canManage ? 6 : 5}
                  className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {t("apiClients.noSecretsThisClientCannot")}
                </TableCell>
              </TableRow>
            ) : (
              secrets.map((secret) => {
                const state = secretState(secret);

                return (
                  <TableRow
                    key={secret.apiClientSecretId}
                    className="hover:bg-slate-50/50"
                  >
                    <TableCell>
                      <span className="inline-flex items-center gap-1.5 font-mono text-xs text-slate-600">
                        <KeyRound className="size-3.5 flex-shrink-0 text-slate-400" />
                        {secret.secretPrefix}
                        <span className="text-slate-300">{"•".repeat(12)}</span>
                      </span>
                    </TableCell>
                    <TableCell>
                      <StatusBadge
                        status={state === "Active" ? "Enabled" : "Disabled"}
                        label={state}
                      />
                    </TableCell>
                    <TableCell className="text-sm text-slate-600">
                      <Timestamp value={secret.createdAt} />
                    </TableCell>
                    <TableCell className="text-sm text-slate-600">
                      {secret.lastUsedAt === null ? (
                        <span
                          className="font-mono text-xs uppercase tracking-widest text-slate-400"
                          title={t("apiClients.thisSecretHasNeverBeen")}
                        >
                          {t("apiClients.never")}
                        </span>
                      ) : (
                        <Timestamp value={secret.lastUsedAt} />
                      )}
                    </TableCell>
                    <TableCell className="text-sm text-slate-600">
                      {secret.expiresAt === null ? (
                        <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
                          {t("apiClients.never")}
                        </span>
                      ) : (
                        <Timestamp value={secret.expiresAt} />
                      )}
                    </TableCell>
                    {canManage && (
                      <TableCell>
                        <Button
                          variant="outline"
                          size="sm"
                          className="text-critical-700 hover:bg-critical-50 hover:text-critical-700"
                          disabled={state === "Revoked"}
                          onClick={() => setRevokeTarget(secret)}
                        >
                          {t("apiClients.revoke")}
                        </Button>
                      </TableCell>
                    )}
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      <p className="text-xs text-slate-400">
        Rotate without downtime: add a secret, deploy it to the caller, then
        revoke the old one once it stops being used. Both work in the meantime —
        a rotated-away secret whose last use predates the rollout is the signal
        that nothing depends on it any more.
      </p>

      <Dialog open={addOpen} onOpenChange={setAddOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("apiClients.addASecret")}</DialogTitle>
            <DialogDescription>
              The client&apos;s existing secrets keep working. The new one is
              shown once, straight after this.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("apiClients.expiresAt")}
            </label>
            <DateTimePicker
              value={expiresAt}
              onChange={setExpiresAt}
              placeholder={t("apiClients.noExpiry")}
              aria-label={t("apiClients.secretExpiry")}
              disabled={addLoading}
            />
            <p className="text-xs text-slate-500">
              Optional, and must be in the future. Leave it empty for a secret
              that stays valid until it is revoked.
            </p>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setAddOpen(false)}
              disabled={addLoading}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={handleAddSecret} disabled={addLoading}>
              {addLoading ? "Adding..." : "Add secret"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={revokeTarget !== null}
        onOpenChange={(open) => !open && setRevokeTarget(null)}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("apiClients.revokeThisSecret")}</DialogTitle>
          </DialogHeader>

          <p className="text-sm text-slate-600">
            <span className="font-mono text-xs text-slate-700">
              {revokeTarget?.secretPrefix}…
            </span>{" "}
            stops authenticating immediately, and the access and refresh tokens
            it has already issued stop working with it. This cannot be undone.
          </p>

          {revokingLastUsable && (
            <div className="flex items-start gap-2.5 rounded-lg border border-attention-200 bg-attention-50 px-3 py-2.5">
              <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
              <p className="text-sm text-attention-700">
                This is the client&apos;s last usable secret. Revoking it leaves
                the client unable to authenticate at all — add its replacement
                first if anything is still calling with it.
              </p>
            </div>
          )}

          {revokeTarget?.lastUsedAt === null && (
            <p className="text-xs text-slate-500">
              This secret has never been used, so nothing is calling with it
              yet.
            </p>
          )}

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setRevokeTarget(null)}
              disabled={revokeLoading}
            >
              {t("common.cancel")}
            </Button>
            <Button
              className="bg-critical-solid text-critical-solid-foreground hover:bg-critical-solid/90"
              onClick={() => revokeTarget && handleRevoke(revokeTarget)}
              disabled={revokeLoading}
            >
              {revokeLoading ? "Revoking..." : "Revoke secret"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ClientCredentialsDialog
        clientId={clientId}
        clientSecret={createdSecret}
        title={t("apiClients.yourNewClientSecret")}
        description={t("apiClients.deployThisToTheCaller")}
        onAcknowledge={() => setCreatedSecret(null)}
      />
    </div>
  );
}

function Timestamp({ value }: { value: string }) {
  const format = useFormat();
  const date = new Date(value);

  return (
    <time dateTime={value} title={format.instant(date)}>
      {format.relative(date)}
    </time>
  );
}
