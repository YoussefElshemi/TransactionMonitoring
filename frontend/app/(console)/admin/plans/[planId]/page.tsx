"use client";

import { useCallback, useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { toast } from "sonner";
import {
  ArrowLeft,
  ChevronDown,
  ChevronRight,
  Lock,
  Trash2,
  TrendingUp,
} from "lucide-react";

import { CurrencyCode, Plan, PlanVersion, UserResource } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  DELETE_IS_NARROW,
  DESCRIPTION_MAX_LENGTH,
  DISABLE_RATHER_THAN_DELETE,
  NAME_MAX_LENGTH,
  OWNERSHIP_HINTS,
  OWNERSHIP_LABELS,
  PLAN_EDITABLE_FIELDS,
  PLAN_IN_USE_MESSAGE,
  REPRICING_MOVES_NOBODY,
  VERSIONS_ARE_IMMUTABLE,
  formatRate,
  isCurrentVersion,
  isValidCostPerRequest,
  isValidPlanDescription,
  isValidPlanName,
  planOwnership,
  versionsNewestFirst,
} from "@/lib/plans";
import {
  type StructureDraft,
  draftFromVersion,
  draftHeadlineRate,
  hasPriceStructure,
  priceShapeLabel,
  priceShapeSentence,
  pricesRequestBody,
  structureRefusal,
} from "@/lib/pricing";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  CURRENCY_CODES,
  CURRENCY_IS_PART_OF_THE_PRICE,
  CURRENCY_LABELS,
} from "@/lib/currency";
import { PriceBuilder } from "@/components/billing/PriceBuilder";
import { PriceStructure } from "@/components/billing/PriceStructure";
import { canWriteBilling } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { apiFetch } from "@/lib/http";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { cn } from "@/lib/utils";
import { LocalTime } from "@/components/LocalTime";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { PolicySwitch } from "@/components/PolicySwitch";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Textarea } from "@/components/ui/textarea";
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

const VERSION_PAGE_SIZE = 100;

const PUBLISHER_PAGE_SIZE = 100;

