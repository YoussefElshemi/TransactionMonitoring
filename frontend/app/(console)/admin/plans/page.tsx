"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Eye, Plus } from "lucide-react";

import { CurrencyCode, Organization, Plan } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  DESCRIPTION_MAX_LENGTH,
  NAME_HINT,
  NAME_MAX_LENGTH,
  OWNERSHIP_CHOICE_HINT,
  OWNERSHIP_HINTS,
  OWNERSHIP_LABELS,
  PlanOwnership,
  isValidCostPerRequest,
  isValidPlanDescription,
  isValidPlanName,
  planOwnership,
  rateSentence,
} from "@/lib/plans";
import {
  type StructureDraft,
  draftHeadlineRate,
  priceShapeLabel,
  priceShapeSentence,
  pricesRequestBody,
  structureRefusal,
} from "@/lib/pricing";
import {
  CURRENCY_CODES,
  CURRENCY_IS_PART_OF_THE_PRICE,
  CURRENCY_LABELS,
} from "@/lib/currency";
import { PriceBuilder } from "@/components/billing/PriceBuilder";
import { canWriteBilling } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { apiFetch } from "@/lib/http";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { cn } from "@/lib/utils";
import Pagination from "@/components/Pagination";
import { LocalTime } from "@/components/LocalTime";
import { PageHeader } from "@/components/PageHeader";
import { PolicySwitch } from "@/components/PolicySwitch";
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
import { useTranslation } from "@/context/LocalizationContext";

const OWNER_FILTER_ANY = "Any";

const OWNER_PAGE_SIZE = 100;

const NO_STRUCTURE: StructureDraft = {
  currency: "GBP",
  platformFee: "",
  minimumCharge: "",
  prices: [],
};

