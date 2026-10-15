"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { toast } from "sonner";
import { ArrowLeft, FileText, Pencil, Send } from "lucide-react";

import type {
  Incident,
  IncidentImpact,
  IncidentStatus,
  ServiceComponent,
} from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { canOperateStatus } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import {
  EDIT_CORRECTS_THE_ACCOUNT,
  INCIDENT_IMPACTS,
  INCIDENT_IMPACT_DESCRIPTIONS,
  INCIDENT_STATUSES,
  INCIDENT_STATUS_DESCRIPTIONS,
  RESOLVED_ENDS_IT,
  REVIEW_IS_PUBLIC,
  REVIEW_MAX_LENGTH,
  SUMMARY_MAX_LENGTH,
  TITLE_MAX_LENGTH,
  UPDATE_BODY_MAX_LENGTH,
  UPDATE_IS_PUBLISHED,
  durationSentence,
  hasPublishedReview,
  incidentRefusal,
  reviewContentRefusal,
  reviewRefusal,
  timelineNewestFirst,
  updateRefusal,
} from "@/lib/incidents";
import { componentsSentence } from "@/lib/status";
import {
  IncidentImpactChip,
  IncidentStatusChip,
} from "@/components/status/StatusChips";
import { ComponentPicker } from "@/components/status/ComponentPicker";
import { LocalTime } from "@/components/LocalTime";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { Field, FieldList } from "@/components/FieldList";
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

