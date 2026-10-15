"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { FlaskConical, Play, RefreshCw, RotateCcw, Send, TriangleAlert } from "lucide-react";
import { toast } from "sonner";

import { useAuth } from "@/context/AuthContext";
import { useEnvironment } from "@/context/ModeContext";
import { usePermissions } from "@/context/PermissionsContext";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { readRetryAfterMs } from "@/lib/rateLimit";
import {
  MAX_SIMULATION_PAYMENT_COUNT,
  SANDBOX_EVALUATION_LIMIT_PER_MINUTE,
  SANDBOX_RESET_CONFIRMATION,
  isSandboxExhausted,
  isSandboxResetConfirmation,
  sandboxCreditPercentRemaining,
  sandboxRateLimitDescription,
  sandboxResetDescription,
  subscribeToSandboxCredit,
} from "@/lib/sandbox";
import { Paged, SandboxCreditResponse, SandboxResetResponse, SimulationResource, WebhookDeliveryResource, WebhookDeliveryResponse } from "@/types";
import Pagination from "@/components/Pagination";
import { StatusBadge } from "@/components/StatusBadge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";

import { LocalTime } from "@/components/LocalTime";
import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";

const PAGE_SIZE = 5;
const ALLOWANCE_FIELD_LABELS = { "Body.SandboxMonthlyCredits": "Monthly credit allowance" };
const ACTIVE_STATUSES = new Set(["Pending", "Running"]);
const SCENARIOS = ["Baseline", "Structuring", "VelocitySpike", "HighValue", "Mixed"] as const;

interface SandboxPanelProps {
  organizationId?: string;
  organizationName?: string;
  canConfigureAllowance?: boolean;
}

