"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { ArrowLeft, Save, Trash2 } from "lucide-react";

import {
  SubOrganizationBody,
  SubOrganizationResource,
  SubOrganization,
  SubOrganizationUsage,
} from "@/types";
import { reportApiError, unwrap } from "@/lib/api";
import {
  readSubOrganization,
  subOrganizationsPath,
  SUB_ORGANIZATION_DESCRIPTION_MAX,
  SUB_ORGANIZATION_NAME_MAX,
} from "@/lib/roles";
import { SUB_ORGANIZATION_OWNERSHIP_UNREADABLE } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import { TabBar } from "@/components/TabBar";
import { FieldList, Field } from "@/components/FieldList";
import { HistoryPanel, historyPath } from "@/components/history";
import { UsageWarning } from "@/components/subOrganizations/SubOrganizationParts";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

export function SubOrganizationDetail({
  organizationId,
  subOrganizationId,
  backHref,
  onUnauthorized,
}: {
  organizationId: string;
  subOrganizationId: string;
  backHref: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();
  const { holds, holdsAll } = usePermissions();
  const canWrite = holds("sub-organizations:write");
  const canReadHistory = holdsAll("sub-organizations:read", "audit-logs:read");

  const [unit, setUnit] = useState<SubOrganization | null>(null);
  const [usage, setUsage] = useState<SubOrganizationUsage | null>(null);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [activeTab, setActiveTab] = useState("Contents");

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [reference, setReference] = useState("");
  const [isEnabled, setIsEnabled] = useState(true);
  const [saving, setSaving] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const path = `${subOrganizationsPath(organizationId)}/${subOrganizationId}`;

  const fetchUnit = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(path, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const fetched = readSubOrganization(
          await unwrap<SubOrganizationResource>(response)
        );
        setUnit(fetched);
        setName(fetched.name);
        setDescription(fetched.description ?? "");
        setReference(fetched.externalReference ?? "");
        setIsEnabled(fetched.isEnabled);
        setNotFound(false);
      } else if (response.status === 404) {
        setNotFound(true);
      } else {
        await reportApiError(response, {
          fallback: t("subOrganizations.errorOccurredFetchingBusinessUnit"),
          onUnauthorized,
          toastId: "error-fetching-unit",
        });
      }
    } catch {
      toast.error(t("subOrganizations.errorOccurredFetchingBusinessUnit"), {
        id: "error-fetching-unit",
      });
    } finally {
      setLoading(false);
    }
  }, [path, onUnauthorized]);

  const fetchUsage = useCallback(async () => {
    try {
      const response = await apiFetch(`${path}/usages`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });
      if (response.ok) {
        setUsage(await unwrap<SubOrganizationUsage>(response));
      }
    } catch {
    }
  }, [path]);

  useEffect(() => {
    fetchUnit();
    fetchUsage();
  }, [fetchUnit, fetchUsage]);

  const handleSave = async () => {
    if (!unit) return;
    setSaving(true);

    const trimmedReference = reference.trim();
    const body: SubOrganizationBody = {
      name: name.trim(),
      description: description.trim() || null,
      isEnabled,
    };

    // The one rule that matters here. An emptied box means "clear it", which is
    // its own flag; sending `externalReference: ""` alongside it is a 400, and
    // sending `""` on its own would set the reference to an empty string.
    if (trimmedReference) {
      body.externalReference = trimmedReference;
    } else if (unit.externalReference) {
      body.clearExternalReference = true;
    }

    try {
      const response = await apiFetch(path, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify(body),
      });

      if (response.ok) {
        toast.success(t("common.namedSaved", name.trim()));
        fetchUnit();
      } else {
        await reportApiError(response, {
          fallback: t("subOrganizations.failedSaveBusinessUnit"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("subOrganizations.failedSaveBusinessUnit"));
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (!unit) return;

    try {
      const response = await apiFetch(path, {
        method: "DELETE",
        credentials: "include",
      });

      if (response.ok) {
        toast.success(t("common.namedDeleted", unit.name));
        setDeleteOpen(false);
        router.push(backHref);
      } else {
        await reportApiError(response, {
          fallback: t("subOrganizations.failedDeleteBusinessUnit"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("subOrganizations.failedDeleteBusinessUnit"));
    }
  };

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (notFound || !unit) {
    return (
      <NotFound
        title={t("subOrganizations.businessUnitNotFound")}
        description={t("common.notFound.description")}
        backHref={backHref}
        backLabel={t("subOrganizations.allBusinessUnits")}
      />
    );
  }

  const tabs = ["Contents", ...(canReadHistory ? ["History"] : [])];
  const nameBlocked = name.trim().length === 0;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("subOrganizations.businessUnit")}
        title={unit.name}
        description={unit.description ?? undefined}
      >
        <Button variant="outline" size="sm" className="gap-1.5" asChild>
          <Link href={backHref}>
            <ArrowLeft className="size-4" />
            {t("subOrganizations.allUnits")}
          </Link>
        </Button>
        {canWrite && (
          <>
            <Button
              variant="outline"
              size="sm"
              className="gap-1.5"
              onClick={handleSave}
              disabled={saving || nameBlocked}
            >
              <Save className="size-4" />
              {saving ? "Saving..." : "Save"}
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-critical-500 hover:text-critical-700"
              title={t("common.delete")}
              aria-label={t("common.delete")}
              onClick={() => setDeleteOpen(true)}
            >
              <Trash2 className="size-4" />
            </Button>
          </>
        )}
      </PageHeader>

      <Card>
        <CardContent className="space-y-4 pt-6">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.name")}
              </label>
              <Input
                value={name}
                onChange={(event) => setName(event.target.value)}
                disabled={!canWrite}
                maxLength={SUB_ORGANIZATION_NAME_MAX}
                className="font-mono"
              />
            </div>
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("subOrganizations.externalReference")}
              </label>
              <Input
                value={reference}
                onChange={(event) => setReference(event.target.value)}
                disabled={!canWrite}
                placeholder="BU-0041"
                className="font-mono"
              />
              <p className="text-xs text-slate-500">
                {t("subOrganizations.carriedAndNeverInterpretedEmptying")}
              </p>
            </div>
            <div className="space-y-1.5 sm:col-span-2">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.description")}
              </label>
              <Textarea
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                disabled={!canWrite}
                maxLength={SUB_ORGANIZATION_DESCRIPTION_MAX}
                rows={2}
              />
            </div>
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.status")}
              </label>
              <div className="flex items-center gap-2 pt-1">
                <input
                  type="checkbox"
                  checked={isEnabled}
                  onChange={(event) => setIsEnabled(event.target.checked)}
                  disabled={!canWrite}
                  className="h-4 w-4 rounded border-slate-300 accent-brand-800"
                />
                <span className="text-sm text-slate-700">{t("common.enabled")}</span>
              </div>
              <p className="text-xs text-slate-500">
                Disabling takes the unit out of the pickers. It does not release
                anybody homed in it — their permissions stay narrowed to it.
              </p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <FieldList>
            <Field label={t("common.status")}>
              <StatusBadge status={unit.isEnabled ? "Enabled" : "Disabled"} />
            </Field>
            <Field label={t("common.created")} mono>
              <LocalTime value={unit.createdAt} />
            </Field>
            <Field label={t("common.updated")} mono>
              <LocalTime value={unit.updatedAt} />
            </Field>
            <Field label={t("subOrganizations.unitId")} mono>
              <span className="break-all text-xs text-slate-500">
                {unit.subOrganizationId}
              </span>
            </Field>
          </FieldList>
        </CardContent>
      </Card>

      <div className="space-y-4">
        <TabBar tabs={tabs} activeTab={activeTab} onChange={setActiveTab} />

        {activeTab === "Contents" && (
          <div className="space-y-3">
            <UsageWarning
              usage={usage}
              unavailableNote="What this unit holds could not be read."
            />
            <p className="text-xs text-slate-400">
              {SUB_ORGANIZATION_OWNERSHIP_UNREADABLE}
            </p>
          </div>
        )}

        {activeTab === "History" && canReadHistory && (
          <HistoryPanel
            entityType="SubOrganization"
            path={historyPath.SubOrganization(
              organizationId,
              unit.subOrganizationId
            )}
            entityId={unit.subOrganizationId}
            onUnauthorized={onUnauthorized}
          />
        )}
      </div>

      <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("subOrganizations.deleteThisBusinessUnit")}</DialogTitle>
            <DialogDescription>
              Deleting a unit is allowed even while things are in it. Nothing is
              orphaned; everything in it goes back to belonging to the
              organisation as a whole, which means everybody homed here gets
              wider access than they have now.
            </DialogDescription>
          </DialogHeader>

          <UsageWarning
            usage={usage}
            unavailableNote="What this unit holds could not be read, so how much this widens is unknown."
          />

          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button variant="destructive" onClick={handleDelete}>
              {t("common.delete")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
