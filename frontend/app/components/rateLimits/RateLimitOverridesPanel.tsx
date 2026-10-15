"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import { Gauge, Plus, TriangleAlert, Undo2 } from "lucide-react";

import { RateLimitOverride, RateLimitOverridePool } from "@/types";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  ALL_POOLS,
  ALL_POOLS_HINT,
  APPLIED_SHORTLY,
  DEFAULT_EXPIRY_HOURS,
  EXPIRY_PRESETS,
  OVERRIDE_POOLS,
  POOL_HINTS,
  REASON_HINT,
  REASON_MAX_LENGTH,
  SETTLE_DELAY_MS,
  bucketSentence,
  effectiveByPool,
  expiresAtFrom,
  hasExpired,
  isValidExpiry,
  isValidLimit,
  isValidReason,
  poolLabel,
  poolsInForce,
} from "@/lib/rateLimitOverrides";
import { apiFetch } from "@/lib/http";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { cn } from "@/lib/utils";
import Pagination from "@/components/Pagination";
import { LocalTime } from "@/components/LocalTime";
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

const POOL_FILTER_ANY = "Any";

const IN_FORCE_PAGE_SIZE = 100;

export function RateLimitOverridesPanel({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const submission = useIdempotencyKey();

  const [overrides, setOverrides] = useState<RateLimitOverride[]>([]);
  const [inForce, setInForce] = useState<RateLimitOverride[]>([]);

  const [poolFilter, setPoolFilter] = useState<string>(POOL_FILTER_ANY);
  const [includeExpired, setIncludeExpired] = useState(false);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(25);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [createOpen, setCreateOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [liftTarget, setLiftTarget] = useState<RateLimitOverride | null>(null);
  const [lifting, setLifting] = useState(false);

  const [newPool, setNewPool] = useState<string>(ALL_POOLS);
  const [newPermitLimit, setNewPermitLimit] = useState("60");
  const [newTokensPerPeriod, setNewTokensPerPeriod] = useState("60");
  const [newPeriodSeconds, setNewPeriodSeconds] = useState("60");
  const [newReason, setNewReason] = useState("");
  const [newExpiryHours, setNewExpiryHours] = useState(DEFAULT_EXPIRY_HOURS);

  const settleTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const fetchOverrides = useCallback(async () => {
    setLoading(true);

    try {
      const query = new URLSearchParams({
        PageNumber: String(pageNumber),
        PageSize: String(pageSize),
      });
      if (poolFilter !== POOL_FILTER_ANY) query.set("Pool", poolFilter);
      if (includeExpired) query.set("IncludeExpired", "true");

      const [listed, live] = await Promise.all([
        apiFetch(
          `${API_BASE}/organizations/${organizationId}/rate-limit-overrides?${query}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        ),
        apiFetch(
          `${API_BASE}/organizations/${organizationId}/rate-limit-overrides?PageNumber=1&PageSize=${IN_FORCE_PAGE_SIZE}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        ),
      ]);

      if (listed.ok) {
        const page = await unwrapPage<RateLimitOverride>(listed);
        setOverrides(page.data);
        setTotalPages(page.metadata.pageCount);
      } else {
        await reportApiError(listed, {
          fallback: t("rateLimits.errorOccurredFetchingRateLimit"),
          onUnauthorized,
          toastId: "error-fetching-rate-limit-overrides",
        });
      }

      if (live.ok) {
        const page = await unwrapPage<RateLimitOverride>(live);
        setInForce(page.data);
      } else {
        setInForce([]);
      }
    } catch {
      toast.error(t("rateLimits.errorOccurredFetchingRateLimit"), {
        id: "error-fetching-rate-limit-overrides",
      });
    } finally {
      setLoading(false);
    }
  }, [
    organizationId,
    pageNumber,
    pageSize,
    poolFilter,
    includeExpired,
    onUnauthorized,
  ]);

  useEffect(() => {
    fetchOverrides();
  }, [fetchOverrides]);

  useEffect(
    () => () => {
      if (settleTimer.current) clearTimeout(settleTimer.current);
    },
    []
  );

  const scheduleSettle = useCallback(() => {
    if (settleTimer.current) clearTimeout(settleTimer.current);
    settleTimer.current = setTimeout(() => {
      settleTimer.current = null;
      fetchOverrides();
    }, SETTLE_DELAY_MS);
  }, [fetchOverrides]);

  const resetCreateForm = () => {
    setNewPool(ALL_POOLS);
    setNewPermitLimit("60");
    setNewTokensPerPeriod("60");
    setNewPeriodSeconds("60");
    setNewReason("");
    setNewExpiryHours(DEFAULT_EXPIRY_HOURS);
  };

  const handleCreate = async () => {
    const expiresAt = expiresAtFrom(newExpiryHours);

    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/rate-limit-overrides`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({
            ...(newPool === ALL_POOLS ? {} : { pool: newPool }),
            permitLimit: Number(newPermitLimit),
            tokensPerPeriod: Number(newTokensPerPeriod),
            replenishmentPeriodSeconds: Number(newPeriodSeconds),
            reason: newReason.trim(),
            expiresAt,
          }),
        },
        { idempotencyKey: submission.current("create") }
      );

      if (response.ok) {
        const created = await unwrap<RateLimitOverride>(response);
        submission.completed("create");

        toast.success(
          t("rateLimits.limitsBeingApplied", poolLabel(created.pool)),
          { description: APPLIED_SHORTLY }
        );

        setCreateOpen(false);
        resetCreateForm();
        await fetchOverrides();
        scheduleSettle();
      } else {
        await reportApiError(response, {
          fallback: t("rateLimits.failedApplyRateLimitOverride"),
          onUnauthorized,
          fieldLabels: {
            "Body.Pool": "Pool",
            "Body.PermitLimit": "Burst allowance",
            "Body.TokensPerPeriod": "Refill rate",
            "Body.ReplenishmentPeriodSeconds": "Refill period",
            "Body.Reason": "Reason",
            "Body.ExpiresAt": "Expires",
          },
        });
      }
    } catch {
      toast.error(t("rateLimits.failedApplyRateLimitOverride"));
    } finally {
      setSaving(false);
    }
  };

  const handleLift = async () => {
    if (!liftTarget) return;

    const { rateLimitOverrideId } = liftTarget;
    setLifting(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/rate-limit-overrides/${rateLimitOverrideId}`,
        {
          method: "DELETE",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: submission.current(`lift:${rateLimitOverrideId}`) }
      );

      if (response.ok) {
        submission.completed(`lift:${rateLimitOverrideId}`);

        toast.success(
          t("rateLimits.overrideBeingLifted", poolLabel(liftTarget.pool)),
          { description: APPLIED_SHORTLY }
        );

        setLiftTarget(null);
        await fetchOverrides();
        scheduleSettle();
      } else {
        await reportApiError(response, {
          fallback: t("rateLimits.failedLiftRateLimitOverride"),
          onUnauthorized,
        });
      }
    } catch {
      toast.error(t("rateLimits.failedLiftRateLimitOverride"));
    } finally {
      setLifting(false);
    }
  };

  const permitLimitValid = isValidLimit(Number(newPermitLimit));
  const tokensValid = isValidLimit(Number(newTokensPerPeriod));
  const periodValid = isValidLimit(Number(newPeriodSeconds));
  const reasonValid = isValidReason(newReason);
  const expiryValid = isValidExpiry(expiresAtFrom(newExpiryHours));

  const createBlocked =
    saving ||
    !permitLimitValid ||
    !tokensValid ||
    !periodValid ||
    !reasonValid ||
    !expiryValid;

  const effective = effectiveByPool(inForce);
  const anyInForce = OVERRIDE_POOLS.some((pool) => effective[pool] !== null);

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <p className="text-sm text-slate-500">
          What this tenant may spend against the API, when the configured
          ceiling is not the right number for what is happening right now. An
          override replaces the <span className="font-medium">organization
          ceiling</span> — the limit across every API client the tenant has
          issued — rather than any one client&apos;s budget, and it applies to
          their integrations rather than to people signed in at the console.
        </p>
        <p className="text-sm text-slate-500">
          Every change here is audited and every one needs a reason. Overrides
          expire on their own; lifting one early is for when the incident ends
          sooner than you guessed.
        </p>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        {OVERRIDE_POOLS.map((pool) => {
          const override = effective[pool];
          const viaBlanket = override !== null && override.pool === null;

          return (
            <div
              key={pool}
              className={cn(
                "rounded-xl border p-4",
                override
                  ? "border-elevated-200 bg-elevated-50"
                  : "border-slate-200 bg-card"
              )}
            >
              <div className="flex items-center justify-between gap-2">
                <span className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {pool}
                </span>
                {override && (
                  <Gauge className="size-3.5 text-elevated-700" />
                )}
              </div>

              {loading ? (
                <Skeleton className="mt-2 h-4 w-32" />
              ) : override ? (
                <>
                  <p className="mt-2 font-mono text-sm text-slate-900">
                    {bucketSentence(override)}
                  </p>
                  <p className="mt-1 text-xs text-elevated-700">
                    {viaBlanket
                      ? "From the all-pools override"
                      : "Set for this pool"}
                    {" · until "}
                    <LocalTime value={override.expiresAt} />
                  </p>
                </>
              ) : (
                <>
                  <p className="mt-2 text-sm text-slate-500">
                    {t("rateLimits.configuredDefault")}
                  </p>
                  <p className="mt-1 text-xs text-slate-400">
                    Nothing overriding it. The installation&apos;s own ceiling
                    applies; this screen cannot read what that is.
                  </p>
                </>
              )}
            </div>
          );
        })}
      </div>

      <div className="flex flex-wrap items-end justify-between gap-3">
        <div className="flex flex-wrap items-center gap-3">
          <Select
            value={poolFilter}
            onValueChange={(next) => {
              setPoolFilter(next);
              setPageNumber(1);
            }}
          >
            <SelectTrigger className="w-44">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={POOL_FILTER_ANY}>{t("rateLimits.everyOverride")}</SelectItem>
              {OVERRIDE_POOLS.map((pool) => (
                <SelectItem key={pool} value={pool}>
                  {pool} only
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

        <Button
          size="sm"
          className="gap-1.5"
          onClick={() => setCreateOpen(true)}
        >
          <Plus className="size-4" />
          {t("rateLimits.newOverride")}
        </Button>
      </div>

      <PolicySwitch
        label={t("rateLimits.showExpiredOverrides")}
        checked={includeExpired}
        onChange={(next) => {
          setIncludeExpired(next);
          setPageNumber(1);
        }}
      >
        Off by default, because an expired override is not doing anything. Turn
        it on to answer what a tenant&apos;s limits were at the time.
      </PolicySwitch>

      {poolFilter !== POOL_FILTER_ANY && (
        <p className="flex items-start gap-2 rounded-lg border border-attention-200 bg-attention-50 p-3 text-xs text-attention-700">
          <TriangleAlert className="mt-0.5 size-3.5 flex-shrink-0" />
          <span>
            This filter matches an override&apos;s own pool, so an all-pools
            override is hidden by it even when it is the one governing{" "}
            {poolFilter}. The summary above is unfiltered and still correct.
          </span>
        </p>
      )}

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead className="w-32">{t("rateLimits.pool")}</TableHead>
              <TableHead className="w-56">{t("rateLimits.limit")}</TableHead>
              <TableHead className="w-40">{t("rateLimits.inForce")}</TableHead>
              <TableHead>{t("common.reason")}</TableHead>
              <TableHead className="w-44">{t("rateLimits.expires")}</TableHead>
              <TableHead className="w-24">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 3 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: 6 }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : overrides.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} className="py-14 text-center">
                  <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                    {poolFilter !== POOL_FILTER_ANY || includeExpired
                      ? "No results found"
                      : "No overrides in force"}
                  </p>
                  {poolFilter === POOL_FILTER_ANY && !includeExpired && (
                    <p className="mx-auto mt-3 max-w-md text-sm text-slate-500">
                      This tenant is on the installation&apos;s configured
                      limits.
                      {anyInForce
                        ? " Something is in force but did not load into this page."
                        : " Nothing here is clamped or raised."}
                    </p>
                  )}
                </TableCell>
              </TableRow>
            ) : (
              overrides.map((override) => {
                const expired = hasExpired(override);
                const governing = expired
                  ? []
                  : poolsInForce(override, inForce);

                return (
                  <TableRow
                    key={override.rateLimitOverrideId}
                    className={cn(
                      "hover:bg-slate-50/50",
                      expired && "text-slate-400"
                    )}
                  >
                    <TableCell>
                      <span
                        className={cn(
                          "rounded-full border px-2 py-0.5 font-mono text-xs",
                          override.pool === null
                            ?
                              "border-elevated-200 bg-elevated-50 text-elevated-700"
                            : "border-slate-200 bg-slate-50 text-slate-700"
                        )}
                      >
                        {poolLabel(override.pool)}
                      </span>
                    </TableCell>

                    <TableCell className="font-mono text-xs text-slate-700">
                      {bucketSentence(override)}
                    </TableCell>

                    <TableCell>
                      {expired ? (
                        <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
                          expired
                        </span>
                      ) : governing.length === 0 ? (
                        <span
                          className="font-mono text-[11px] uppercase tracking-widest text-slate-400"
                          title={t("rateLimits.aNarrowerOverrideCoversEvery")}
                        >
                          superseded
                        </span>
                      ) : (
                        <span className="font-mono text-[11px] uppercase tracking-widest text-elevated-700">
                          {governing.join(", ")}
                        </span>
                      )}
                    </TableCell>

                    <TableCell className="max-w-sm">
                      <p
                        className="truncate text-sm text-slate-700"
                        title={override.reason}
                      >
                        {override.reason}
                      </p>
                      <p className="text-xs text-slate-400">
                        {t("rateLimits.set")} <LocalTime value={override.createdAt} />
                      </p>
                    </TableCell>

                    <TableCell className="text-xs text-slate-500">
                      <LocalTime value={override.expiresAt} />
                    </TableCell>

                    <TableCell>
                      {expired ? (
                        <span className="font-mono text-[11px] uppercase tracking-widest text-slate-300">
                          —
                        </span>
                      ) : (
                        <Button
                          variant="ghost"
                          size="sm"
                          className="gap-1.5 text-slate-500 hover:text-slate-900"
                          onClick={() => setLiftTarget(override)}
                        >
                          <Undo2 className="size-4" />
                          {t("rateLimits.lift")}
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {overrides.length > 0 && (
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
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("rateLimits.newRateLimitOverride")}</DialogTitle>
            <DialogDescription>
              Replaces this tenant&apos;s organization ceiling for as long as it
              runs. It takes effect within about fifteen seconds, not
              immediately.
            </DialogDescription>
          </DialogHeader>

          <div className="max-h-[60vh] space-y-4 overflow-y-auto pr-1">
            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("rateLimits.pool")}
              </label>
              <Select value={newPool} onValueChange={setNewPool}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={ALL_POOLS}>{t("rateLimits.allPools")}</SelectItem>
                  {OVERRIDE_POOLS.map((pool) => (
                    <SelectItem key={pool} value={pool}>
                      {pool}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-500">
                {newPool === ALL_POOLS
                  ? ALL_POOLS_HINT
                  : POOL_HINTS[newPool as RateLimitOverridePool]}
              </p>
            </div>

            <div className="space-y-3 rounded-lg border border-slate-200 p-3">
              <p className="text-xs text-slate-500">
                A bucket, not a quota. The burst allowance is how much may be
                spent at once; the refill rate is how much comes back each
                period. Setting the refill below the burst is normal — it lets a
                quiet client catch up without letting a busy one run away.
              </p>

              <div className="grid gap-3 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                    {t("rateLimits.burstAllowance")}
                  </label>
                  <Input
                    value={newPermitLimit}
                    onChange={(event) => setNewPermitLimit(event.target.value)}
                    inputMode="numeric"
                    className="font-mono"
                  />
                  {!permitLimitValid && (
                    <p className="text-xs text-critical-500">
                      {t("rateLimits.mustBeAWholeNumber")}
                    </p>
                  )}
                </div>

                <div className="space-y-1.5">
                  <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                    {t("rateLimits.refillRate")}
                  </label>
                  <Input
                    value={newTokensPerPeriod}
                    onChange={(event) =>
                      setNewTokensPerPeriod(event.target.value)
                    }
                    inputMode="numeric"
                    className="font-mono"
                  />
                  {!tokensValid && (
                    <p className="text-xs text-critical-500">
                      {t("rateLimits.mustBeAWholeNumber")}
                    </p>
                  )}
                </div>
              </div>

              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                  {t("rateLimits.refillPeriodSeconds")}
                </label>
                <Input
                  value={newPeriodSeconds}
                  onChange={(event) => setNewPeriodSeconds(event.target.value)}
                  inputMode="numeric"
                  className="font-mono"
                />
                {!periodValid && (
                  <p className="text-xs text-critical-500">
                    {t("rateLimits.mustBeAWholeNumber")}
                  </p>
                )}
              </div>

              {permitLimitValid && tokensValid && periodValid && (
                <p className="font-mono text-xs text-slate-700">
                  {bucketSentence({
                    permitLimit: Number(newPermitLimit),
                    tokensPerPeriod: Number(newTokensPerPeriod),
                    replenishmentPeriodSeconds: Number(newPeriodSeconds),
                  })}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("common.reason")}
              </label>
              <Textarea
                value={newReason}
                onChange={(event) => setNewReason(event.target.value)}
                placeholder={t("rateLimits.incident4021RetryLoopFrom")}
                maxLength={REASON_MAX_LENGTH}
                className="min-h-[72px]"
              />
              <div className="flex items-start justify-between gap-3">
                <p className="text-xs text-slate-500">{REASON_HINT}</p>
                <span
                  className={cn(
                    "font-mono text-xs tabular-nums",
                    newReason.length >= REASON_MAX_LENGTH
                      ? "text-critical-500"
                      : "text-slate-400"
                  )}
                >
                  {newReason.length}/{REASON_MAX_LENGTH}
                </span>
              </div>
              {newReason.length > 0 && !reasonValid && (
                <p className="text-xs text-critical-500">
                  {t("rateLimits.aReasonOfOnlySpaces")}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
                {t("rateLimits.runsFor")}
              </label>
              <div className="flex flex-wrap gap-1.5">
                {EXPIRY_PRESETS.map((preset) => (
                  <button
                    key={preset.hours}
                    type="button"
                    onClick={() => setNewExpiryHours(preset.hours)}
                    className={cn(
                      "rounded-full border px-3 py-1 font-mono text-xs transition-colors",
                      newExpiryHours === preset.hours
                        ? "border-brand-800 bg-brand-50 text-brand-900"
                        : "border-slate-200 bg-card text-slate-600 hover:bg-slate-50"
                    )}
                  >
                    {preset.label}
                  </button>
                ))}
              </div>
              <p className="text-xs text-slate-500">
                {EXPIRY_PRESETS.find(
                  (preset) => preset.hours === newExpiryHours
                )?.hint ?? ""}
              </p>
              <p className="text-xs text-slate-400">
                Expires{" "}
                <LocalTime value={expiresAtFrom(newExpiryHours)} />
              </p>
              {!expiryValid && (
                <p className="text-xs text-critical-500">
                  That expiry is outside what the API accepts — it must be in the
                  future and at most 30 days out.
                </p>
              )}
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
              {saving ? "Applying…" : "Apply override"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={liftTarget !== null}
        onOpenChange={(open) => !open && setLiftTarget(null)}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              Lift the {poolLabel(liftTarget?.pool ?? null).toLowerCase()}{" "}
              override?
            </DialogTitle>
            <DialogDescription>
              This tenant goes back to the installation&apos;s configured
              ceiling within about fifteen seconds. An override left alone
              expires on its own — lifting is for an incident that ended early.
            </DialogDescription>
          </DialogHeader>

          {liftTarget && (
            <div className="space-y-2 rounded-lg border border-slate-200 bg-slate-50 p-3">
              <p className="font-mono text-xs text-slate-700">
                {bucketSentence(liftTarget)}
              </p>
              <p className="text-sm text-slate-700">{liftTarget.reason}</p>
              <p className="text-xs text-slate-500">
                The record is kept — it is the only evidence the clamp was in
                force — but it leaves this list. After this, the audit trail is
                where to find it.
              </p>
            </div>
          )}

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setLiftTarget(null)}
              disabled={lifting}
            >
              {t("common.cancel")}
            </Button>
            <Button onClick={handleLift} disabled={lifting}>
              {lifting ? "Lifting…" : "Lift override"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