export function SandboxPanel({ organizationId, organizationName, canConfigureAllowance = false }: SandboxPanelProps = {}) {
  const t = useTranslation();
  const format = useFormat();
  const router = useRouter();
  const { user } = useAuth();
  const { holds } = usePermissions();
  const { mode, refreshEnvironment } = useEnvironment();
  const targetId = organizationId ?? user?.organizationId;
  const targetName = organizationName ?? user?.organization?.friendlyName ?? "this organization";
  const onUnauthorized = useCallback(
    () => router.push("/auth/login?returnUrl=%2Forganization%23Sandbox"),
    [router]
  );

  const [credit, setCredit] = useState<SandboxCreditResponse | null>(null);
  const [creditLoading, setCreditLoading] = useState(true);
  const [creditError, setCreditError] = useState(false);
  const [allowanceDraft, setAllowanceDraft] = useState("");
  const [allowanceTouched, setAllowanceTouched] = useState(false);
  const [allowanceSaving, setAllowanceSaving] = useState(false);
  const [scenario, setScenario] = useState<(typeof SCENARIOS)[number]>("Baseline");
  const [paymentCount, setPaymentCount] = useState("25");
  const [seed, setSeed] = useState("");
  const [starting, setStarting] = useState(false);
  const [simulations, setSimulations] = useState<SimulationResource[]>([]);
  const [simulationPage, setSimulationPage] = useState(1);
  const [simulationPages, setSimulationPages] = useState(1);
  const [simulationsLoading, setSimulationsLoading] = useState(true);
  const [simulationsError, setSimulationsError] = useState(false);
  const [deliveries, setDeliveries] = useState<WebhookDeliveryResource[]>([]);
  const [deliveryPage, setDeliveryPage] = useState(1);
  const [deliveryPages, setDeliveryPages] = useState(1);
  const [deliveriesLoading, setDeliveriesLoading] = useState(true);
  const [deliveriesError, setDeliveriesError] = useState(false);
  const [replayingId, setReplayingId] = useState<string | null>(null);
  const [inspectedDelivery, setInspectedDelivery] = useState<WebhookDeliveryResponse | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [resetConfirmation, setResetConfirmation] = useState("");
  const [resetting, setResetting] = useState(false);

  const loadCredit = useCallback(async (quiet = false) => {
    if (mode !== "test" || !targetId) return;
    if (!quiet) setCreditLoading(true);
    setCreditError(false);
    try {
      const response = await apiFetch(`${API_BASE}/organizations/${targetId}/sandbox/credits`);
      if (!response.ok) {
        setCreditError(true);
        if (!quiet) await reportApiError(response, { fallback: t("organization.unableLoadSandboxUsage"), onUnauthorized });
        return;
      }
      const value = await unwrap<SandboxCreditResponse>(response);
      setCredit(value);
      if (!allowanceTouched) setAllowanceDraft(String(value.allowance));
    } catch {
      setCreditError(true);
    } finally {
      if (!quiet) setCreditLoading(false);
    }
  }, [allowanceTouched, mode, onUnauthorized, targetId]);

  const loadSimulations = useCallback(async (quiet = false) => {
    if (mode !== "test" || !targetId) return;
    if (!quiet) setSimulationsLoading(true);
    setSimulationsError(false);
    try {
      const response = await apiFetch(`${API_BASE}/organizations/${targetId}/sandbox/simulations?pageNumber=${simulationPage}&pageSize=${PAGE_SIZE}`);
      if (!response.ok) {
        setSimulationsError(true);
        if (!quiet) await reportApiError(response, { fallback: t("organization.unableLoadSimulations"), onUnauthorized });
        return;
      }
      const page = (await response.json()) as Paged<SimulationResource>;
      setSimulations(page.data);
      setSimulationPages(Math.max(1, page.metadata.pageCount));
    } catch {
      setSimulationsError(true);
    } finally {
      if (!quiet) setSimulationsLoading(false);
    }
  }, [mode, onUnauthorized, simulationPage, targetId]);

  const loadDeliveries = useCallback(async () => {
    if (mode !== "test" || !targetId) return;
    setDeliveriesLoading(true);
    setDeliveriesError(false);
    try {
      const response = await apiFetch(`${API_BASE}/organizations/${targetId}/sandbox/webhook-deliveries?pageNumber=${deliveryPage}&pageSize=${PAGE_SIZE}`);
      if (!response.ok) {
        setDeliveriesError(true);
        await reportApiError(response, { fallback: t("organization.unableLoadWebhookDeliveries"), onUnauthorized });
        return;
      }
      const page = (await response.json()) as Paged<WebhookDeliveryResource>;
      setDeliveries(page.data);
      setDeliveryPages(Math.max(1, page.metadata.pageCount));
    } catch {
      setDeliveriesError(true);
    } finally {
      setDeliveriesLoading(false);
    }
  }, [deliveryPage, mode, onUnauthorized, targetId]);

  useEffect(() => { void loadCredit(); }, [loadCredit]);
  useEffect(() => subscribeToSandboxCredit(() => { void loadCredit(true); }), [loadCredit]);
  useEffect(() => { void loadSimulations(); }, [loadSimulations]);
  useEffect(() => { void loadDeliveries(); }, [loadDeliveries]);

  const hasActiveSimulation = useMemo(
    () => simulations.some(({ simulation }) => ACTIVE_STATUSES.has(simulation.status)),
    [simulations]
  );

  useEffect(() => {
    if (!hasActiveSimulation) return;
    const timer = window.setInterval(() => {
      void loadSimulations(true);
      void loadCredit(true);
    }, 3000);
    return () => window.clearInterval(timer);
  }, [hasActiveSimulation, loadCredit, loadSimulations]);

  const handleSandboxFailure = async (response: Response, fallback: string) => {
    if (response.status === 402) {
      return;
    }
    if (response.status === 429) {
      toast.error(t("common.sandboxEvaluationLimitReached"), { description: sandboxRateLimitDescription(readRetryAfterMs(response)) });
      return;
    }
    await reportApiError(response, { fallback, onUnauthorized });
  };

  const startSimulation = async () => {
    if (!targetId) return;
    const count = Number(paymentCount);
    const chosenSeed = seed.trim() === "" ? null : Number(seed);
    if (!Number.isInteger(count) || count < 1 || count > MAX_SIMULATION_PAYMENT_COUNT) {
      toast.error(t(
        "organization.transactionCountBetween",
        format.count(MAX_SIMULATION_PAYMENT_COUNT)
      ));
      return;
    }
    if (chosenSeed !== null && (!Number.isInteger(chosenSeed) || chosenSeed < 0)) {
      toast.error(t("organization.seedMustNonNegativeWhole"));
      return;
    }
    setStarting(true);
    try {
      const body: { scenario: string; paymentCount: number; seed?: number } = { scenario, paymentCount: count };
      if (chosenSeed !== null) body.seed = chosenSeed;
      const response = await apiFetch(`${API_BASE}/organizations/${targetId}/sandbox/simulations`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body),
      });
      if (!response.ok) {
        await handleSandboxFailure(response, "Unable to start simulation");
        return;
      }
      const created = await unwrap<SimulationResource>(response);
      setSimulations((current) => [created, ...current.filter(({ simulation }) => simulation.simulationId !== created.simulation.simulationId)].slice(0, PAGE_SIZE));
      setSeed(String(created.simulation.seed));
      if (created.simulation.status === "Failed") {
        toast.error(t("organization.simulationCouldNotStarted"), { description: created.simulation.failureReason ?? t("organization.runRecordedFailedBeforeAny") });
      } else {
        toast.success(t("organization.simulationStarted"), { description: t("organization.seedReproducesTraffic", created.simulation.seed) });
      }
      await loadCredit(true);
    } catch {
      toast.error(t("organization.unableStartSimulation"));
    } finally {
      setStarting(false);
    }
  };

  const replayDelivery = async (outboxMessageId: string) => {
    if (!targetId) return;
    setReplayingId(outboxMessageId);
    try {
      const response = await apiFetch(`${API_BASE}/organizations/${targetId}/sandbox/webhook-deliveries/${outboxMessageId}/replay`, { method: "POST" });
      if (!response.ok) {
        await handleSandboxFailure(response, "Unable to replay webhook delivery");
        return;
      }
      const replay = await unwrap<WebhookDeliveryResource>(response);
      setDeliveries((current) => current.map((item) => item.webhookDelivery.outboxMessageId === outboxMessageId ? replay : item));
      toast.success(t("organization.webhookQueuedReplay"));
    } catch {
      toast.error(t("organization.unableReplayWebhookDelivery"));
    } finally {
      setReplayingId(null);
    }
  };

  const resetSandbox = async () => {
    if (!targetId || !isSandboxResetConfirmation(resetConfirmation)) return;
    setResetting(true);
    try {
      const response = await apiFetch(`${API_BASE}/organizations/${targetId}/sandbox/reset`, { method: "POST" });
      if (!response.ok) {
        await reportApiError(response, { fallback: t("organization.sandboxResetFailed"), onUnauthorized });
        return;
      }
      const result = await unwrap<SandboxResetResponse>(response);
      const counts = result.counts.filter((item) => item.count > 0).map((item) => `${item.count} ${item.resource}`).join(", ");
      toast.success(t("organization.sandboxResetComplete"), { description: t(
        "organization.sandboxResetDescription",
        counts ||
          t("organization.testRecordsDeleted", format.count(result.totalDeleted))
      ) });
      setConfirmOpen(false);
      setResetConfirmation("");
      refreshEnvironment();
    } catch {
      toast.error(t("organization.sandboxResetFailed"));
    } finally {
      setResetting(false);
    }
  };

  const saveAllowance = async (useHouseDefault = false) => {
    if (!targetId) return;
    const value = Number(allowanceDraft);
    if (!useHouseDefault && (!Number.isInteger(value) || value < 0)) {
      toast.error(t("organization.monthlyCreditsMustNonNegative"));
      return;
    }
    setAllowanceSaving(true);
    try {
      const response = await apiFetch(`${API_BASE}/organizations/${targetId}`, {
        method: "PATCH",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ sandboxMonthlyCredits: useHouseDefault ? null : value }),
      });
      if (!response.ok) {
        await reportApiError(response, { fallback: t("organization.unableUpdateSandboxAllowance"), onUnauthorized, fieldLabels: ALLOWANCE_FIELD_LABELS });
        return;
      }
      setAllowanceTouched(false);
      toast.success(useHouseDefault ? t("organization.usingHouseSandboxAllowance") : t("organization.sandboxAllowanceUpdated"));
      await loadCredit(true);
    } catch {
      toast.error(t("organization.unableUpdateSandboxAllowance"));
    } finally {
      setAllowanceSaving(false);
    }
  };

  if (mode !== "test") {
    return <Card><CardContent className="flex items-center gap-3 p-6"><FlaskConical className="size-5 text-slate-400" /><p className="text-sm text-slate-600">{t("organization.switchToTestToSee")}</p></CardContent></Card>;
  }

  const percentRemaining = sandboxCreditPercentRemaining(credit);
  const exhausted = isSandboxExhausted(credit);

  return (
    <div className="space-y-6">
      <Card className="overflow-hidden border-attention-300">
        <div className="h-1 bg-attention-500" />
        <CardHeader><CardTitle className="flex items-center gap-2"><FlaskConical className="size-5 text-attention-700" />{t("organization.sandboxAllowance")}</CardTitle><p className="text-sm text-slate-600">{t("organization.testDataCredentialsTrafficAnd")}</p></CardHeader>
        <CardContent>
          {creditLoading ? <div className="space-y-3"><SkeletonLine /><div className="h-2 rounded-full bg-slate-100" /><SkeletonLine /></div> : creditError ? <InlineError message={t("organization.usageCouldNotLoaded")} onRetry={() => void loadCredit()} /> : credit ? (
            <div className="space-y-4">
              <div className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-sm text-slate-500">{t("organization.creditsRemaining")}</p><p className="font-mono text-3xl font-semibold text-slate-900">{credit.remaining}<span className="text-base font-normal text-slate-400"> / {credit.allowance}</span></p></div><div className="grid grid-cols-2 gap-x-6 gap-y-2"><div><p className="text-sm text-slate-500">{t("organization.usedThisMonth")}</p><p className="font-mono text-sm font-medium text-slate-700">{t("organization.sandboxCreditsUsed", format.count(credit.used))}</p></div><div><p className="text-sm text-slate-500">{t("organization.resets")}</p><p className="font-mono text-sm text-slate-700"><LocalTime value={credit.resetsAt} /></p></div></div></div>
              <div className="h-2 overflow-hidden rounded-full bg-slate-100" role="progressbar" aria-label={`${Math.round(percentRemaining)} percent of Sandbox credits remaining`} aria-valuemin={0} aria-valuemax={Math.max(1, credit.allowance)} aria-valuenow={credit.remaining}><div className={`h-full rounded-full ${exhausted ? "bg-critical-solid" : "bg-attention-500"}`} style={{ width: `${percentRemaining}%` }} /></div>
              <p className="text-sm text-slate-600">{t("organization.oneRulePassOneCredit", SANDBOX_EVALUATION_LIMIT_PER_MINUTE)}</p>
              {canConfigureAllowance && <div className="flex flex-wrap items-end gap-2 border-t border-slate-100 pt-4"><div className="min-w-52 flex-1 space-y-1.5"><label className="text-sm font-medium text-slate-700" htmlFor="sandbox-monthly-allowance">{t("organization.monthlyCreditAllowance")}</label><Input id="sandbox-monthly-allowance" type="number" min={0} value={allowanceDraft} onChange={(event) => { setAllowanceDraft(event.target.value); setAllowanceTouched(true); }} disabled={allowanceSaving} /></div><Button size="sm" onClick={() => void saveAllowance()} disabled={!allowanceTouched || allowanceSaving}>{allowanceSaving ? "Saving…" : "Save allowance"}</Button><Button size="sm" variant="outline" onClick={() => void saveAllowance(true)} disabled={allowanceSaving}>{t("organization.useHouseDefault")}</Button></div>}
              {exhausted && <div className="rounded-md border border-critical-200 bg-critical-50 p-3 text-sm text-critical-800">{t("organization.creditsAreExhaustedEveryTest")}</div>}
            </div>
          ) : <EmptyState text={t("organization.noSandboxAllowanceAvailable")} />}
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>{t("organization.trafficSimulator")}</CardTitle><p className="text-sm text-slate-600">{t("organization.generateReproducibleTestTransactionsShaped")}</p></CardHeader>
        <CardContent className="space-y-5">
          {holds("sandbox:write") ? <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-[1fr_8rem_8rem_auto]"><Select value={scenario} onValueChange={(value) => setScenario(value as (typeof SCENARIOS)[number])}><SelectTrigger aria-label={t("organization.simulationScenario")}><SelectValue /></SelectTrigger><SelectContent>{SCENARIOS.map((value) => <SelectItem key={value} value={value}>{value}</SelectItem>)}</SelectContent></Select><Input type="number" min={1} max={MAX_SIMULATION_PAYMENT_COUNT} value={paymentCount} onChange={(event) => setPaymentCount(event.target.value)} aria-label={t("organization.transactionCount")} placeholder={t("organization.transactions")} /><Input type="number" min={0} value={seed} onChange={(event) => setSeed(event.target.value)} aria-label={t("organization.simulationSeed")} placeholder={t("organization.randomSeed")} /><Button onClick={() => void startSimulation()} disabled={starting || hasActiveSimulation || exhausted}><Play className="size-4" />{starting ? "Starting…" : hasActiveSimulation ? "Run in progress" : exhausted ? "No credits left" : "Run simulation"}</Button></div> : <p className="text-sm text-slate-500">{t("organization.youDoNotHavePermission")}</p>}
          {simulationsLoading ? <RowsSkeleton /> : simulationsError ? <InlineError message={t("organization.simulationsCouldNotLoaded")} onRetry={() => void loadSimulations()} /> : simulations.length === 0 ? <EmptyState text={t("organization.noSimulationsHaveRunTest")} /> : <SimulationTable resources={simulations} />}
          {simulationPages > 1 && <Pagination pageNumber={simulationPage} totalPages={simulationPages} handlePrevPage={() => setSimulationPage((page) => Math.max(1, page - 1))} handleNextPage={() => setSimulationPage((page) => Math.min(simulationPages, page + 1))} />}
        </CardContent>
      </Card>

      <Card>
        <CardHeader><CardTitle>{t("organization.webhookDeliveries")}</CardTitle><p className="text-sm text-slate-600">{t("organization.inspectWhatTheSandboxSent")}</p></CardHeader>
        <CardContent>
          {deliveriesLoading ? <RowsSkeleton /> : deliveriesError ? <InlineError message={t("organization.webhookDeliveriesCouldNotLoaded")} onRetry={() => void loadDeliveries()} /> : deliveries.length === 0 ? <EmptyState text={t("organization.noTestWebhookDeliveriesHave")} /> : <DeliveryTable resources={deliveries} canReplay={holds("sandbox:write")} replayingId={replayingId} onReplay={replayDelivery} onInspect={setInspectedDelivery} />}
          {deliveryPages > 1 && <Pagination pageNumber={deliveryPage} totalPages={deliveryPages} handlePrevPage={() => setDeliveryPage((page) => Math.max(1, page - 1))} handleNextPage={() => setDeliveryPage((page) => Math.min(deliveryPages, page + 1))} />}
        </CardContent>
      </Card>

      <Card><CardHeader><CardTitle>{t("organization.resetSandbox")}</CardTitle><p className="text-sm text-slate-600">{t("organization.emptyThisTestEnvironmentOf")}</p></CardHeader><CardContent>{holds("sandbox:reset") ? <Button variant="outline" onClick={() => { setResetConfirmation(""); setConfirmOpen(true); }}><RotateCcw className="size-4" />{t("organization.resetTestEnvironment")}</Button> : <p className="text-sm text-slate-500">{t("organization.youDoNotHavePermission2")}</p>}</CardContent></Card>
      <Dialog open={inspectedDelivery !== null} onOpenChange={(open) => { if (!open) setInspectedDelivery(null); }}><DialogContent className="sm:max-w-2xl"><DialogHeader><DialogTitle>{t("organization.webhookPayload")}</DialogTitle><DialogDescription className="break-all">{t("organization.sandboxSentTo", inspectedDelivery?.destination ?? "")}</DialogDescription></DialogHeader><pre className="max-h-[55vh] overflow-auto rounded-lg border border-console-border bg-console p-4 font-mono text-xs text-console-200 whitespace-pre-wrap break-words">{inspectedDelivery?.payload}</pre><DialogFooter><Button variant="outline" onClick={() => { if (inspectedDelivery) void navigator.clipboard.writeText(inspectedDelivery.payload).then(() => toast.success(t("organization.payloadCopied"))).catch(() => toast.error(t("common.copyingIsnTAvailableBrowser"))); }}>{t("organization.copyPayload")}</Button><Button onClick={() => setInspectedDelivery(null)}>{t("organization.close")}</Button></DialogFooter></DialogContent></Dialog>
      <Dialog open={confirmOpen} onOpenChange={(open) => { setConfirmOpen(open); if (!open) setResetConfirmation(""); }}><DialogContent><DialogHeader><DialogTitle>{t("organization.resetTestEnvironment2")}</DialogTitle><DialogDescription>{sandboxResetDescription(targetName)}</DialogDescription></DialogHeader><div className="space-y-1.5"><label className="text-sm font-medium text-slate-700" htmlFor="sandbox-reset-confirmation">{t("organization.type")} <span className="font-mono">{SANDBOX_RESET_CONFIRMATION}</span> to confirm</label><Input id="sandbox-reset-confirmation" value={resetConfirmation} onChange={(event) => setResetConfirmation(event.target.value)} autoComplete="off" disabled={resetting} /></div><DialogFooter><Button variant="outline" onClick={() => setConfirmOpen(false)} disabled={resetting}>{t("common.cancel")}</Button><Button variant="destructive" onClick={() => void resetSandbox()} disabled={resetting || !isSandboxResetConfirmation(resetConfirmation)}>{resetting ? "Resetting…" : "Delete Test data"}</Button></DialogFooter></DialogContent></Dialog>
    </div>
  );
}

