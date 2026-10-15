"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { TriangleAlert } from "lucide-react";

import { API_BASE, reportApiError } from "@/lib/api";
import { useSubOrganizations } from "@/lib/roles";
import { usePermissions } from "@/context/PermissionsContext";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const NO_UNIT = "__none__";

export function HomeSubOrganizationControl({
  organizationId,
  userId,
  current,
  onChanged,
  onUnauthorized,
}: {
  organizationId: string;
  userId: string;
  current: string | null;
  onChanged: (subOrganizationId: string | null) => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { holds } = usePermissions();
  const canWrite = holds("sub-organizations:write");

  const units = useSubOrganizations(organizationId, holds("sub-organizations:read"));
  const [chosen, setChosen] = useState(current ?? NO_UNIT);
  const [confirming, setConfirming] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setChosen(current ?? NO_UNIT);
  }, [current, userId]);

  const target = chosen === NO_UNIT ? null : chosen;
  const changed = target !== current;
  const narrowing = target !== null;

  const currentName =
    units.data.find((unit) => unit.subOrganizationId === current)?.name ?? null;
  const targetName =
    units.data.find((unit) => unit.subOrganizationId === target)?.name ?? null;

  const handleSave = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/users/${userId}/sub-organization`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ subOrganizationId: target }),
        }
      );

      if (response.ok) {
        toast.success(
          narrowing
            ? t(
                "subOrganizations.movedInto",
                targetName ?? t("subOrganizations.theUnit")
              )
            : t("subOrganizations.takenOutUnit"),
          {
            description: narrowing
              ? t("subOrganizations.everyOrganisationWidePermissionThey")
              : t("subOrganizations.everyPermissionTheyHoldNow"),
          }
        );
        setConfirming(false);
        onChanged(target);
      } else {
        await reportApiError(response, {
          fallback: t("subOrganizations.failedChangeHomeBusinessUnit"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("subOrganizations.failedChangeHomeBusinessUnit"));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Card>
      <CardContent className="space-y-4 pt-6">
        <div>
          <h3 className="font-mono text-sm font-medium tracking-tight text-slate-900">
            {t("subOrganizations.homeBusinessUnit")}
          </h3>
          <p className="mt-1 max-w-2xl text-sm text-slate-500">
            Where this person sits. A home unit narrows every organisation-wide
            permission they hold to that unit — this is a stronger control than
            any single role, because it applies to grants somebody else made.
          </p>
        </div>

        {units.unavailable ? (
          <p className="text-sm text-slate-500">
            The business units could not be read, so there is nothing to choose
            between. That needs{" "}
            <span className="font-mono">sub-organizations:read</span>.
          </p>
        ) : units.data.length === 0 && !current ? (
          <p className="text-sm text-slate-500">
            Your organisation has no business units, so everybody&apos;s
            permissions reach all of it.
          </p>
        ) : (
          <div className="flex flex-wrap items-end gap-3">
            <div className="min-w-56 space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("subOrganizations.unit")}
              </label>
              <Select
                value={chosen}
                onValueChange={setChosen}
                disabled={!canWrite || saving}
              >
                <SelectTrigger className="w-64">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NO_UNIT}>
                    {t("subOrganizations.theWholeOrganisation")}
                  </SelectItem>
                  {units.data.map((unit) => (
                    <SelectItem
                      key={unit.subOrganizationId}
                      value={unit.subOrganizationId}
                    >
                      {unit.name}
                      {!unit.isEnabled && (
                        <span className="ml-2 font-mono text-[10px] uppercase tracking-wider text-slate-400">
                          disabled
                        </span>
                      )}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            {canWrite && (
              <Button
                variant={narrowing ? "default" : "secondary"}
                disabled={!changed || saving}
                onClick={() => setConfirming(true)}
              >
                {changed ? "Apply" : "No change"}
              </Button>
            )}
          </div>
        )}

        {units.truncated && (
          <p className="text-xs text-slate-500">
            Showing the first {units.data.length}
            {units.totalCount === null ? "" : ` of ${units.totalCount}`} units.
            If the one you want is missing, it is past the first page.
          </p>
        )}

        {!canWrite && (
          <p className="text-xs text-slate-500">
            Changing this needs{" "}
            <span className="font-mono">sub-organizations:write</span>.
          </p>
        )}
      </CardContent>

      <Dialog open={confirming} onOpenChange={setConfirming}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {narrowing
                ? `Narrow this account to ${targetName ?? "one unit"}?`
                : "Give this account the whole organisation?"}
            </DialogTitle>
            <DialogDescription>
              {narrowing
                ? "Every organisation-wide permission this person holds will be narrowed to that unit — including grants made by somebody else, for reasons that have nothing to do with it. They also lose platform:administer entirely, whatever role they are in."
                : `Every permission this person holds will reach the whole organisation again${
                    currentName ? `, not just ${currentName}` : ""
                  }. This is a widening, not an undo: nothing checks whether their grants were made on the assumption that they were partitioned.`}
            </DialogDescription>
          </DialogHeader>

          {!narrowing && (
            <div className="flex gap-2.5 rounded-md border border-attention-200 bg-attention-50 px-3 py-2.5 ring-1 ring-inset ring-attention-200">
              <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
              <p className="text-sm text-slate-600">
                Check their grants afterwards. Somebody granted a broad role
                while they were partitioned may now hold far more than whoever
                granted it intended.
              </p>
            </div>
          )}

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setConfirming(false)}
              disabled={saving}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={handleSave} disabled={saving}>
              {saving
                ? "Applying..."
                : narrowing
                  ? "Narrow to the unit"
                  : "Widen to the organisation"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