const Plans = () => {
  const t = useTranslation();
  const { allowed } = useRequirePermission(
    ["plans:read", "platform:administer"],
    "/"
  );

  const router = useRouter();
  const { grants } = usePermissions();
  const submission = useIdempotencyKey();

  const [plans, setPlans] = useState<Plan[]>([]);
  const [organizations, setOrganizations] = useState<Organization[]>([]);

  const [nameFilter, setNameFilter] = useState("");
  const [ownerFilter, setOwnerFilter] = useState<string>(OWNER_FILTER_ANY);
  const [includeDisabled, setIncludeDisabled] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const [newName, setNewName] = useState("");
  const [newDescription, setNewDescription] = useState("");
  const [newOwnership, setNewOwnership] = useState<PlanOwnership>("catalogue");
  const [newOwnerId, setNewOwnerId] = useState("");
  const [newRate, setNewRate] = useState("0");

  const [banded, setBanded] = useState(false);
  const [newStructure, setNewStructure] =
    useState<StructureDraft>(NO_STRUCTURE);

  const canWrite = canWriteBilling(grants, "plans:write");

  const goToLogin = useCallback(() => router.push("/login"), [router]);

  const fetchPlans = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: String(pageSize),
      });
      if (nameFilter.trim().length > 0) query.set("Name", nameFilter.trim());
      if (ownerFilter !== OWNER_FILTER_ANY) {
        query.set("OwnerOrganizationId", ownerFilter);
      }
      if (includeDisabled) query.set("IncludeDisabled", "true");

      const response = await apiFetch(`${API_BASE}/plans?${query}`, {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (response.ok) {
        const page = await unwrapPage<Plan>(response);
        setPlans(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(response, {
          fallback: t("plans.errorOccurredFetchingPlans"),
          onUnauthorized: goToLogin,
          toastId: "error-fetching-plans",
        });
      }
    } catch {
      toast.error(t("plans.errorOccurredFetchingPlans"), {
        id: "error-fetching-plans",
      });
    } finally {
      setLoading(false);
    }
  }, [pageNumber, pageSize, nameFilter, ownerFilter, includeDisabled, goToLogin]);

  const fetchOrganizations = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations?PageNumber=1&PageSize=${OWNER_PAGE_SIZE}`,
        {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );

      if (response.ok) {
        const page = await unwrapPage<Organization>(response);
        setOrganizations(page.data);
      }
    } catch {
      setOrganizations([]);
    }
  }, []);

  useEffect(() => {
    if (allowed) fetchPlans();
  }, [allowed, fetchPlans]);

  useEffect(() => {
    if (allowed) fetchOrganizations();
  }, [allowed, fetchOrganizations]);

  const ownerName = (organizationId: string): string =>
    organizations.find(
      (organization) => organization.organizationId === organizationId
    )?.friendlyName ?? organizationId;

  const resetCreateForm = () => {
    setNewName("");
    setNewDescription("");
    setNewOwnership("catalogue");
    setNewOwnerId("");
    setNewRate("0");
    setBanded(false);
    setNewStructure(NO_STRUCTURE);
  };

  const nameValid = isValidPlanName(newName);
  const descriptionValid = isValidPlanDescription(newDescription);
  const rateValid = isValidCostPerRequest(Number(newRate));

  const structureRefused = banded ? structureRefusal(newStructure) : null;

  const createBlocked =
    saving ||
    !nameValid ||
    !descriptionValid ||
    !rateValid ||
    structureRefused !== null ||
    (newOwnership === "bespoke" && newOwnerId.length === 0);

  const handleCreate = async () => {
    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/plans`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            name: newName.trim(),
            // Omitted rather than sent empty: the server reads a blank
            // description as a value and bounces it off `PlanDescription`.
            ...(newDescription.trim().length > 0
              ? { description: newDescription.trim() }
              : {}),
            ...(newOwnership === "bespoke"
              ? { organizationId: newOwnerId }
              : {}),
            costPerRequest: Number(newRate),
            currency: newStructure.currency,
            ...(banded
              ? {
                  platformFee: Number(newStructure.platformFee || 0),
                  minimumCharge: Number(newStructure.minimumCharge || 0),
                  prices: pricesRequestBody(newStructure),
                }
              : {}),
          }),
        },
        { idempotencyKey: submission.current("create") }
      );

      if (response.ok) {
        const created = await unwrap<Plan>(response);
        submission.completed("create");

        toast.success(t("plans.versionOnePublished", created.name), {
          description: created.currentVersion
            ? priceShapeSentence(created.currentVersion)
            : rateSentence(Number(newRate), newStructure.currency),
        });

        setCreateOpen(false);
        resetCreateForm();
        router.push(`/admin/plans/${created.planId}`);
      } else if (response.status === 409) {
        toast.error(t("plans.planNameAlreadyExists"), {
          description:
            newOwnership === "bespoke"
              ? t("plans.namesAreUniquePerOwner")
              : t("plans.namesAreUniqueAcrossCatalogue"),
        });
      } else {
        await reportApiError(response, {
          fallback: t("plans.failedCreatePlan"),
          onUnauthorized: goToLogin,
          fieldLabels: {
            "Body.Name": "Name",
            "Body.Description": "Description",
            "Body.OrganizationId": "Owner",
            "Body.CostPerRequest": "Rate",
            "Body.PlatformFee": "Platform fee",
            "Body.MinimumCharge": "Monthly minimum",
            "Body.Prices": "Prices",
          },
        });
      }
    } catch {
      toast.error(t("plans.failedCreatePlan"));
    } finally {
      setSaving(false);
    }
  };

  if (!allowed) return null;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("plans.platform")}
        title={t("plans.plans")}
        description={t("plans.everyPriceThisInstallationOffers")}
      >
        {canWrite && (
          <Button
            size="sm"
            className="gap-1.5"
            onClick={() => {
              resetCreateForm();
              setCreateOpen(true);
            }}
          >
            <Plus className="size-4" />
            {t("plans.newPlan")}
          </Button>
        )}
      </PageHeader>

      <div className="flex flex-wrap items-end justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Input
            placeholder={t("common.searchByName")}
            value={nameFilter}
            onChange={(event) => {
              setNameFilter(event.target.value);
              setPageNumber(1);
            }}
            className="w-56"
          />

          <Select
            value={ownerFilter}
            onValueChange={(next) => {
              setOwnerFilter(next);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-64">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={OWNER_FILTER_ANY}>{t("plans.everyPlan")}</SelectItem>
              {organizations.map((organization) => (
                <SelectItem
                  key={organization.organizationId}
                  value={organization.organizationId}
                >
                  What {organization.friendlyName} may be put on
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <PageSizeSelect
            value={pageSize}
            onChange={(next) => {
              setPageSize(next);
              setPageNumber(1);
            }}
          />
        </div>
      </div>

      <PolicySwitch
        label={t("plans.showDisabledPlans")}
        checked={includeDisabled}
        onChange={(next) => {
          setIncludeDisabled(next);
          setPageNumber(1);
        }}
      >
        Off by default. A disabled plan is withdrawn from the catalogue — nobody
        new can be put on it — but everyone already on it stays on it and goes
        on being invoiced, so it is not gone.
      </PolicySwitch>

      {ownerFilter !== OWNER_FILTER_ANY && (
        <p className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500">
          Showing what {ownerName(ownerFilter)} may be put on: the whole
          catalogue, plus any plan negotiated with them alone. Somebody
          else&apos;s bespoke plan is not in this list.
        </p>
      )}

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("common.plan")}</TableHead>
              <TableHead className="w-56">{t("plans.offeredTo")}</TableHead>
              <TableHead className="w-44">{t("plans.currentVersion")}</TableHead>
              <TableHead className="w-44">{t("plans.price")}</TableHead>
              <TableHead className="w-44">{t("common.updated")}</TableHead>
              <TableHead className="w-20">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 4 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 6 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : plans.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="py-14 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {nameFilter || ownerFilter !== OWNER_FILTER_ANY
                      ? "No results found"
                      : "No plans yet"}
                  </p>
                  {!nameFilter && ownerFilter === OWNER_FILTER_ANY && (
                    <p className="mx-auto mt-3 max-w-md text-sm text-slate-500">
                      Nothing can be invoiced until there is a plan to be put
                      on. Create one, then subscribe organisations to it from
                      the Billing section of each.
                    </p>
                  )}
                </TableCell>
              </TableRow>
            ) : (
              plans.map((plan) => {
                const ownership = planOwnership(plan);

                return (
                  <TableRow
                    key={plan.planId}
                    className={cn(
                      "hover:bg-slate-50/50",
                      !plan.isEnabled && "text-slate-400"
                    )}
                  >
                    <TableCell>
                      <Link
                        href={`/admin/plans/${plan.planId}`}
                        className="font-medium text-slate-900 hover:underline"
                      >
                        {plan.name}
                      </Link>
                      {!plan.isEnabled && (
                        <span className="ml-2 font-mono text-[11px] uppercase tracking-widest text-slate-400">
                          disabled
                        </span>
                      )}
                      {plan.description && (
                        <p
                          className="max-w-md truncate text-xs text-slate-500"
                          title={plan.description}
                        >
                          {plan.description}
                        </p>
                      )}
                    </TableCell>

                    <TableCell>
                      <span
                        className={cn(
                          "rounded-full border px-2 py-0.5 font-mono text-xs",
                          ownership === "catalogue"
                            ? "border-elevated-200 bg-elevated-50 text-elevated-700"
                            : "border-slate-200 bg-slate-50 text-slate-700"
                        )}
                      >
                        {OWNERSHIP_LABELS[ownership]}
                      </span>
                      {plan.organizationId !== null && (
                        <p className="mt-1 truncate text-xs text-slate-500">
                          {ownerName(plan.organizationId)}
                        </p>
                      )}
                    </TableCell>

                    <TableCell className="font-mono text-xs text-slate-700">
                      v{plan.currentVersionNumber}
                    </TableCell>

                    <TableCell className="font-mono text-xs text-slate-700">
                      {plan.currentVersion
                        ? priceShapeLabel(plan.currentVersion)
                        : "—"}
                    </TableCell>

                    <TableCell className="text-xs text-slate-500">
                      <LocalTime value={plan.updatedAt} />
                    </TableCell>

                    <TableCell>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-slate-500 hover:text-slate-900"
                        onClick={() => router.push(`/admin/plans/${plan.planId}`)}
                      >
                        <Eye className="size-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {plans.length > 0 && (
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
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>{t("plans.newPlan2")}</DialogTitle>
            <DialogDescription>
              Creates the plan and its version 1 in one act. The rate below is
              that first version&apos;s, and it can never be edited afterwards —
              only superseded by publishing another.
            </DialogDescription>
          </DialogHeader>

          <div className="max-h-[60vh] space-y-4 overflow-y-auto pr-1">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.name")}
              </label>
              <Input
                value={newName}
                onChange={(event) => setNewName(event.target.value)}
                placeholder={t("plans.growth")}
                maxLength={NAME_MAX_LENGTH}
              />
              <p className="text-xs text-slate-500">{NAME_HINT}</p>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.description")}
              </label>
              <Textarea
                value={newDescription}
                onChange={(event) => setNewDescription(event.target.value)}
                placeholder={t("plans.optionalWhatThisPlanIs")}
                maxLength={DESCRIPTION_MAX_LENGTH}
                className="min-h-[64px]"
              />
              {!descriptionValid && (
                <p className="text-xs text-critical-500">
                  At most {DESCRIPTION_MAX_LENGTH} characters.
                </p>
              )}
            </div>

            <div className="space-y-3 rounded-lg border border-slate-200 p-3">
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("plans.offeredTo")}
                </label>
                <div className="flex flex-wrap gap-1.5">
                  {(["catalogue", "bespoke"] as PlanOwnership[]).map(
                    (ownership) => (
                      <button
                        key={ownership}
                        type="button"
                        onClick={() => setNewOwnership(ownership)}
                        className={cn(
                          "rounded-full border px-3 py-1 font-mono text-xs transition-colors",
                          newOwnership === ownership
                            ? "border-brand-800 bg-brand-50 text-brand-900"
                            : "border-slate-200 bg-card text-slate-600 hover:bg-slate-50"
                        )}
                      >
                        {OWNERSHIP_LABELS[ownership]}
                      </button>
                    )
                  )}
                </div>
                <p className="text-xs text-slate-500">
                  {OWNERSHIP_HINTS[newOwnership]}
                </p>
                <p className="text-xs text-slate-400">
                  {OWNERSHIP_CHOICE_HINT}
                </p>
              </div>

              {newOwnership === "bespoke" && (
                <div className="space-y-1.5">
                  <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                    {t("plans.customer")}
                  </label>
                  <Select value={newOwnerId} onValueChange={setNewOwnerId}>
                    <SelectTrigger>
                      <SelectValue placeholder={t("plans.chooseAnOrganization")} />
                    </SelectTrigger>
                    <SelectContent>
                      {organizations.map((organization) => (
                        <SelectItem
                          key={organization.organizationId}
                          value={organization.organizationId}
                        >
                          {organization.friendlyName}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              )}
            </div>

            {!banded && (
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("common.currency")}
                </label>
                <Select
                  value={newStructure.currency}
                  onValueChange={(value) =>
                    setNewStructure({
                      ...newStructure,
                      currency: value as CurrencyCode,
                    })
                  }
                >
                  <SelectTrigger>
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

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("plans.ratePerRequest")}
              </label>
              <Input
                value={newRate}
                onChange={(event) => setNewRate(event.target.value)}
                inputMode="decimal"
                className="font-mono"
              />
              {rateValid ? (
                <p className="text-xs text-slate-500">
                  {banded ? (
                    <>
                      Will be quoted as{" "}
                      {rateSentence(
                        draftHeadlineRate(Number(newRate), newStructure),
                        newStructure.currency
                      )}
                      — the headline only, restated from the catch-all
                      price&apos;s bottom band so the plan can be given in one
                      figure. The invoice is computed from the structure below
                      rather than from it.
                    </>
                  ) : (
                    <>
                      Version 1 will be{" "}
                      {rateSentence(Number(newRate), newStructure.currency)}. Zero is
                      legal — an organisation on a free plan is invoiceable for
                      nothing, which is a different state from one on no plan.
                    </>
                  )}
                </p>
              ) : (
                <p className="text-xs text-critical-500">
                  {t("plans.mustBeANumberAnd")}
                </p>
              )}
            </div>

            {!banded ? (
              <button
                type="button"
                onClick={() => {
                  setBanded(true);
                  setNewStructure({
                    currency: newStructure.currency,
                    platformFee: "",
                    minimumCharge: "",
                    prices: [
                      {
                        billableResource: null,
                        pricingModel: "Flat",
                        freeAllowance: "",
                        tiers: [
                          {
                            fromQuantity: "0",
                            toQuantity: "",
                            unitAmount: newRate,
                            flatAmount: "",
                          },
                        ],
                      },
                    ],
                  });
                }}
                className="text-xs text-brand-900 underline underline-offset-2 hover:text-brand-800"
              >
                {t("plans.priceThisInBands")}
              </button>
            ) : (
              <div className="space-y-3 rounded-lg border border-slate-200 p-3">
                <div className="flex flex-wrap items-baseline justify-between gap-2">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
                    {t("plans.priceStructure")}
                  </p>
                  <button
                    type="button"
                    onClick={() => {
                      setBanded(false);
                      setNewStructure(NO_STRUCTURE);
                    }}
                    className="text-xs text-slate-500 underline underline-offset-2 hover:text-slate-900"
                  >
                    {t("plans.useTheSingleRateInstead")}
                  </button>
                </div>

                <PriceBuilder
                  draft={newStructure}
                  onChange={setNewStructure}
                  disabled={saving}
                />
              </div>
            )}
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)}>
              {t("common.cancel")}
            </Button>
            <Button onClick={handleCreate} disabled={createBlocked}>
              {saving ? "Creating..." : "Create"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default Plans;
