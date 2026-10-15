"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { CircleAlert, CircleCheck, Pencil, Plus, X } from "lucide-react";

import type {
  MaintenanceStatus,
  MaintenanceWindow,
  ServiceComponent,
} from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { canOperateStatus } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import {
  CANCELLING_KEEPS_THE_RECORD,
  CREATING_IS_ANNOUNCING,
  DESCRIPTION_MAX_LENGTH,
  MAINTENANCE_STATUSES,
  MAINTENANCE_STATUS_LABELS,
  MOVING_REDATES_THE_ANNOUNCEMENT,
  TITLE_MAX_LENGTH,
  canCancel,
  canEdit,
  editRefusal,
  isProperlyAnnounced,
  maintenanceRefusal,
  noticeSentence,
  plannedNotice,
  soonestFirst,
  windowLength,
} from "@/lib/maintenanceWindows";
import { SERVICE_COMPONENTS, componentLabel, componentsSentence } from "@/lib/status";
import { MaintenanceStatusChip } from "@/components/status/StatusChips";
import { ComponentPicker } from "@/components/status/ComponentPicker";
import { LocalTime } from "@/components/LocalTime";
import { PageHeader } from "@/components/PageHeader";
import Pagination from "@/components/Pagination";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { DateTimePicker } from "@/components/DateTimePicker";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
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
import { useTranslation } from "@/context/LocalizationContext";