function SimulationTable({ resources }: { resources: SimulationResource[] }) {
  const t = useTranslation();
  return <div className="overflow-x-auto"><Table><TableHeader><TableRow><TableHead>{t("organization.scenario")}</TableHead><TableHead>{t("organization.progress")}</TableHead><TableHead>{t("organization.seed")}</TableHead><TableHead>{t("common.status")}</TableHead><TableHead>{t("common.started")}</TableHead></TableRow></TableHeader><TableBody>{resources.map(({ simulation }) => <TableRow key={simulation.simulationId}><TableCell className="font-medium">{simulation.scenario}</TableCell><TableCell className="font-mono text-xs">{simulation.processedCount} / {simulation.totalPayments}</TableCell><TableCell className="font-mono text-xs">{simulation.seed}</TableCell><TableCell><StatusBadge status={simulation.status} />{simulation.failureReason && <p className="mt-1 max-w-xs text-xs text-critical-700">{simulation.failureReason}</p>}</TableCell><TableCell className="text-slate-500"><LocalTime value={simulation.createdAt} /></TableCell></TableRow>)}</TableBody></Table></div>;
}

function DeliveryTable({ resources, canReplay, replayingId, onReplay, onInspect }: { resources: WebhookDeliveryResource[]; canReplay: boolean; replayingId: string | null; onReplay: (id: string) => Promise<void>; onInspect: (delivery: WebhookDeliveryResponse) => void }) {
  const t = useTranslation();
  return <div className="overflow-x-auto"><Table><TableHeader><TableRow><TableHead>{t("organization.destination")}</TableHead><TableHead>{t("organization.attempts")}</TableHead><TableHead>{t("common.state")}</TableHead><TableHead>{t("common.created")}</TableHead><TableHead><span className="sr-only">{t("common.actions")}</span></TableHead></TableRow></TableHeader><TableBody>{resources.map(({ webhookDelivery }) => <TableRow key={webhookDelivery.outboxMessageId}><TableCell className="max-w-xs truncate font-mono text-xs" title={webhookDelivery.destination}>{webhookDelivery.destination}</TableCell><TableCell className="font-mono text-xs">{webhookDelivery.attempts}</TableCell><TableCell><StatusBadge status={webhookDelivery.isDelivered ? "Completed" : webhookDelivery.suppressedAt ? "Disabled" : "Pending"} /></TableCell><TableCell className="text-slate-500"><LocalTime value={webhookDelivery.createdAt} /></TableCell><TableCell><div className="flex justify-end gap-2"><Button size="sm" variant="ghost" onClick={() => onInspect(webhookDelivery)}>{t("organization.viewPayload")}</Button>{canReplay && <Button size="sm" variant="outline" onClick={() => void onReplay(webhookDelivery.outboxMessageId)} disabled={replayingId === webhookDelivery.outboxMessageId}><Send className="size-4" />{replayingId === webhookDelivery.outboxMessageId ? "Queuing…" : "Replay"}</Button>}</div></TableCell></TableRow>)}</TableBody></Table></div>;
}

function InlineError({ message, onRetry }: { message: string; onRetry: () => void }) {
  const t = useTranslation();
  return <div className="flex flex-wrap items-center gap-3 rounded-md border border-critical-200 bg-critical-50 p-3 text-sm text-critical-800"><TriangleAlert className="size-4" /><span>{message}</span><Button variant="outline" size="sm" onClick={onRetry}><RefreshCw className="size-4" />{t("organization.retry")}</Button></div>;
}

function EmptyState({ text }: { text: string }) {
  return <div className="rounded-md border border-dashed border-slate-200 px-4 py-8 text-center text-sm text-slate-500">{text}</div>;
}

function RowsSkeleton() {
  return <div className="space-y-2"><div className="h-10 animate-pulse rounded bg-slate-100" /><div className="h-10 animate-pulse rounded bg-slate-100" /><div className="h-10 animate-pulse rounded bg-slate-100" /></div>;
}

function SkeletonLine() {
  return <div className="h-6 w-56 animate-pulse rounded bg-slate-100" />;
}