const PlanDetail = () => {
  const t = useTranslation();
  const { allowed } = useRequirePermission(
    ["plans:read", "platform:administer"],
    "/"
  );

  const router = useRouter();
  const { planId } = useParams<{ planId: string }>();
  const { grants } = usePermissions();
  const submission = useIdempotencyKey();

  const [plan, setPlan] = useState<Plan | null>(null);
  const [versions, setVersions] = useState<PlanVersion[]>([]);
  const [publishers, setPublishers] = useState<UserResource[]>([]);
  const [loading, setLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isEnabled, setIsEnabled] = useState(true);
  const [saving, setSaving] = useState(false);

  const [repriceOpen, setRepriceOpen] = useState(false);
  const [newRate, setNewRate] = useState("");
  const [publishing, setPublishing] = useState(false);

  const [banded, setBanded] = useState(false);
  const [repriceStructure, setRepriceStructure] = useState<StructureDraft>({
    currency: "GBP",
    platformFee: "",
    minimumCharge: "",
    prices: [],
  });

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);

  const [expandedVersionId, setExpandedVersionId] = useState<string | null>(
    null
  );

  const canWrite = canWriteBilling(grants, "plans:write");

  const goToLogin = useCallback(() => router.push("/login"), [router]);

  const fetchPlan = useCallback(async () => {
    setLoading(true);

    try {
      const [detail, history] = await Promise.all([
        apiFetch(`${API_BASE}/plans/${planId}`, {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }),
        apiFetch(
          `${API_BASE}/plans/${planId}/versions?PageNumber=1&PageSize=${VERSION_PAGE_SIZE}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        ),
      ]);

      if (detail.ok) {
        const data = await unwrap<Plan>(detail);
        setPlan(data);
        setName(data.name);
        setDescription(data.description ?? "");
        setIsEnabled(data.isEnabled);
      } else if (detail.status === 404) {
        setNotFound(true);
      } else {
        await reportApiError(detail, {
          fallback: t("plans.errorOccurredFetchingPlan"),
          onUnauthorized: goToLogin,
          toastId: "error-fetching-plan",
        });
      }

      if (history.ok) {
        const page = await unwrapPage<PlanVersion>(history);
        setVersions(page.data);
      }
    } catch {
      toast.error(t("plans.errorOccurredFetchingPlan"), {
        id: "error-fetching-plan",
      });
    } finally {
      setLoading(false);
    }
  }, [planId, goToLogin]);

  const fetchPublishers = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/users?PageNumber=1&PageSize=${PUBLISHER_PAGE_SIZE}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<UserResource>(response);
        setPublishers(page.data);
      }
    } catch {
      setPublishers([]);
    }
  }, []);

  useEffect(() => {
    if (allowed) fetchPlan();
  }, [allowed, fetchPlan]);

  useEffect(() => {
    if (allowed) fetchPublishers();
  }, [allowed, fetchPublishers]);

  const publisherName = (userId: string | null): string => {
    if (userId === null) return "—";

    return (
      publishers.find((resource) => resource.user.userId === userId)?.user
        .userName ?? userId
    );
  };

  const nameValid = isValidPlanName(name);
  const descriptionValid = isValidPlanDescription(description);

  const detailsDirty =
    plan !== null &&
    (plan.name !== name.trim() ||
      (plan.description ?? "") !== description.trim() ||
      plan.isEnabled !== isEnabled);

  const handleSave = async () => {
    if (!plan) return;

    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/plans/${plan.planId}`,
        {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            name: name.trim(),
            description: description.trim(),
            isEnabled,
          }),
        },
        { idempotencyKey: submission.current("update") }
      );

      if (response.ok) {
        const updated = await unwrap<Plan>(response);
        submission.completed("update");
        setPlan(updated);

        toast.success(
          plan.isEnabled && !isEnabled
            ? t("plans.withdrawnFromCatalogue", updated.name)
            : t("plans.planUpdated"),
          {
            description:
              plan.isEnabled && !isEnabled
                ? DISABLE_RATHER_THAN_DELETE
                : undefined,
          }
        );
      } else {
        await reportApiError(response, {
          fallback: t("plans.failedUpdatePlan"),
          onUnauthorized: goToLogin,
          fieldLabels: {
            "Body.Name": "Name",
            "Body.Description": "Description",
            "Body.IsEnabled": "Offered",
          },
        });
      }
    } catch {
      toast.error(t("plans.failedUpdatePlan"));
    } finally {
      setSaving(false);
    }
  };

  const rateValid = isValidCostPerRequest(Number(newRate));
  const structureRefused = banded ? structureRefusal(repriceStructure) : null;

  const handleReprice = async () => {
    if (!plan) return;

    setPublishing(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/plans/${plan.planId}/versions`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            costPerRequest: Number(newRate),
            currency: repriceStructure.currency,
            ...(banded
              ? {
                  platformFee: Number(repriceStructure.platformFee || 0),
                  minimumCharge: Number(repriceStructure.minimumCharge || 0),
                  prices: pricesRequestBody(repriceStructure),
                }
              : {}),
          }),
        },
        { idempotencyKey: submission.current("reprice") }
      );

      if (response.ok) {
        const published = await unwrap<PlanVersion>(response);
        submission.completed("reprice");

        toast.success(t(
            "plans.versionPublished",
            plan.name,
            published.versionNumber
          ), {
          description: REPRICING_MOVES_NOBODY,
        });

        setRepriceOpen(false);
        setNewRate("");
        setBanded(false);
        await fetchPlan();
      } else {
        await reportApiError(response, {
          fallback: t("plans.failedPublishNewVersion"),
          onUnauthorized: goToLogin,
          fieldLabels: {
            "Body.CostPerRequest": "Rate",
            "Body.PlatformFee": "Platform fee",
            "Body.MinimumCharge": "Monthly minimum",
            "Body.Prices": "Prices",
          },
        });
      }
    } catch {
      toast.error(t("plans.failedPublishNewVersion"));
    } finally {
      setPublishing(false);
    }
  };

  const handleDelete = async () => {
    if (!plan) return;

    setDeleting(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/plans/${plan.planId}`,
        {
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: submission.current("delete") }
      );

      if (response.ok) {
        submission.completed("delete");
        toast.success(t("common.namedDeleted", plan.name));
        router.push("/admin/plans");
      } else if (response.status === 409) {
        toast.error(t("plans.planUse"), {
          description: PLAN_IN_USE_MESSAGE,
        });
        setDeleteOpen(false);
      } else {
        await reportApiError(response, {
          fallback: t("plans.failedDeletePlan"),
          onUnauthorized: goToLogin,
        });
      }
    } catch {
      toast.error(t("plans.failedDeletePlan"));
    } finally {
      setDeleting(false);
    }
  };

  if (!allowed) return null;

  if (notFound) {
    return (
      <NotFound
        title={t("plans.planNotFound")}
        description={t("plans.itMayHaveBeenDeleted")}
        backHref="/admin/plans"
        backLabel={t("plans.backToPlans")}
      />
    );
  }

  if (loading || !plan) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-16 w-96" />
        <Skeleton className="h-48 w-full rounded-xl" />
        <Skeleton className="h-64 w-full rounded-xl" />
      </div>
    );
  }

  const ownership = planOwnership(plan);
  const history = versionsNewestFirst(versions);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("plans.plan")}
        title={plan.name}
        description={OWNERSHIP_HINTS[ownership]}
      >
        <Button
          variant="ghost"
          size="sm"
          className="gap-1.5 text-slate-500 hover:text-slate-900"
          onClick={() => router.push("/admin/plans")}
        >
          <ArrowLeft className="size-4" />
          {t("plans.plans")}
        </Button>
        {canWrite && (
          <Button
            size="sm"
            className="gap-1.5"
            onClick={() => {
              setNewRate(String(plan.currentVersion?.costPerRequest ?? 0));

              const structured =
                plan.currentVersion !== null &&
                hasPriceStructure(plan.currentVersion);

              setBanded(structured);
              setRepriceStructure(
                structured && plan.currentVersion !== null
                  ? draftFromVersion(plan.currentVersion)
                  : {
                      currency: plan.currentVersion?.currency ?? "GBP",
                      platformFee: "",
                      minimumCharge: "",
                      prices: [],
                    }
              );

              setRepriceOpen(true);
            }}
          >
            <TrendingUp className="size-4" />
            {t("plans.reprice")}
          </Button>
        )}
      </PageHeader>

      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border border-elevated-200 bg-elevated-50 p-4">
          <span className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("plans.currentVersion")}
          </span>
          <p className="mt-2 text-2xl font-medium text-slate-900">
            v{plan.currentVersionNumber}
          </p>
          <p className="mt-1 font-mono text-sm text-slate-700">
            {plan.currentVersion
              ? priceShapeSentence(plan.currentVersion)
              : "—"}
          </p>
        </div>

        <div className="rounded-xl border border-slate-200 bg-card p-4">
          <span className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("plans.offeredTo")}
          </span>
          <p className="mt-2 text-2xl font-medium text-slate-900">
            {OWNERSHIP_LABELS[ownership]}
          </p>
          <p className="mt-1 text-sm text-slate-500">
            {plan.isEnabled
              ? "In the catalogue"
              : "Withdrawn — nobody new can be put on it"}
          </p>
        </div>

        <div className="rounded-xl border border-slate-200 bg-card p-4">
          <span className="font-mono text-xs uppercase tracking-widest text-slate-500">
            {t("plans.versions")}
          </span>
          <p className="mt-2 text-2xl font-medium text-slate-900">
            {history.length}
          </p>
          <p className="mt-1 text-sm text-slate-500">
            {t("common.created")} <LocalTime value={plan.createdAt} dateOnly />
          </p>
        </div>
      </div>

      <Card>
        <CardContent className="space-y-4 pt-6">
          <p className="text-sm text-slate-500">{PLAN_EDITABLE_FIELDS}</p>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("common.name")}
            </label>
            <Input
              value={name}
              onChange={(event) => setName(event.target.value)}
              maxLength={NAME_MAX_LENGTH}
              disabled={!canWrite || saving}
            />
            {!nameValid && (
              <p className="text-xs text-critical-500">
                A plan needs a name, of at most {NAME_MAX_LENGTH} characters.
              </p>
            )}
          </div>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("common.description")}
            </label>
            <Textarea
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              maxLength={DESCRIPTION_MAX_LENGTH}
              className="min-h-[64px]"
              disabled={!canWrite || saving}
            />
            {!descriptionValid && (
              <p className="text-xs text-critical-500">
                At most {DESCRIPTION_MAX_LENGTH} characters.
              </p>
            )}
          </div>

          <PolicySwitch
            label={t("plans.offeredInTheCatalogue")}
            checked={isEnabled}
            disabled={!canWrite || saving}
            onChange={setIsEnabled}
          >
            {DISABLE_RATHER_THAN_DELETE}
          </PolicySwitch>

          {canWrite && (
            <div className="flex items-center gap-2 border-t border-slate-100 pt-4">
              <Button
                size="sm"
                onClick={handleSave}
                disabled={
                  !detailsDirty || !nameValid || !descriptionValid || saving
                }
              >
                {saving ? "Saving..." : "Save"}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setName(plan.name);
                  setDescription(plan.description ?? "");
                  setIsEnabled(plan.isEnabled);
                }}
                disabled={!detailsDirty || saving}
              >
                {t("common.reset")}
              </Button>

              <div className="ml-auto">
                <Button
                  variant="ghost"
                  size="sm"
                  className="gap-1.5 text-critical-500 hover:text-critical-700"
                  onClick={() => setDeleteOpen(true)}
                >
                  <Trash2 className="size-4" />
                  {t("common.delete")}
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <div className="space-y-3">
        <div className="flex items-start gap-2">
          <Lock className="mt-0.5 size-3.5 flex-shrink-0 text-slate-400" />
          <div>
            <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
              {t("plans.versionHistory")}
            </p>
            <p className="mt-1 max-w-2xl text-sm text-slate-500">
              {VERSIONS_ARE_IMMUTABLE} {REPRICING_MOVES_NOBODY}
            </p>
          </div>
        </div>

        <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
          <Table>
            <TableHeader>
              <TableRow className="bg-slate-50 hover:bg-slate-50">
                <TableHead className="w-28">{t("plans.version")}</TableHead>
                <TableHead className="w-64">{t("plans.price")}</TableHead>
                <TableHead>{t("plans.publishedBy")}</TableHead>
                <TableHead className="w-48">{t("plans.published")}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {history.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={4} className="py-10 text-center">
                    <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                      {t("plans.noVersionsLoaded")}
                    </p>
                  </TableCell>
                </TableRow>
              ) : (
                history.flatMap((version) => {
                  const current = isCurrentVersion(plan, version);
                  const structured = hasPriceStructure(version);
                  const expanded = expandedVersionId === version.planVersionId;

                  return [
                    <TableRow
                      key={version.planVersionId}
                      className={cn(
                        "hover:bg-slate-50/50",
                        !current && "text-slate-500"
                      )}
                    >
                      <TableCell>
                        <span
                          className={cn(
                            "rounded-full border px-2 py-0.5 font-mono text-xs",
                            current
                              ? "border-elevated-200 bg-elevated-50 text-elevated-700"
                              : "border-slate-200 bg-slate-50 text-slate-700"
                          )}
                        >
                          v{version.versionNumber}
                        </span>
                        {current && (
                          <span
                            className="ml-2 font-mono text-[11px] uppercase tracking-widest text-elevated-700"
                            title={t("plans.whatANewSubscriptionIs")}
                          >
                            current
                          </span>
                        )}
                      </TableCell>

                      <TableCell className="font-mono text-xs text-slate-700">
                        {structured ? (
                          <button
                            type="button"
                            onClick={() =>
                              setExpandedVersionId(
                                expanded ? null : version.planVersionId
                              )
                            }
                            className="flex items-center gap-1 text-left hover:text-slate-900 hover:underline"
                          >
                            {expanded ? (
                              <ChevronDown className="size-3.5 flex-shrink-0" />
                            ) : (
                              <ChevronRight className="size-3.5 flex-shrink-0" />
                            )}
                            {priceShapeLabel(version)}
                          </button>
                        ) : (
                          priceShapeLabel(version)
                        )}
                      </TableCell>

                      <TableCell className="text-sm">
                        {publisherName(version.publishedByUserId)}
                      </TableCell>

                      <TableCell className="text-xs text-slate-500">
                        <LocalTime value={version.createdAt} />
                      </TableCell>
                    </TableRow>,

                    ...(expanded
                      ? [
                          <TableRow
                            key={`${version.planVersionId}-structure`}
                            className="hover:bg-transparent"
                          >
                            <TableCell colSpan={4} className="bg-slate-50/60 p-4">
                              <PriceStructure version={version} />
                            </TableCell>
                          </TableRow>,
                        ]
                      : []),
                  ];
                })
              )}
            </TableBody>
          </Table>
        </div>
      </div>

      <Dialog open={repriceOpen} onOpenChange={setRepriceOpen}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("plans.repriceTitle", plan.name)}</DialogTitle>
            <DialogDescription>
              {t(
                "plans.repriceDescription",
                String(plan.currentVersionNumber + 1),
                String(plan.currentVersionNumber)
              )}
            </DialogDescription>
          </DialogHeader>

          <div className="grid gap-3 sm:grid-cols-2">
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
              <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
                v{plan.currentVersionNumber} · now
              </p>
              <p className="mt-2 font-mono text-sm text-slate-700">
                {plan.currentVersion
                  ? priceShapeSentence(plan.currentVersion)
                  : "—"}
              </p>
            </div>

            <div className="rounded-lg border border-brand-800 bg-brand-50 p-3">
              <p className="font-mono text-xs uppercase tracking-widest text-brand-900">
                v{plan.currentVersionNumber + 1} · new
              </p>
              <Input
                value={newRate}
                onChange={(event) => setNewRate(event.target.value)}
                inputMode="decimal"
                className="mt-2 font-mono"
              />
              {banded &&
                draftHeadlineRate(Number(newRate), repriceStructure) !==
                  Number(newRate) && (
                  <p className="mt-1 text-xs text-attention-700">
                    Will be quoted as{" "}
                    {formatRate(
                      draftHeadlineRate(Number(newRate), repriceStructure),
                      repriceStructure.currency
                    )}
                    , taken from the catch-all price&apos;s bottom band.
                  </p>
                )}
            </div>
          </div>

          {!rateValid && (
            <p className="text-xs text-critical-500">
              {t("plans.mustBeANumberAnd")}
            </p>
          )}

          {!banded && (
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.currency")}
              </label>
              <Select
                value={repriceStructure.currency}
                onValueChange={(value) =>
                  setRepriceStructure({
                    ...repriceStructure,
                    currency: value as CurrencyCode,
                  })
                }
                disabled={publishing}
              >
                <SelectTrigger className="sm:w-72">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {CURRENCY_CODES.map((currency) => (
                    <SelectItem key={currency} value={currency}>
                      {CURRENCY_LABELS[currency]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-500">
                {CURRENCY_IS_PART_OF_THE_PRICE}
              </p>
            </div>
          )}

          <div className="max-h-[50vh] space-y-3 overflow-y-auto pr-1">
            {!banded ? (
              <div className="space-y-2">
                <button
                  type="button"
                  onClick={() =>
                    setBanded(true)
                  }
                  className="text-xs text-brand-900 underline underline-offset-2 hover:text-brand-800"
                >
                  {t("plans.priceThisVersionInBands")}
                </button>
                {plan.currentVersion &&
                  hasPriceStructure(plan.currentVersion) && (
                    <p className="text-xs text-attention-700">
                      v{plan.currentVersionNumber} has bands, a fee or a
                      minimum. Published as it stands, v
                      {plan.currentVersionNumber + 1} would be a single flat
                      rate instead.
                    </p>
                  )}
              </div>
            ) : (
              <div className="space-y-3 rounded-lg border border-slate-200 p-3">
                <div className="flex flex-wrap items-baseline justify-between gap-2">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
                    v{plan.currentVersionNumber + 1} structure
                  </p>
                  <button
                    type="button"
                    onClick={() => setBanded(false)}
                    className="text-xs text-slate-500 underline underline-offset-2 hover:text-slate-900"
                  >
                    {t("plans.publishASingleRateInstead")}
                  </button>
                </div>

                <PriceBuilder
                  draft={repriceStructure}
                  onChange={setRepriceStructure}
                  disabled={publishing}
                />
              </div>
            )}
          </div>

          <p className="rounded-lg border border-attention-200 bg-attention-50 p-3 text-xs text-attention-700">
            {VERSIONS_ARE_IMMUTABLE} {REPRICING_MOVES_NOBODY} Move them from the
            Billing section of each organization, where the change can also be
            given a future date.
          </p>

          <DialogFooter>
            <Button variant="outline" onClick={() => setRepriceOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button
              onClick={handleReprice}
              disabled={publishing || !rateValid || structureRefused !== null}
            >
              {publishing
                ? "Publishing..."
                : `Publish v${plan.currentVersionNumber + 1}`}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("plans.deleteTitle", plan.name)}</DialogTitle>
            <DialogDescription>{DELETE_IS_NARROW}</DialogDescription>
          </DialogHeader>

          <p className="rounded-lg border border-attention-200 bg-attention-50 p-3 text-xs text-attention-700">
            If anybody has ever been on this plan, the API will refuse. Withdraw
            it from the catalogue instead — turn off &ldquo;Offered in the
            catalogue&rdquo; above and save.
          </p>

          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteOpen(false)}>
              {t("common.keepIt")}
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={deleting}
            >
              {deleting ? "Deleting..." : "Delete"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default PlanDetail;
