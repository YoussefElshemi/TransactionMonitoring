"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Eye, Plus, Trash2 } from "lucide-react";

import {
  SubOrganization,
  SubOrganizationResource,
  SubOrganizationUsage,
} from "@/types";
import { reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  readSubOrganization,
  subOrganizationsPath,
  SUB_ORGANIZATION_DESCRIPTION_MAX,
  SUB_ORGANIZATION_NAME_MAX,
} from "@/lib/roles";
import { usePermissions } from "@/context/PermissionsContext";
import Pagination from "@/components/Pagination";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import { UsageWarning } from "@/components/subOrganizations/SubOrganizationParts";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
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

const ANY_STATE = "All";

export function SubOrganizationsPanel({
  organizationId,
  detailHref,
  embedded = false,
  onUnauthorized,
}: {
  organizationId: string;
  detailHref: (subOrganizationId: string) => string;
  embedded?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const router = useRouter();
  const { holds } = usePermissions();
  const canWrite = holds("sub-organizations:write");

  const [units, setUnits] = useState<SubOrganization[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState("");
  const [state, setState] = useState(ANY_STATE);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [newName, setNewName] = useState("");
  const [newDescription, setNewDescription] = useState("");
  const [newReference, setNewReference] = useState("");

  const [deleteTarget, setDeleteTarget] = useState<{
    unit: SubOrganization;
    usage: SubOrganizationUsage | null;
  } | null>(null);

  const fetchUnits = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: String(pageSize),
      });
      if (searchTerm) query.set("Name", searchTerm);
      if (state !== ANY_STATE) query.set("IsEnabled", String(state === "Enabled"));

      const response = await apiFetch(
        `${subOrganizationsPath(organizationId)}?${query}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<SubOrganizationResource>(response);
        setUnits(page.data.map(readSubOrganization));
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("subOrganizations.errorOccurredFetchingBusinessUnits"),
          onUnauthorized,
          toastId: "error-fetching-units",
        });
      }
    } catch {
      toast.error(t("subOrganizations.errorOccurredFetchingBusinessUnits"), {
        id: "error-fetching-units",
      });
    } finally {
      setLoading(false);
    }
  }, [organizationId, pageNumber, pageSize, searchTerm, state, onUnauthorized]);

  useEffect(() => {
    fetchUnits();
  }, [fetchUnits]);

  const handleCreate = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(subOrganizationsPath(organizationId), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({
          name: newName.trim(),
          description: newDescription.trim() || null,
          externalReference: newReference.trim() || null,
          isEnabled: true,
        }),
      });

      if (response.ok) {
        const created = readSubOrganization(
          await unwrap<SubOrganizationResource>(response)
        );
        toast.success(t("common.namedCreated", created.name), {
          description:
            t("subOrganizations.nothingYetQueuesRuleSets"),
        });
        setCreateOpen(false);
        setNewName("");
        setNewDescription("");
        setNewReference("");
        router.push(detailHref(created.subOrganizationId));
      } else {
        await reportApiError(response, {
          fallback: t("subOrganizations.failedCreateBusinessUnit"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("subOrganizations.failedCreateBusinessUnit"));
    } finally {
      setSaving(false);
    }
  };

  const askToDelete = async (unit: SubOrganization) => {
    setDeleteTarget({ unit, usage: null });

    try {
      const response = await apiFetch(
        `${subOrganizationsPath(organizationId)}/${unit.subOrganizationId}/usages`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );
      if (response.ok) {
        setDeleteTarget({
          unit,
          usage: await unwrap<SubOrganizationUsage>(response),
        });
      }
    } catch {
    }
  };

  const handleDelete = async (unit: SubOrganization) => {
    try {
      const response = await apiFetch(
        `${subOrganizationsPath(organizationId)}/${unit.subOrganizationId}`,
        { method: "DELETE", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("common.namedDeleted", unit.name), {
          action: { label: t("common.undo"), onClick: () => handleRecover(unit) },
        });
        setDeleteTarget(null);
        fetchUnits();
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

  const handleRecover = async (unit: SubOrganization) => {
    try {
      const response = await apiFetch(
        `${subOrganizationsPath(organizationId)}/${unit.subOrganizationId}/recover`,
        { method: "POST", credentials: "include" }
      );

      if (response.ok) {
        toast.success(t("common.namedRestored", unit.name));
        fetchUnits();
      } else {
        await reportApiError(response, {
          fallback: t("subOrganizations.failedRecoverBusinessUnit"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("subOrganizations.failedRecoverBusinessUnit"));
    }
  };

  const createBlocked = saving || newName.trim().length === 0;

  const createButton = canWrite ? (
    <Button size="sm" className="gap-1.5" onClick={() => setCreateOpen(true)}>
      <Plus className="size-4" />
      {t("subOrganizations.newBusinessUnit")}
    </Button>
  ) : null;

  return (
    <div className="space-y-4">
      {!embedded && (
        <PageHeader
          eyebrow={t("subOrganizations.access")}
          title={t("subOrganizations.businessUnits")}
          description={t("subOrganizations.aPartitionInsideYourOrganisation")}
        >
          {createButton}
        </PageHeader>
      )}

      {embedded && (
        <p className="text-sm text-slate-500">
          A partition inside your organisation. Queues, rule sets and people can
          belong to one — and somebody given a home unit has every
          organisation-wide permission they hold narrowed to it, which is the
          part worth thinking about before creating them.
        </p>
      )}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Input
            value={searchTerm}
            onChange={(event) => {
              setSearchTerm(event.target.value);
              setPageNumber(1);
            }}
            placeholder={t("common.searchByName")}
            className="w-56"
          />
          <Select
            value={state}
            onValueChange={(value) => {
              setState(value);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ANY_STATE}>{t("subOrganizations.allUnits")}</SelectItem>
              <SelectItem value="Enabled">{t("common.enabled")}</SelectItem>
              <SelectItem value="Disabled">{t("common.disabled")}</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="flex items-center gap-3">
          <PageSizeSelect
            value={pageSize}
            onChange={(next) => {
              setPageSize(next);
              setPageNumber(1);
            }}
          />
          {embedded && createButton}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("common.name")}</TableHead>
              <TableHead>{t("common.description")}</TableHead>
              <TableHead className="w-48">{t("subOrganizations.externalReference")}</TableHead>
              <TableHead className="w-28">{t("common.status")}</TableHead>
              <TableHead className="w-32">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 5 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : units.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={5}
                  className="py-14 text-center text-sm text-slate-500"
                >
                  No business units. Everything belongs to the organisation as a
                  whole, which is a fine way to run one.
                </TableCell>
              </TableRow>
            ) : (
              units.map((unit) => (
                <TableRow
                  key={unit.subOrganizationId}
                  className="hover:bg-slate-50/50"
                >
                  <TableCell className="font-mono text-sm font-medium text-slate-900">
                    {unit.name}
                  </TableCell>
                  <TableCell className="max-w-md text-sm text-slate-600">
                    {unit.description ?? (
                      <span className="text-slate-300">—</span>
                    )}
                  </TableCell>
                  <TableCell className="font-mono text-xs text-slate-500">
                    {unit.externalReference ?? (
                      <span className="text-slate-300">—</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <StatusBadge
                      status={unit.isEnabled ? "Enabled" : "Disabled"}
                    />
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      <Button
                        variant="outline"
                        size="sm"
                        className="gap-1.5"
                        onClick={() =>
                          router.push(detailHref(unit.subOrganizationId))
                        }
                      >
                        <Eye className="size-4" />
                        {t("common.view")}
                      </Button>
                      {canWrite && (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-critical-500 hover:text-critical-700"
                          title={t("common.delete")}
                          aria-label={t("common.delete")}
                          onClick={() => askToDelete(unit)}
                        >
                          <Trash2 className="size-4" />
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {units.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("subOrganizations.newBusinessUnit2")}</DialogTitle>
            <DialogDescription>
              A container for queues, rule sets and people. Creating one changes
              nothing on its own — moving things into it is what narrows access.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.name")}
              </label>
              <Input
                value={newName}
                onChange={(event) => setNewName(event.target.value)}
                placeholder={t("subOrganizations.cards")}
                maxLength={SUB_ORGANIZATION_NAME_MAX}
                className="font-mono"
              />
              <p className="text-xs text-slate-500">
                {t("subOrganizations.uniqueInYourOrganisation")}
              </p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.description")}
              </label>
              <Textarea
                value={newDescription}
                onChange={(event) => setNewDescription(event.target.value)}
                placeholder={t("subOrganizations.cardAcquiringIncludingTheDisputes")}
                maxLength={SUB_ORGANIZATION_DESCRIPTION_MAX}
                rows={2}
              />
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("subOrganizations.externalReference")}
              </label>
              <Input
                value={newReference}
                onChange={(event) => setNewReference(event.target.value)}
                placeholder="BU-0041"
                className="font-mono"
              />
              <p className="text-xs text-slate-500">
                This unit&apos;s id in whatever system you already run. Carried
                and never interpreted — nothing here routes on it.
              </p>
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setCreateOpen(false)}
              disabled={saving}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={handleCreate} disabled={createBlocked}>
              {saving ? "Creating..." : "Create"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={deleteTarget !== null}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("subOrganizations.deleteThisBusinessUnit")}</DialogTitle>
            <DialogDescription>
              Deleting a unit is allowed even while things are in it. Nothing is
              orphaned; everything in it goes back to belonging to the
              organisation as a whole, which means everybody homed here gets
              *wider* access than they have now.
            </DialogDescription>
          </DialogHeader>

          {deleteTarget && (
            <UsageWarning
              usage={deleteTarget.usage}
              unavailableNote="What this unit holds could not be read, so how much this widens is unknown."
            />
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)}>
              {t("common.cancel")}
            </Button>
            {deleteTarget && (
              <Button
                variant="destructive"
                onClick={() => handleDelete(deleteTarget.unit)}
              >
                {t("common.delete")}
              </Button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