const MaintenanceWindows = () => {
  const t = useTranslation();
  const { allowed } = useRequirePermission("maintenance-windows:read", "/");

  const router = useRouter();
  const { grants } = usePermissions();
  const submission = useIdempotencyKey();

  const canWrite = canOperateStatus(grants, "maintenance-windows:write");

  const [windows, setWindows] = useState<MaintenanceWindow[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [componentFilter, setComponentFilter] = useState<
    ServiceComponent | "any"
  >("any");
  const [statusFilter, setStatusFilter] = useState<MaintenanceStatus | "any">(
    "any"
  );
  const [includePast, setIncludePast] = useState(false);

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<MaintenanceWindow | null>(null);
  const [cancelling, setCancelling] = useState<MaintenanceWindow | null>(null);
  const [saving, setSaving] = useState(false);

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [components, setComponents] = useState<ServiceComponent[]>([]);
  const [scheduledStart, setScheduledStart] = useState("");
  const [scheduledEnd, setScheduledEnd] = useState("");

  const fetchWindows = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/maintenance-windows?PageNumber=${pageNumber}&PageSize=20` +
          (componentFilter === "any" ? "" : `&Component=${componentFilter}`) +
          (statusFilter === "any" ? "" : `&Status=${statusFilter}`) +
          `&IncludePast=${includePast}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<MaintenanceWindow>(response);
        setWindows(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("maintenanceWindows.errorOccurredFetchingMaintenanceWindows"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-fetching-maintenance",
        });
      }
    } catch {
      toast.error(t("maintenanceWindows.errorOccurredFetchingMaintenanceWindows"), {
        id: "error-fetching-maintenance",
      });
    } finally {
      setLoading(false);
    }
  }, [pageNumber, componentFilter, statusFilter, includePast, router]);

  useEffect(() => {
    if (allowed) fetchWindows();
  }, [allowed, fetchWindows]);

  const startCreate = () => {
    setEditing(null);
    setTitle("");
    setDescription("");
    setComponents([]);
    setScheduledStart("");
    setScheduledEnd("");
    setFormOpen(true);
  };

  const startEdit = (window: MaintenanceWindow) => {
    setEditing(window);
    setTitle(window.title);
    setDescription(window.description);
    setComponents(window.components);
    // Sliced off the ISO string rather than reformatted, so the box holds the
    // instant the record does rather than one round-tripped through a zone.
    setScheduledStart(window.scheduledStart.slice(0, 16));
    setScheduledEnd(window.scheduledEnd.slice(0, 16));
    setFormOpen(true);
  };

  const refusal = maintenanceRefusal({
    title,
    description,
    components,
    scheduledStart,
    scheduledEnd,
  });

  const notice = plannedNotice(scheduledStart);

  const save = async () => {
    setSaving(true);

    const intent = editing === null ? "create" : `edit-${editing.maintenanceWindowId}`;

    try {
      const response = await apiFetch(
        editing === null
          ? `${API_BASE}/maintenance-windows`
          : `${API_BASE}/maintenance-windows/${editing.maintenanceWindowId}`,
        {
          method: editing === null ? "POST" : "PUT",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            title: title.trim(),
            description: description.trim(),
            components,
            scheduledStart: new Date(scheduledStart).toISOString(),
            scheduledEnd: new Date(scheduledEnd).toISOString(),
          }),
        },
        { idempotencyKey: submission.current(intent) }
      );

      if (response.ok) {
        const saved = await unwrap<MaintenanceWindow>(response);
        submission.completed(intent);
        toast.success(
          editing === null ? t("maintenanceWindows.maintenanceAnnounced") : t("maintenanceWindows.maintenanceMoved"),
          {
            description:
              isProperlyAnnounced(saved) === false
                ? t("maintenanceWindows.noteUnderNoticeMinimumSo")
                : t("maintenanceWindows.everyConfirmedSubscriberHasBeen"),
          }
        );
        setFormOpen(false);
        fetchWindows();
      } else {
        await reportApiError(response, {
          fallback: t("maintenanceWindows.errorOccurredSavingMaintenanceWindow"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-saving-maintenance",
        });
      }
    } catch {
      toast.error(t("maintenanceWindows.errorOccurredSavingMaintenanceWindow"), {
        id: "error-saving-maintenance",
      });
    } finally {
      setSaving(false);
    }
  };

  const cancel = async () => {
    if (cancelling === null) return;

    setSaving(true);
    const intent = `cancel-${cancelling.maintenanceWindowId}`;

    try {
      const response = await apiFetch(
        `${API_BASE}/maintenance-windows/${cancelling.maintenanceWindowId}`,
        {
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: submission.current(intent) }
      );

      if (response.ok) {
        submission.completed(intent);
        toast.success(t("maintenanceWindows.maintenanceCancelled"), {
          description:
            t("maintenanceWindows.staysRecordSomethingAnnouncedThen"),
        });
        setCancelling(null);
        fetchWindows();
      } else {
        await reportApiError(response, {
          fallback: t("maintenanceWindows.errorOccurredCancellingWindow"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-cancelling-maintenance",
        });
      }
    } catch {
      toast.error(t("maintenanceWindows.errorOccurredCancellingWindow"), {
        id: "error-cancelling-maintenance",
      });
    } finally {
      setSaving(false);
    }
  };

  if (!allowed) return null;

  return (
    <div>
      <PageHeader
        eyebrow={t("maintenanceWindows.platform")}
        title={t("maintenanceWindows.maintenanceWindows")}
        description={t("maintenanceWindows.downtimeChosenInAdvanceAnd")}
      >
        {canWrite && (
          <Button onClick={startCreate} className="gap-1.5">
            <Plus className="size-4" />
            {t("maintenanceWindows.scheduleMaintenance")}
          </Button>
        )}
      </PageHeader>

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <Select
          value={componentFilter}
          onValueChange={(value) => {
            setComponentFilter(value as ServiceComponent | "any");
            setPageNumber(1);
          }}
        >
          <SelectTrigger className="w-56">
            <SelectValue placeholder={t("filter.anyComponent")} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t("filter.anyComponent")}</SelectItem>
            {SERVICE_COMPONENTS.map((component) => (
              <SelectItem key={component} value={component}>
                {componentLabel(component)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Select
          value={statusFilter}
          onValueChange={(value) => {
            setStatusFilter(value as MaintenanceStatus | "any");
            setPageNumber(1);
          }}
        >
          <SelectTrigger className="w-48">
            <SelectValue placeholder={t("filter.anyStatus")} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t("filter.anyStatus")}</SelectItem>
            {MAINTENANCE_STATUSES.map((status) => (
              <SelectItem key={status} value={status}>
                {MAINTENANCE_STATUS_LABELS[status]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <label className="flex items-center gap-2 text-sm text-slate-600">
          <input
            type="checkbox"
            checked={includePast}
            onChange={(event) => {
              setIncludePast(event.target.checked);
              setPageNumber(1);
            }}
            className="size-4 rounded border-slate-300 accent-brand-800"
          />
          {t("maintenanceWindows.includePastWindows")}
        </label>
      </div>

      {loading ? (
        <Skeleton className="h-96 w-full rounded-lg" />
      ) : windows.length === 0 ? (
        <div className="rounded-lg border border-slate-200 bg-card px-6 py-16 text-center">
          <p className="font-mono text-sm text-slate-500">
            {t("maintenanceWindows.nothingIsScheduled")}
          </p>
          <p className="mx-auto mt-2 max-w-md text-sm leading-relaxed text-slate-400">
            {includePast
              ? "No window matches these filters."
              : "No maintenance is announced. Past windows are hidden — turn on “Include past windows” to see the history."}
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {soonestFirst(windows).map((window) => (
            <WindowCard
              key={window.maintenanceWindowId}
              window={window}
              canWrite={canWrite}
              onEdit={() => startEdit(window)}
              onCancel={() => setCancelling(window)}
            />
          ))}
        </div>
      )}

      {totalPages > 1 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      <Dialog open={formOpen} onOpenChange={setFormOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>
              {editing === null
                ? "Schedule maintenance"
                : "Move this maintenance window"}
            </DialogTitle>
            <DialogDescription>
              {editing === null
                ? CREATING_IS_ANNOUNCING
                : MOVING_REDATES_THE_ANNOUNCEMENT}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("maintenanceWindows.title")}
              </label>
              <Input
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                maxLength={TITLE_MAX_LENGTH}
                placeholder={t("maintenanceWindows.databaseUpgrade")}
                className="mt-1.5"
              />
            </div>

            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.description")}
              </label>
              <Textarea
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                maxLength={DESCRIPTION_MAX_LENGTH}
                rows={4}
                placeholder={t("maintenanceWindows.whatIsBeingDoneAnd")}
                className="mt-1.5"
              />
            </div>

            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.components")}
              </label>
              <ComponentPicker
                selected={components}
                onChange={setComponents}
                className="mt-1.5"
              />
            </div>

            <div className="flex flex-wrap gap-4">
              <div>
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.starts")}
                </label>
                <DateTimePicker
                  value={scheduledStart}
                  onChange={setScheduledStart}
                  placeholder={t("maintenanceWindows.selectStartDateAndTime")}
                  aria-label={t("maintenanceWindows.maintenanceWindowStarts")}
                  className="mt-1.5 w-60"
                />
              </div>
              <div>
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("maintenanceWindows.ends")}
                </label>
                <DateTimePicker
                  value={scheduledEnd}
                  onChange={setScheduledEnd}
                  placeholder={t("maintenanceWindows.selectEndDateAndTime")}
                  aria-label={t("maintenanceWindows.maintenanceWindowEnds")}
                  className="mt-1.5 w-60"
                />
              </div>
            </div>

            {notice !== null && (
              <p
                className={
                  notice.clears
                    ? "rounded-md border border-settled-200 bg-settled-50 px-3 py-2 text-xs leading-relaxed text-settled-700"
                    : "rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs leading-relaxed text-attention-700"
                }
              >
                {notice.sentence}
              </p>
            )}

            {refusal !== null && title.length + description.length > 0 && (
              <p className="text-sm leading-relaxed text-critical-700">
                {refusal}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setFormOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button onClick={save} disabled={refusal !== null || saving}>
              {saving
                ? "Saving…"
                : editing === null
                  ? "Announce"
                  : "Save and re-announce"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={cancelling !== null}
        onOpenChange={(open) => !open && setCancelling(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("maintenanceWindows.cancelThisMaintenanceWindow")}</DialogTitle>
            <DialogDescription>
              {CANCELLING_KEEPS_THE_RECORD}
            </DialogDescription>
          </DialogHeader>
          {cancelling && (
            <p className="text-sm leading-relaxed text-slate-600">
              <span className="font-medium">{cancelling.title}</span>,
              scheduled for <LocalTime value={cancelling.scheduledStart} />,
              affecting {componentsSentence(cancelling.components)}.
            </p>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setCancelling(null)}>
              {t("common.keepIt")}
            </Button>
            <Button variant="destructive" onClick={cancel} disabled={saving}>
              {saving ? "Cancelling…" : "Cancel the window"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

function WindowCard({
  window,
  canWrite,
  onEdit,
  onCancel,
}: {
  window: MaintenanceWindow;
  canWrite: boolean;
  onEdit: () => void;
  onCancel: () => void;
}) {
  const t = useTranslation();
  const announced = isProperlyAnnounced(window);
  const blocked = editRefusal(window);

  return (
    <Card>
      <CardContent className="space-y-3 p-6">
        <div className="flex flex-wrap items-start justify-between gap-x-4 gap-y-2">
          <div className="min-w-0">
            <h3 className="font-mono text-base font-medium tracking-tight text-slate-900">
              {window.title}
            </h3>
            <p className="mt-1 max-w-2xl text-sm leading-relaxed text-slate-600">
              {window.description}
            </p>
          </div>
          <div className="flex flex-shrink-0 items-center gap-2">
            <MaintenanceStatusChip status={window.status} />
            {canWrite && canEdit(window) && (
              <>
                <Button
                  variant="outline"
                  size="sm"
                  className="gap-1.5"
                  onClick={onEdit}
                >
                  <Pencil className="size-3.5" />
                  {t("maintenanceWindows.move")}
                </Button>
                {canCancel(window) && (
                  <Button
                    variant="outline"
                    size="sm"
                    className="gap-1.5"
                    onClick={onCancel}
                  >
                    <X className="size-3.5" />
                    {t("common.cancel")}
                  </Button>
                )}
              </>
            )}
          </div>
        </div>

        <div className="flex flex-wrap gap-x-6 gap-y-1 font-mono text-[11px] uppercase tracking-widest text-slate-400">
          <span>
            {t(
              "maintenanceWindows.affecting",
              componentsSentence(window.components)
            )}
          </span>
          <span>
            <LocalTime value={window.scheduledStart} /> —{" "}
            <LocalTime value={window.scheduledEnd} />
          </span>
          <span>{windowLength(window)}</span>
        </div>

        <div
          className={
            announced === false
              ? "flex items-start gap-2 rounded-md border border-attention-200 bg-attention-50 px-3 py-2"
              : "flex items-start gap-2"
          }
        >
          {announced === true && (
            <CircleCheck className="mt-0.5 size-3.5 flex-shrink-0 text-settled-700" />
          )}
          {announced === false && (
            <CircleAlert className="mt-0.5 size-3.5 flex-shrink-0 text-attention-700" />
          )}
          <p
            className={
              announced === false
                ? "text-xs leading-relaxed text-attention-700"
                : "text-xs leading-relaxed text-slate-500"
            }
          >
            {noticeSentence(window)}
          </p>
        </div>

        {canWrite && blocked !== null && (
          <p className="text-xs leading-relaxed text-slate-400">{blocked}</p>
        )}
      </CardContent>
    </Card>
  );
}

export default MaintenanceWindows;