const IncidentDetail = () => {
  const t = useTranslation();
  const params = useParams<{ incidentId: string }>();
  const incidentId = params?.incidentId ?? "";

  const { allowed } = useRequirePermission("incidents:read", "/");

  const router = useRouter();
  const { grants } = usePermissions();
  const submission = useIdempotencyKey();

  const canWrite = canOperateStatus(grants, "incidents:write");

  const [incident, setIncident] = useState<Incident | null>(null);
  const [loading, setLoading] = useState(true);
  const [missing, setMissing] = useState(false);

  const [updateOpen, setUpdateOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [reviewOpen, setReviewOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const [updateStatus, setUpdateStatus] = useState<IncidentStatus>("Identified");
  const [updateBody, setUpdateBody] = useState("");

  const [editTitle, setEditTitle] = useState("");
  const [editSummary, setEditSummary] = useState("");
  const [editImpact, setEditImpact] = useState<IncidentImpact>("Major");
  const [editComponents, setEditComponents] = useState<ServiceComponent[]>([]);
  const [editStartedAt, setEditStartedAt] = useState("");

  const [rootCause, setRootCause] = useState("");
  const [remediation, setRemediation] = useState("");

  const fetchIncident = useCallback(async () => {
    setLoading(true);

    try {
      const response = await apiFetch(`${API_BASE}/incidents/${incidentId}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        setIncident(await unwrap<Incident>(response));
      } else if (response.status === 404) {
        setMissing(true);
      } else {
        await reportApiError(response, {
          fallback: t("incidents.errorOccurredFetchingIncident"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: "error-fetching-incident",
        });
      }
    } catch {
      toast.error(t("incidents.errorOccurredFetchingIncident"), {
        id: "error-fetching-incident",
      });
    } finally {
      setLoading(false);
    }
  }, [incidentId, router]);

  useEffect(() => {
    if (allowed && incidentId) fetchIncident();
  }, [allowed, incidentId, fetchIncident]);

  const mutate = async (
    intent: string,
    path: string,
    method: "POST" | "PUT",
    body: unknown,
    success: { title: string; description?: string },
    onDone?: () => void
  ) => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}${path}`,
        {
          method,
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(body),
        },
        { idempotencyKey: submission.current(intent) }
      );

      if (response.ok) {
        setIncident(await unwrap<Incident>(response));
        submission.completed(intent);
        toast.success(success.title, { description: success.description });
        onDone?.();
      } else {
        await reportApiError(response, {
          fallback: t("incidents.errorOccurredPerformingAction"),
          onUnauthorized: () => router.push("/auth/login"),
          toastId: `error-incident-${intent}`,
        });
      }
    } catch {
      toast.error(t("incidents.errorOccurredPerformingAction"), {
        id: `error-incident-${intent}`,
      });
    } finally {
      setSaving(false);
    }
  };

  const openEdit = () => {
    if (incident === null) return;

    setEditTitle(incident.title);
    setEditSummary(incident.summary);
    setEditImpact(incident.impact);
    setEditComponents(incident.components);
    // `datetime-local` wants `YYYY-MM-DDTHH:mm` with no zone. Sliced off the
    // ISO string rather than reformatted, so the box shows the instant the
    // record holds rather than one shifted into the reader's zone and back.
    setEditStartedAt(incident.startedAt.slice(0, 16));
    setEditOpen(true);
  };

  if (!allowed) return null;

  if (missing) {
    return (
      <NotFound
        title={t("incidents.thisIncidentDoesnTExist")}
        description={t("incidents.itMayHaveBeenOpened")}
        backHref="/admin/incidents"
        backLabel={t("incidents.backToIncidents")}
      />
    );
  }

  if (loading || incident === null) {
    return <Skeleton className="h-96 w-full rounded-lg" />;
  }

  const timeline = timelineNewestFirst(incident.updates);
  const reviewGate = reviewRefusal(incident);
  const updateGate = updateRefusal(updateBody);
  const editGate = incidentRefusal({
    title: editTitle,
    summary: editSummary,
    components: editComponents,
  });
  const reviewGateContent = reviewContentRefusal({ rootCause, remediation });

  return (
    <div>
      <Link
        href="/admin/incidents"
        className="mb-4 inline-flex items-center gap-1.5 font-mono text-xs uppercase tracking-widest text-slate-400 hover:text-slate-600"
      >
        <ArrowLeft className="size-3.5" />
        {t("incidents.incidents")}
      </Link>

      <PageHeader
        eyebrow={incident.incidentNumber}
        title={incident.title}
        description={incident.summary}
      >
        {canWrite && (
          <>
            <Button variant="outline" className="gap-1.5" onClick={openEdit}>
              <Pencil className="size-4" />
              {t("common.edit")}
            </Button>
            <Button className="gap-1.5" onClick={() => setUpdateOpen(true)}>
              <Send className="size-4" />
              {t("incidents.postUpdate")}
            </Button>
          </>
        )}
      </PageHeader>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <div className="space-y-6">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-baseline justify-between gap-4">
                <h2 className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("incidents.publicTimeline")}
                </h2>
                <span className="font-mono text-xs text-slate-400">
                  {timeline.length}{" "}
                  {timeline.length === 1 ? "update" : "updates"}
                </span>
              </div>

              {timeline.length === 0 ? (
                <p className="mt-4 text-sm leading-relaxed text-slate-500">
                  Nothing has been posted yet. Customers can see the incident
                  and its summary, and nothing about what is being done.
                </p>
              ) : (
                <ol className="mt-4 space-y-4 border-l-2 border-slate-200 pl-4">
                  {timeline.map((update) => (
                    <li key={update.incidentUpdateId}>
                      <div className="flex flex-wrap items-center gap-2">
                        <IncidentStatusChip status={update.status} />
                        <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                          <LocalTime value={update.postedAt} />
                        </span>
                      </div>
                      <p className="mt-1.5 whitespace-pre-wrap text-sm leading-relaxed text-slate-600">
                        {update.body}
                      </p>
                    </li>
                  ))}
                </ol>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex flex-wrap items-baseline justify-between gap-4">
                <h2 className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("incidents.postIncidentReview")}
                </h2>
                {canWrite && reviewGate === null && (
                  <Button
                    variant="outline"
                    size="sm"
                    className="gap-1.5"
                    onClick={() => {
                      setRootCause(incident.rootCause ?? "");
                      setRemediation(incident.remediation ?? "");
                      setReviewOpen(true);
                    }}
                  >
                    <FileText className="size-4" />
                    {hasPublishedReview(incident) ? "Revise" : "Publish"}
                  </Button>
                )}
              </div>

              {reviewGate !== null ? (
                <p className="mt-4 text-sm leading-relaxed text-slate-500">
                  {reviewGate}
                </p>
              ) : hasPublishedReview(incident) ? (
                <div className="mt-4 space-y-4">
                  <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                    {t("incidents.published")} <LocalTime value={incident.reviewPublishedAt!} />
                  </p>
                  <div>
                    <p className="font-mono text-xs text-slate-500">
                      {t("incidents.rootCause")}
                    </p>
                    <p className="mt-1 whitespace-pre-wrap text-sm leading-relaxed text-slate-600">
                      {incident.rootCause}
                    </p>
                  </div>
                  <div>
                    <p className="font-mono text-xs text-slate-500">
                      {t("incidents.remediation")}
                    </p>
                    <p className="mt-1 whitespace-pre-wrap text-sm leading-relaxed text-slate-600">
                      {incident.remediation}
                    </p>
                  </div>
                </div>
              ) : (
                <p className="mt-4 text-sm leading-relaxed text-slate-500">
                  Nothing published yet. This incident is resolved, so a review
                  can be written — it appears on the public status page under
                  the incident.
                </p>
              )}
            </CardContent>
          </Card>
        </div>

        <Card className="h-fit">
          <CardContent className="p-0">
            <div className="flex flex-wrap items-center gap-2 px-6 pt-6">
              <IncidentStatusChip status={incident.status} />
              <IncidentImpactChip impact={incident.impact} />
            </div>
            <FieldList className="mt-4">
              <Field label={t("common.components")}>
                {componentsSentence(incident.components)}
              </Field>
              <Field label={t("common.started")} mono>
                <LocalTime value={incident.startedAt} />
              </Field>
              <Field label={t("incidents.resolved")} mono>
                {incident.resolvedAt === null ? (
                  <span className="text-slate-400">still open</span>
                ) : (
                  <LocalTime value={incident.resolvedAt} />
                )}
              </Field>
              <Field label={t("incidents.duration")}>
                <span title={durationSentence(incident)}>
                  {durationSentence(incident)}
                </span>
              </Field>
            </FieldList>
          </CardContent>
        </Card>
      </div>

      <Dialog open={updateOpen} onOpenChange={setUpdateOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("incidents.postAnUpdate")}</DialogTitle>
            <DialogDescription>{UPDATE_IS_PUBLISHED}</DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.status")}
              </label>
              <Select
                value={updateStatus}
                onValueChange={(value) =>
                  setUpdateStatus(value as IncidentStatus)
                }
              >
                <SelectTrigger className="mt-1.5 w-64">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {INCIDENT_STATUSES.map((status) => (
                    <SelectItem key={status} value={status}>
                      {status}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="mt-1.5 max-w-xl text-xs leading-relaxed text-slate-500">
                {INCIDENT_STATUS_DESCRIPTIONS[updateStatus]}
              </p>

              {updateStatus === "Resolved" && (
                <p className="mt-3 rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs leading-relaxed text-attention-700">
                  {RESOLVED_ENDS_IT}
                </p>
              )}
            </div>

            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("incidents.whatHasChanged")}
              </label>
              <Textarea
                value={updateBody}
                onChange={(event) => setUpdateBody(event.target.value)}
                maxLength={UPDATE_BODY_MAX_LENGTH}
                rows={5}
                placeholder={t("incidents.whatYouHaveFoundWhat")}
                className="mt-1.5"
              />
            </div>

            {updateGate !== null && updateBody.length > 0 && (
              <p className="text-sm leading-relaxed text-critical-700">
                {updateGate}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setUpdateOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button
              disabled={updateGate !== null || saving}
              onClick={() =>
                mutate(
                  "post-update",
                  `/incidents/${incidentId}/updates`,
                  "POST",
                  { status: updateStatus, body: updateBody.trim() },
                  {
                    title:
                      updateStatus === "Resolved"
                        ? "Resolved"
                        : t("incidents.updatePosted"),
                    description:
                      updateStatus === "Resolved"
                        ? t("incidents.publicPageNoLongerShows")
                        : t("incidents.publishedStatusPageMailedSubscribers"),
                  },
                  () => {
                    setUpdateOpen(false);
                    setUpdateBody("");
                  }
                )
              }
            >
              {saving
                ? "Posting…"
                : updateStatus === "Resolved"
                  ? "Post and resolve"
                  : "Post update"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={editOpen} onOpenChange={setEditOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("incidents.editTheIncident")}</DialogTitle>
            <DialogDescription>{EDIT_CORRECTS_THE_ACCOUNT}</DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("incidents.title")}
              </label>
              <Input
                value={editTitle}
                onChange={(event) => setEditTitle(event.target.value)}
                maxLength={TITLE_MAX_LENGTH}
                className="mt-1.5"
              />
            </div>

            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("incidents.summary")}
              </label>
              <Textarea
                value={editSummary}
                onChange={(event) => setEditSummary(event.target.value)}
                maxLength={SUMMARY_MAX_LENGTH}
                rows={4}
                className="mt-1.5"
              />
            </div>

            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("incidents.impact")}
              </label>
              <Select
                value={editImpact}
                onValueChange={(value) =>
                  setEditImpact(value as IncidentImpact)
                }
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
                {INCIDENT_IMPACT_DESCRIPTIONS[editImpact]}
              </p>
            </div>

            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.components")}
              </label>
              <ComponentPicker
                selected={editComponents}
                onChange={setEditComponents}
                className="mt-1.5"
              />
            </div>

            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("incidents.startedAt")}
              </label>
              <DateTimePicker
                value={editStartedAt}
                onChange={setEditStartedAt}
                placeholder={t("incidents.selectStartDateAndTime")}
                aria-label={t("incidents.incidentStartedAt")}
                className="mt-1.5 w-64"
              />
              <p className="mt-1.5 max-w-xl text-xs leading-relaxed text-slate-500">
                {t("incidents.correctingThisMovesWhatThe")}
              </p>
            </div>

            {editGate !== null && (
              <p className="text-sm leading-relaxed text-critical-700">
                {editGate}
              </p>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setEditOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button
              disabled={editGate !== null || saving}
              onClick={() =>
                mutate(
                  "edit",
                  `/incidents/${incidentId}`,
                  "PUT",
                  {
                    title: editTitle.trim(),
                    summary: editSummary.trim(),
                    impact: editImpact,
                    components: editComponents,
                    startedAt: new Date(editStartedAt).toISOString(),
                  },
                  {
                    title: t("incidents.incidentUpdated"),
                    description:
                      t("incidents.accountHasBeenCorrectedUpdates"),
                  },
                  () => setEditOpen(false)
                )
              }
            >
              {saving ? "Saving…" : "Save"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={reviewOpen} onOpenChange={setReviewOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("incidents.publishThePostIncidentReview")}</DialogTitle>
            <DialogDescription>{REVIEW_IS_PUBLIC}</DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("incidents.rootCause")}
              </label>
              <Textarea
                value={rootCause}
                onChange={(event) => setRootCause(event.target.value)}
                maxLength={REVIEW_MAX_LENGTH}
                rows={5}
                placeholder={t("incidents.whatActuallyWentWrongIn")}
                className="mt-1.5"
              />
            </div>

            <div>
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("incidents.remediation")}
              </label>
              <Textarea
                value={remediation}
                onChange={(event) => setRemediation(event.target.value)}
                maxLength={REVIEW_MAX_LENGTH}
                rows={5}
                placeholder={t("incidents.whatHasChangedSoThat")}
                className="mt-1.5"
              />
            </div>

            {reviewGateContent !== null &&
              rootCause.length + remediation.length > 0 && (
                <p className="text-sm leading-relaxed text-critical-700">
                  {reviewGateContent}
                </p>
              )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setReviewOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button
              disabled={reviewGateContent !== null || saving}
              onClick={() =>
                mutate(
                  "review",
                  `/incidents/${incidentId}/review`,
                  "PUT",
                  {
                    rootCause: rootCause.trim(),
                    remediation: remediation.trim(),
                  },
                  {
                    title: t("incidents.reviewPublished"),
                    description:
                      t("incidents.publicStatusPageUnderIncident"),
                  },
                  () => setReviewOpen(false)
                )
              }
            >
              {saving ? "Publishing…" : "Publish"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default IncidentDetail;
