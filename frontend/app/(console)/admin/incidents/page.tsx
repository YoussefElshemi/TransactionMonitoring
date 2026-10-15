"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Plus } from "lucide-react";

import type {
  Incident,
  IncidentImpact,
  IncidentStatus,
  ServiceComponent,
} from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { canOperateStatus } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import {
  INCIDENT_IMPACTS,
  INCIDENT_IMPACT_DESCRIPTIONS,
  INCIDENT_STATUSES,
  STARTED_AT_HINT,
  SUMMARY_MAX_LENGTH,
  TITLE_MAX_LENGTH,
  durationSentence,
  formatDuration,
  incidentDurationMs,
  incidentRefusal,
} from "@/lib/incidents";
import { SERVICE_COMPONENTS, componentLabel, componentsSentence } from "@/lib/status";
import {
  IncidentImpactChip,
  IncidentStatusChip,
} from "@/components/status/StatusChips";
import { ComponentPicker } from "@/components/status/ComponentPicker";
import { LocalTime } from "@/components/LocalTime";
import { PageHeader } from "@/components/PageHeader";
import Pagination from "@/components/Pagination";
import { Button } from "@/components/ui/button";
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
import { useTranslation } from "@/context/LocalizationContext";

const Incidents = () => {
  const t = useTranslation();
  const { allowed } = useRequirePermission("incidents:read", "/");

  const router = useRouter();
  const { grants } = usePermissions();
  const submission = useIdempotencyKey();

  const canOpen = canOperateStatus(grants, "incidents:write");

  const [incidents, setIncidents] = useState<Incident[]>([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [componentFilter, setComponentFilter] = useState<
    ServiceComponent | "any"
  >("any");
  const [statusFilter, setStatusFilter] = useState<IncidentStatus | "any">(
    "any"
  );
  const [includeResolved, setIncludeResolved] = useState(false);

  const [openDialog, setOpenDialog] = useState(false);
  const [saving, setSaving] = useState(false);

  const [title, setTitle] = useState("");
  const [summary, setSummary] = useState("");
  const [impact, setImpact] = useState<IncidentImpact>("Major");
  const [components, setComponents] = useState<ServiceComponent[]>([]);
  const [startedAt, setStartedAt] = useState("");

  const fetchIncidents = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/incidents?PageNumber=${pageNumber}&PageSize=20` +
          (componentFilter === "any" ? "" : `&Component=${componentFilter}`) +
          (statusFilter === "any" ? "" : `&Status=${statusFilter}`) +
          `&IncludeResolved=${includeResolved}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<Incident>(response);
        setIncidents(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("incidents.errorOccurredFetchingIncidents"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-fetching-incidents",
        });
      }
    } catch {
      toast.error(t("incidents.errorOccurredFetchingIncidents"), {
        id: "error-fetching-incidents",
      });
    } finally {
      setLoading(false);
    }
  }, [pageNumber, componentFilter, statusFilter, includeResolved, router]);

  useEffect(() => {
    if (allowed) fetchIncidents();
  }, [allowed, fetchIncidents]);

  const refusal = incidentRefusal({ title, summary, components });

  const open = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/incidents`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            title: title.trim(),
            summary: summary.trim(),
            impact,
            components,
            ...(startedAt.trim().length > 0
              ? { startedAt: new Date(startedAt).toISOString() }
              : {}),
          }),
        },
        { idempotencyKey: submission.current("open") }
      );

      if (response.ok) {
        const created = await unwrap<Incident>(response);
        submission.completed("open");
        toast.success(t("incidents.incidentOpened", created.incidentNumber), {
          description:
            t("incidents.publicStatusPageNowEvery"),
        });
        setOpenDialog(false);
        setTitle("");
        setSummary("");
        setComponents([]);
        setStartedAt("");
        router.push(`/admin/incidents/${created.incidentId}`);
      } else {
        await reportApiError(response, {
          fallback: t("incidents.errorOccurredOpeningIncident"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-opening-incident",
        });
      }
    } catch {
      toast.error(t("incidents.errorOccurredOpeningIncident"), {
        id: "error-opening-incident",
      });
    } finally {
      setSaving(false);
    }
  };

  if (!allowed) return null;

  return (
    <div>
      <PageHeader
        eyebrow={t("incidents.platform")}
        title={t("incidents.incidents")}
        description={t("incidents.whatThePlatformHasTold")}
      >
        {canOpen && (
          <Button onClick={() => setOpenDialog(true)} className="gap-1.5">
            <Plus className="size-4" />
            {t("incidents.openIncident")}
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
            setStatusFilter(value as IncidentStatus | "any");
            setPageNumber(1);
          }}
        >
          <SelectTrigger className="w-48">
            <SelectValue placeholder={t("filter.anyStatus")} />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="any">{t("filter.anyStatus")}</SelectItem>
            {INCIDENT_STATUSES.map((status) => (
              <SelectItem key={status} value={status}>
                {status}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <label className="flex items-center gap-2 text-sm text-slate-600">
          <input
            type="checkbox"
            checked={includeResolved}
            onChange={(event) => {
              setIncludeResolved(event.target.checked);
              setPageNumber(1);
            }}
            className="size-4 rounded border-slate-300 accent-brand-800"
          />
          {t("incidents.includeResolved")}
        </label>
      </div>

      {loading ? (
        <Skeleton className="h-96 w-full rounded-lg" />
      ) : incidents.length === 0 ? (
        <div className="rounded-lg border border-slate-200 bg-card px-6 py-16 text-center">
          <p className="font-mono text-sm text-slate-500">
            {includeResolved
              ? "No incidents match these filters."
              : "Nothing is open."}
          </p>
          <p className="mx-auto mt-2 max-w-md text-sm leading-relaxed text-slate-400">
            {includeResolved
              ? "Try widening the component or status filter."
              : "No incident is currently being told to customers. Resolved ones are hidden — turn on “Include resolved” to see the history."}
          </p>
        </div>
      ) : (
        <div className="overflow-hidden rounded-lg border border-slate-200 bg-card">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t("incidents.incident")}</TableHead>
                <TableHead>{t("common.status")}</TableHead>
                <TableHead>{t("incidents.impact")}</TableHead>
                <TableHead>{t("common.components")}</TableHead>
                <TableHead>{t("common.started")}</TableHead>
                <TableHead>{t("incidents.duration")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {incidents.map((incident) => (
                <TableRow key={incident.incidentId}>
                  <TableCell className="max-w-md">
                    <Link
                      href={`/admin/incidents/${incident.incidentId}`}
                      className="font-mono text-xs text-brand-800 underline-offset-4 hover:underline"
                    >
                      {incident.incidentNumber}
                    </Link>
                    <p className="mt-0.5 truncate text-sm text-slate-700">
                      {incident.title}
                    </p>
                  </TableCell>
                  <TableCell>
                    <IncidentStatusChip status={incident.status} />
                  </TableCell>
                  <TableCell>
                    <IncidentImpactChip impact={incident.impact} />
                  </TableCell>
                  <TableCell className="text-sm text-slate-600">
                    {componentsSentence(incident.components)}
                  </TableCell>
                  <TableCell className="font-mono text-xs text-slate-500">
                    <LocalTime value={incident.startedAt} />
                  </TableCell>
                  <TableCell
                    className="text-sm text-slate-600"
                    title={durationSentence(incident)}
                  >
                    {formatDuration(incidentDurationMs(incident))}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
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

      <Dialog open={openDialog} onOpenChange={setOpenDialog}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("incidents.openAnIncident")}</DialogTitle>
            <DialogDescription>
              This appears on the public status page immediately and is mailed
              to every confirmed subscriber. It cannot be withdrawn — an
              incident opened in error is resolved and explained, not deleted.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("incidents.title")}
              </label>
              <Input
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                maxLength={TITLE_MAX_LENGTH}
                placeholder={t("incidents.elevatedErrorRatesOnRule")}
                className="mt-1.5"
              />
            </div>

            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("incidents.summary")}
              </label>
              <Textarea
                value={summary}
                onChange={(event) => setSummary(event.target.value)}
                maxLength={SUMMARY_MAX_LENGTH}
                rows={4}
                placeholder={t("incidents.whatACustomerNeedsTo")}
                className="mt-1.5"
              />
              <p className="mt-1.5 text-xs leading-relaxed text-slate-500">
                This is what a customer reads first, and it becomes the opening
                entry on the public timeline.
              </p>
            </div>

            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("incidents.impact")}
              </label>
              <Select
                value={impact}
                onValueChange={(value) => setImpact(value as IncidentImpact)}
              >
                <SelectTrigger className="mt-1.5 w-64">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {INCIDENT_IMPACTS.map((option) => (
                    <SelectItem key={option} value={option}>
                      {option}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="mt-1.5 text-xs leading-relaxed text-slate-500">
                {INCIDENT_IMPACT_DESCRIPTIONS[impact]}
              </p>
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

            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("incidents.startedAt")}
              </label>
              <DateTimePicker
                value={startedAt}
                onChange={setStartedAt}
                placeholder={t("incidents.useCurrentTime")}
                aria-label={t("incidents.incidentStartedAt")}
                className="mt-1.5 w-64"
              />
              <p className="mt-1.5 max-w-xl text-xs leading-relaxed text-slate-500">
                {STARTED_AT_HINT}
              </p>
            </div>

            {refusal !== null && title.length + summary.length > 0 && (
              <p className="text-sm leading-relaxed text-critical-700">
                {refusal}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setOpenDialog(false)}>
              {t("common.cancel")}
            </Button>
            <Button onClick={open} disabled={refusal !== null || saving}>
              {saving ? "Opening…" : "Open incident"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default Incidents;
