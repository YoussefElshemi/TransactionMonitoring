"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import { AlertTriangle, Plus, RotateCcw, Trash2, Users } from "lucide-react";

import {
  AuditLogChange,
  PartyMapping,
  PartyMappingEntry,
  PartyMappingResource,
  PartyMappingUsage,
  PartyMappingVersion,
  PartyMappingVersionResource,
  PartyRole,
  PARTY_ROLES,
  SchemaField,
  TransactionSchemaResource,
} from "@/types";
import Pagination from "@/components/Pagination";
import { StatusBadge } from "@/components/StatusBadge";
import { HistoryPanel, historyPath } from "@/components/history";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardContent } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { API_BASE, reportApiError, unwrap, unwrapPage } from "@/lib/api";
import {
  Breakage,
  DraftMapping,
  MappingField,
  absolutePath,
  addAttributeTo,
  breakagesOf,
  describeBreakage,
  diffPartyMappings,
  hasErrors,
  isManyParty,
  isUndeclaredPath,
  memberSchemaProblem,
  newDraftMapping,
  partyContainerPaths,
  partyMemberPaths,
  parsePartyMappings,
  placeMappingViolations,
  removeAttributeFrom,
  removeDraftAt,
  sameMappings,
  toDrafts,
  toRequestMappings,
  updateAttributeAt,
  updateDraftAt,
  validateDrafts,
} from "@/lib/partyMapping";
import {
  ROLE_HINTS,
  isMappingConflict,
  readMappingUsages,
} from "@/lib/parties";
import { invalidatePartyMapping } from "@/lib/usePartyMapping";
import { SchemaFieldCombobox } from "@/components/SchemaFieldCombobox";
import { SchemaPath } from "@/lib/transactionSchema";
import { cn } from "@/lib/utils";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const SECTIONS = ["Mapping", "Versions", "History"] as const;
type Section = (typeof SECTIONS)[number];

const VERSIONS_PAGE_SIZE = 10;

const PROP_LABELS: Record<string, string> = {
  Role: "role",
  Path: "path",
  IdentifierPath: "identifier",
  DisplayNamePath: "display name",
  AttributePaths: "attributes",
};

const validationLabels = (drafts: DraftMapping[]): Record<string, string> => {
  const labels: Record<string, string> = { "Body.Mappings": "Mapping" };

  drafts.forEach((draft, index) => {
    const key = `Body.Mappings[${index}]`;
    labels[key] = draft.role;
    for (const [property, label] of Object.entries(PROP_LABELS)) {
      labels[`${key}.${property}`] = `${draft.role} ${label}`;
    }
  });

  return labels;
};

const landingRole = (
  mappings: PartyMappingEntry[] | undefined | null
): PartyRole => mappings?.[0]?.role ?? "Remitter";

const ROW_KEY = /^Body\.Mappings\[(\d+)\](?:\.(\w+))?/;

const FIELD_OF: Record<string, MappingField> = {
  Role: "role",
  Path: "path",
  IdentifierPath: "identifierPath",
  DisplayNamePath: "displayNamePath",
};

export function PartyMappingPanel({
  organizationSegment,
  canEdit = true,
  onUnauthorized,
}: {
  organizationSegment: string;
  canEdit?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const base = `${API_BASE}/organizations/${organizationSegment}/party-mapping`;

  const [section, setSection] = useState<Section>("Mapping");
  const [mapping, setMapping] = useState<PartyMapping | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [failed, setFailed] = useState<boolean>(false);
  const [drafts, setDrafts] = useState<DraftMapping[]>([]);
  const [saving, setSaving] = useState<boolean>(false);
  const [rowErrors, setRowErrors] = useState<
    Record<string, Partial<Record<MappingField, string>>>
  >({});
  const [attributeErrors, setAttributeErrors] = useState<
    Record<string, string>
  >({});
  const [formError, setFormError] = useState<string | null>(null);
  const [conflicted, setConflicted] = useState<boolean>(false);
  const [versionsKey, setVersionsKey] = useState<number>(0);
  const [withdrawing, setWithdrawing] = useState<boolean>(false);
  const [restoring, setRestoring] = useState<boolean>(false);
  const [confirmingWithdraw, setConfirmingWithdraw] = useState<boolean>(false);
  const [usages, setUsages] = useState<PartyMappingUsage[]>([]);
  const [refusedBy, setRefusedBy] = useState<PartyMappingUsage[] | null>(null);
  const [schemaFields, setSchemaFields] = useState<SchemaField[] | null>(null);

  const fetchMapping = useCallback(
    async ({ keepDrafts = false }: { keepDrafts?: boolean } = {}) => {
      setLoading(true);
      try {
        const response = await apiFetch(`${base}?includeDeleted=true`, {
          credentials: "include",
        });

        if (response.ok) {
          const resource = await unwrap<PartyMappingResource>(response);
          setMapping(resource.partyMapping);
          setFailed(false);
          if (!keepDrafts) {
            const declared = resource.partyMapping.isDeleted
              ? []
              : resource.partyMapping.mappings;
            setDrafts(toDrafts(declared));
            setSelectedRole(landingRole(declared));
          }
          return;
        }

        if (response.status === 404) {
          setMapping(null);
          setFailed(false);
          if (!keepDrafts) setDrafts([]);
          return;
        }

        setFailed(true);
        await reportApiError(response, {
          fallback: t("organization.failedLoadPartyMapping"),
          onUnauthorized,
          toastId: "error-party-mapping",
        });
      } catch {
        setFailed(true);
        toast.error(t("organization.failedLoadPartyMapping"), {
          id: "error-party-mapping",
        });
      } finally {
        setLoading(false);
      }
    },
    [base, onUnauthorized]
  );

  const fetchUsages = useCallback(async () => {
    try {
      const response = await apiFetch(`${base}/usages`, { credentials: "include" });
      if (!response.ok) {
        setUsages([]);
        return;
      }
      setUsages(await unwrap<PartyMappingUsage[]>(response));
    } catch {
      setUsages([]);
    }
  }, [base]);

  const fetchSchema = useCallback(async () => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationSegment}/transaction-schema`,
        { credentials: "include" }
      );
      if (!response.ok) {
        setSchemaFields(null);
        return;
      }
      const resource = await unwrap<TransactionSchemaResource>(response);
      setSchemaFields(resource.transactionSchema.fields);
    } catch {
      setSchemaFields(null);
    }
  }, [organizationSegment]);

  useEffect(() => {
    fetchMapping();
    fetchUsages();
    fetchSchema();
  }, [fetchMapping, fetchUsages, fetchSchema]);

  const resolving = mapping !== null && !mapping.isDeleted;
  const withdrawn = mapping !== null && mapping.isDeleted;
  const everExisted = mapping !== null;

  const requestMappings = useMemo(() => toRequestMappings(drafts), [drafts]);
  const dirty = !sameMappings(requestMappings, resolving ? mapping.mappings : []);

  const [selectedRole, setSelectedRole] = useState<PartyRole>("Remitter");

  const selected = drafts.find((draft) => draft.role === selectedRole) ?? null;

  const rolesWithErrors = useMemo(() => {
    const roles = new Set<PartyRole>();
    for (const draft of drafts) {
      const row = rowErrors[draft.id];
      if (row && Object.keys(row).length > 0) roles.add(draft.role);
      if (draft.attributes.some((a) => attributeErrors[a.id])) {
        roles.add(draft.role);
      }
    }
    return roles;
  }, [drafts, rowErrors, attributeErrors]);

  const breakages = useMemo(
    () =>
      breakagesOf(
        requestMappings,
        usages,
        resolving ? mapping.mappings : null
      ),
    [requestMappings, usages, resolving, mapping]
  );

  const editDrafts = (next: (prev: DraftMapping[]) => DraftMapping[]) => {
    setDrafts(next);
    setFormError(null);
    setRefusedBy(null);
  };

  const updateDraft = (id: string, updates: Partial<DraftMapping>) => {
    editDrafts((prev) => updateDraftAt(prev, id, updates));
    setRowErrors((prev) => {
      if (!prev[id]) return prev;
      const next = { ...prev };
      delete next[id];
      return next;
    });
  };

  const clearAttributeError = (attributeId: string) => {
    setAttributeErrors((prev) => {
      if (!prev[attributeId]) return prev;
      const next = { ...prev };
      delete next[attributeId];
      return next;
    });
  };

  const reset = () => {
    setDrafts(resolving ? toDrafts(mapping.mappings) : []);
    setSelectedRole(landingRole(resolving ? mapping.mappings : null));
    setRowErrors({});
    setAttributeErrors({});
    setFormError(null);
    setConflicted(false);
    setRefusedBy(null);
  };

  const save = async () => {
    const validation = validateDrafts(drafts);
    setRowErrors(validation.rowErrors);
    setAttributeErrors(validation.attributeErrors);
    setFormError(validation.formError);

    if (hasErrors(validation)) {
      toast.error(
        validation.formError ??
          t("organization.someRowsNeedFixingBefore")
      );
      return;
    }

    setSaving(true);
    setRefusedBy(null);

    try {
      const response = await apiFetch(base, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ mappings: requestMappings }),
        credentials: "include",
      });

      if (response.ok) {
        const resource = await unwrap<PartyMappingResource>(response);
        setMapping(resource.partyMapping);
        setDrafts(toDrafts(resource.partyMapping.mappings));
        setConflicted(false);
        setVersionsKey((key) => key + 1);
        invalidatePartyMapping();
        void fetchUsages();
        toast.success(
          t(
          "organization.partyMappingSavedAsVersion",
          resource.partyMapping.currentVersionNumber
        )
        );
        return;
      }

      if (response.status === 409) {
        const body = await response
          .clone()
          .json()
          .catch(() => null);

        const named = readMappingUsages(body);
        if (named) {
          setRefusedBy(named);
          void fetchUsages();
          toast.error(t("organization.changeWouldBreakRulesRead"), {
            description:
              t("organization.nothingSavedRulesWouldStrand"),
          });
          return;
        }

        setConflicted(true);
        await fetchMapping({ keepDrafts: true });
        toast.error(
          isMappingConflict(body)
            ? t("organization.mappingChangedYouWereEditing")
            : t("organization.mappingCouldNotSavedStands"),
          {
            description:
              t("organization.yourChangesHaveNotBeen"),
          }
        );
        return;
      }

      if (response.status === 400) {
        const body = await response
          .clone()
          .json()
          .catch(() => null);
        const errors = (body as { errors?: Record<string, string[]> } | null)
          ?.errors;
        if (errors) {
          const next: Record<
            string,
            Partial<Record<MappingField, string>>
          > = {};
          for (const [key, messages] of Object.entries(errors)) {
            const match = ROW_KEY.exec(key);
            if (!match) continue;
            const draft = drafts[Number(match[1])];
            if (!draft) continue;
            const field = FIELD_OF[match[2] ?? ""] ?? "identifierPath";
            next[draft.id] ??= {};
            next[draft.id][field] = messages.join(" ");
          }
          setRowErrors(next);
        }
      }

      await reportApiError(response, {
        fallback: t("organization.failedSavePartyMapping"),
        onUnauthorized,
        fieldLabels: validationLabels(drafts),
        onSchemaViolations: (report) => {
          if (report.kind !== "partyMapping") return;
          const placement = placeMappingViolations(
            drafts,
            report.violations,
            schemaFields
          );
          setRowErrors(placement.rowErrors);
          setAttributeErrors(placement.attributeErrors);
          setFormError(
            placement.unplaced.length > 0
              ? placement.unplaced.join(" · ")
              : null
          );
        },
      });
    } catch {
      toast.error(t("organization.failedSavePartyMapping"));
    } finally {
      setSaving(false);
    }
  };

  const reportStaleState = async (message: string) => {
    toast.error(message, {
      description: t("organization.reloadingCurrentStateMapping"),
    });
    await fetchMapping();
  };

  const withdraw = async () => {
    setWithdrawing(true);

    try {
      const response = await apiFetch(base, {
        method: "DELETE",
        credentials: "include",
      });

      if (response.ok) {
        setConfirmingWithdraw(false);
        setDrafts([]);
        setRowErrors({});
        setAttributeErrors({});
        setFormError(null);
        setConflicted(false);
        await fetchMapping();
        setVersionsKey((key) => key + 1);
        invalidatePartyMapping();
        toast.success(t("organization.organisationNoLongerResolvingParties"), {
          description:
            t("organization.partiesAlreadyResolvedAreKept"),
        });
        return;
      }

      if (response.status === 409) {
        const body = await response
          .clone()
          .json()
          .catch(() => null);
        const named = readMappingUsages(body);
        if (named) {
          setRefusedBy(named);
          setConfirmingWithdraw(false);
          setSection("Mapping");
          void fetchUsages();
          toast.error(t("organization.rulesStillReadParty"), {
            description:
              t("organization.mappingUnchangedChangeDisableRules"),
          });
          return;
        }
      }

      if (response.status === 404) {
        setConfirmingWithdraw(false);
        await reportStaleState("There is no mapping in force to withdraw");
        return;
      }

      await reportApiError(response, {
        fallback: t("organization.failedWithdrawPartyMapping"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("organization.failedWithdrawPartyMapping"));
    } finally {
      setWithdrawing(false);
    }
  };

  const restore = async () => {
    setRestoring(true);

    try {
      const response = await apiFetch(`${base}/recover`, {
        method: "POST",
        credentials: "include",
      });

      if (response.ok) {
        await fetchMapping();
        setVersionsKey((key) => key + 1);
        invalidatePartyMapping();
        void fetchUsages();
        toast.success(t("organization.partyMappingRestored"), {
          description:
            t("organization.partiesAreResolvedAgainFrom"),
        });
        return;
      }

      if (response.status === 400) {
        await reportStaleState("This mapping is already in force");
        return;
      }

      if (response.status === 404) {
        await reportStaleState("There is no withdrawn mapping to restore");
        return;
      }

      await reportApiError(response, {
        fallback: t("organization.failedRestorePartyMapping"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("organization.failedRestorePartyMapping"));
    } finally {
      setRestoring(false);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-1.5">
        {SECTIONS.filter(
          (candidate) => candidate !== "History" || everExisted
        ).map((candidate) => (
          <Button
            key={candidate}
            type="button"
            size="sm"
            variant={section === candidate ? "default" : "outline"}
            onClick={() => setSection(candidate)}
          >
            {candidate === "Versions" ? "Version history" : candidate}
          </Button>
        ))}
        {!loading && !failed && (
          <div className="ml-auto flex items-center gap-2">
            <StatusBadge
              status={resolving ? "Enabled" : "Disabled"}
              label={resolving ? t("organization.resolvingParties") : t("organization.notResolving")}
            />
            {mapping && (
              <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {resolving
                  ? `Current version v${mapping.currentVersionNumber}`
                  : `Last version v${mapping.currentVersionNumber}`}
              </span>
            )}
          </div>
        )}
      </div>

      {section === "Mapping" && (
        <Card>
          <CardContent className="space-y-4 pt-6">
            <div>
              <h2 className="text-sm font-semibold text-slate-900">
                {t("organization.partyMapping")}
              </h2>
              <p className="mt-0.5 text-sm text-slate-500">
                Where the counterparties sit in your payloads. Declare it and the
                engine resolves who is paying whom on every evaluate, gives each
                a lasting identity, and lets a rule ask something the payload in
                hand cannot answer — whether this beneficiary is new, or has been
                assessed before.
              </p>
            </div>

            {usages.length > 0 && (
              <UsageBanner usages={usages} />
            )}

            {loading ? (
              <div className="space-y-2">
                {Array.from({ length: 2 }).map((_, index) => (
                  <Skeleton key={index} className="h-24 w-full" />
                ))}
              </div>
            ) : failed ? (
              <div className="rounded-md border border-dashed border-slate-200 py-12 text-center">
                <p className="text-sm text-slate-500">
                  {t("organization.thePartyMappingCouldNot")}
                </p>
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-3"
                  onClick={() => fetchMapping()}
                >
                  {t("common.retry")}
                </Button>
              </div>
            ) : (
              <>
                {!resolving && (
                  <NotResolvingNotice
                    withdrawn={withdrawn ? mapping : null}
                    canEdit={canEdit}
                    restoring={restoring}
                    onRestore={restore}
                  />
                )}

                {conflicted && (
                  <div className="rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs text-attention-700">
                    Someone else saved this mapping while you had it open. What
                    you see below is still your version — the stored one is now
                    v{mapping?.currentVersionNumber}. Saving again replaces it.
                  </div>
                )}

                {refusedBy && <RefusalNotice usages={refusedBy} />}

                {breakages.length > 0 && !refusedBy && (
                  <BreakageNotice breakages={breakages} />
                )}

                <RoleSelector
                  value={selectedRole}
                  onChange={setSelectedRole}
                  configured={drafts.map((draft) => draft.role)}
                  withErrors={rolesWithErrors}
                  withBreakages={new Set(breakages.map((b) => b.role))}
                />

                {selected ? (
                  <RoleForm
                    draft={selected}
                    schemaFields={schemaFields}
                    readOnly={!canEdit || saving}
                    errors={rowErrors[selected.id]}
                    attributeErrors={attributeErrors}
                    onChange={updateDraft}
                    onStopCollecting={(id) =>
                      editDrafts((prev) => removeDraftAt(prev, id))
                    }
                    onAddAttribute={(id) =>
                      editDrafts((prev) => addAttributeTo(prev, id))
                    }
                    onChangeAttribute={(mappingId, attributeId, updates) => {
                      editDrafts((prev) =>
                        updateAttributeAt(prev, mappingId, attributeId, updates)
                      );
                      clearAttributeError(attributeId);
                    }}
                    onRemoveAttribute={(mappingId, attributeId) => {
                      editDrafts((prev) =>
                        removeAttributeFrom(prev, mappingId, attributeId)
                      );
                      clearAttributeError(attributeId);
                    }}
                  />
                ) : (
                  <RoleNotCollected
                    role={selectedRole}
                    canEdit={canEdit && !saving}
                    onCollect={() =>
                      editDrafts((prev) => [
                        ...prev,
                        newDraftMapping(selectedRole),
                      ])
                    }
                  />
                )}

                {formError && (
                  <p className="text-xs text-critical-700">{formError}</p>
                )}

                {canEdit && (
                  <div className="flex flex-wrap items-center gap-2 border-t border-slate-100 pt-4">
                    <Button size="sm" onClick={save} disabled={saving || !dirty}>
                      {saving
                        ? "Saving..."
                        : `Save mapping (${drafts.length} of ${PARTY_ROLES.length} ${
                            drafts.length === 1 ? "role" : "roles"
                          })`}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={reset}
                      disabled={saving || !dirty}
                    >
                      {t("common.reset")}
                    </Button>
                    {resolving && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="ml-auto text-critical-500 hover:text-critical-700"
                        disabled={saving || withdrawing}
                        onClick={() => setConfirmingWithdraw(true)}
                      >
                        {t("organization.stopResolving")}
                      </Button>
                    )}
                  </div>
                )}

                <p className="text-xs text-slate-400">
                  Saving replaces the mapping outright and snapshots a new
                  version. It changes what is resolved from the next evaluate on
                  — the parties already recorded are what the payments said, and
                  are never rewritten by a later mapping.
                </p>
              </>
            )}
          </CardContent>
        </Card>
      )}

      {section === "Versions" && (
        <VersionsSection
          base={base}
          refreshKey={versionsKey}
          currentVersionNumber={mapping?.currentVersionNumber ?? null}
          onUnauthorized={onUnauthorized}
        />
      )}

      {section === "History" && everExisted && (
        <PartyMappingHistory
          organizationSegment={organizationSegment}
          onUnauthorized={onUnauthorized}
        />
      )}

      <WithdrawDialog
        open={confirmingWithdraw}
        withdrawing={withdrawing}
        roleCount={mapping?.mappings.length ?? 0}
        usages={usages}
        discardsEdits={dirty}
        onOpenChange={setConfirmingWithdraw}
        onConfirm={withdraw}
      />
    </div>
  );
}

function UsageLink({ usage }: { usage: PartyMappingUsage }) {
  return (
    <Link
      href={`/rules/${usage.ruleId}`}
      className="inline-flex items-baseline gap-1.5 underline underline-offset-2 hover:no-underline"
    >
      <span className="font-medium">{usage.friendlyName}</span>
      <span className="font-mono text-[11px] uppercase tracking-wide opacity-70">
        {usage.roles.join(", ")}
      </span>
    </Link>
  );
}

function UsageBanner({ usages }: { usages: PartyMappingUsage[] }) {
  const t = useTranslation();
  const [open, setOpen] = useState(false);
  const enabled = usages.filter((usage) => usage.isEnabled !== false).length;

  return (
    <div className="rounded-lg border border-brand-200 bg-brand-50 px-3 py-2">
      <div className="flex flex-wrap items-center gap-2">
        <Users className="size-4 shrink-0 text-brand-800" />
        <p className="text-sm text-brand-900">
          <span className="font-medium">
            {usages.length} {usages.length === 1 ? "rule reads" : "rules read"} a
            party
          </span>
          {enabled !== usages.length && (
            <span className="text-brand-800">
              {" "}
              ({enabled} of them enabled)
            </span>
          )}
          . Dropping a role one of them names, or turning it into a list, is
          refused.
        </p>
        <Button
          variant="ghost"
          size="sm"
          className="ml-auto h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
          onClick={() => setOpen((current) => !current)}
        >
          {open ? "Hide" : "Show them"}
        </Button>
      </div>

      {open && (
        <ul className="mt-2 space-y-1 border-t border-brand-200 pt-2 text-sm text-brand-900">
          {usages.map((usage) => (
            <li key={usage.ruleId} className="flex flex-wrap items-baseline gap-2">
              <UsageLink usage={usage} />
              {usage.isEnabled === false && (
                <StatusBadge status="Disabled" label={t("common.disabled")} />
              )}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function BreakageNotice({ breakages }: { breakages: Breakage[] }) {
  const t = useTranslation();
  return (
    <div className="flex items-start gap-2 rounded-lg border border-attention-200 bg-attention-50 px-3 py-2">
      <AlertTriangle className="mt-0.5 size-4 shrink-0 text-attention-700" />
      <div className="space-y-1 text-sm text-attention-700">
        <p className="font-medium">
          {t("organization.thisSaveWillBeRefused")}
        </p>
        <ul className="space-y-0.5 text-xs">
          {breakages.map((breakage, index) => (
            <li key={`${breakage.usage.ruleId}-${breakage.role}-${index}`}>
              <UsageLink usage={breakage.usage} /> {describeBreakage(breakage)}.
            </li>
          ))}
        </ul>
        <p className="text-xs">
          A rule is refused if the mapping cannot support it, so the mapping
          cannot be edited into a state that would not have supported one. Change
          or disable the rules first.
        </p>
      </div>
    </div>
  );
}

function RefusalNotice({ usages }: { usages: PartyMappingUsage[] }) {
  const t = useTranslation();
  return (
    <div className="flex items-start gap-2 rounded-lg border border-critical-200 bg-critical-50 px-3 py-2">
      <AlertTriangle className="mt-0.5 size-4 shrink-0 text-critical-700" />
      <div className="space-y-1 text-sm text-critical-700">
        <p className="font-medium">{t("organization.nothingWasSaved")}</p>
        <p className="text-xs">
          {t("organization.theseRulesReadAParty")}
        </p>
        <ul className="space-y-0.5 text-xs">
          {usages.map((usage) => (
            <li key={usage.ruleId}>
              <UsageLink usage={usage} />
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

function WithdrawDialog({
  open,
  withdrawing,
  roleCount,
  usages,
  discardsEdits,
  onOpenChange,
  onConfirm,
}: {
  open: boolean;
  withdrawing: boolean;
  roleCount: number;
  usages: PartyMappingUsage[];
  discardsEdits: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: () => void;
}) {
  const t = useTranslation();
  const blocked = usages.length > 0;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t("organization.stopResolvingParties")}</DialogTitle>
        </DialogHeader>

        <div className="space-y-3 text-sm text-slate-600">
          {blocked && (
            <div className="rounded-md border border-critical-200 bg-critical-50 px-3 py-2 text-xs text-critical-700">
              <p className="font-medium">
                This will be refused: {usages.length}{" "}
                {usages.length === 1 ? "rule reads" : "rules read"} a party.
              </p>
              <ul className="mt-1 space-y-0.5">
                {usages.map((usage) => (
                  <li key={usage.ruleId}>
                    <UsageLink usage={usage} />
                  </li>
                ))}
              </ul>
              <p className="mt-1">
                Change or disable them first. A rule left reading a party nothing
                resolves would not fail loudly — it would simply stop matching.
              </p>
            </div>
          )}

          <p>
            New evaluations stop resolving parties. Payloads are accepted exactly
            as they are now — nothing about them is rejected for it — and the
            transactions that follow simply carry nobody.
          </p>
          <ul className="space-y-2">
            <li className="flex gap-2">
              <span aria-hidden className="text-settled-500">
                ✓
              </span>
              <span>
                <span className="font-medium text-slate-900">
                  {t("organization.thePartiesAlreadyResolvedAre")}
                </span>{" "}
                Every party, every link to a transaction, and every risk
                assessment stays exactly as it is. They are what the payments
                said, and withdrawing does not unsay them.
              </span>
            </li>
            <li className="flex gap-2">
              <span aria-hidden className="text-settled-500">
                ✓
              </span>
              <span>
                <span className="font-medium text-slate-900">
                  {t("organization.theDeclarationIsKept")}
                </span>{" "}
                All {roleCount} {roleCount === 1 ? "role" : "roles"} stay stored,
                and restoring puts them back exactly as they are without creating
                a new version.
              </span>
            </li>
            <li className="flex gap-2">
              <span aria-hidden className="text-settled-500">
                ✓
              </span>
              <span>
                <span className="font-medium text-slate-900">
                  {t("organization.theHistoryStaysReadable")}
                </span>{" "}
                Version snapshots and the change trail remain, and this
                withdrawal is recorded in them.
              </span>
            </li>
          </ul>
          {discardsEdits && (
            <p className="rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs text-attention-700">
              The unsaved changes in the form will be discarded — what is kept
              and restorable is the mapping as last saved.
            </p>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={withdrawing}
          >
            {t("common.cancel")}
          </Button>
          <Button onClick={onConfirm} disabled={withdrawing}>
            {withdrawing ? "Stopping..." : "Stop resolving"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function NotResolvingNotice({
  canEdit,
  withdrawn,
  restoring,
  onRestore,
}: {
  canEdit: boolean;
  withdrawn: PartyMapping | null;
  restoring: boolean;
  onRestore: () => void;
}) {
  const [showMapping, setShowMapping] = useState<boolean>(false);

  return (
    <div className="rounded-lg border border-dashed border-slate-300 bg-slate-50/60 px-4 py-3">
      <div>
        <p className="text-sm font-medium text-slate-600">
          {withdrawn
            ? "Parties are not being resolved"
            : "No party mapping yet"}
        </p>
        <p className="mt-1 max-w-2xl text-xs text-slate-500">
          {withdrawn ? (
            <>
              It was withdrawn
              {withdrawn.deletedAt && (
                <> on <LocalTime value={withdrawn.deletedAt} /></>
              )}
              , so evaluations since then have recorded no parties. Everyone
              resolved before that is still there, still linked to their
              transactions, and still carrying whatever an analyst assessed them
              at. The declaration from v{withdrawn.currentVersionNumber} is kept,
              and restoring puts it back exactly as it was.
            </>
          ) : (
            <>
              Nobody is being resolved, and that is not an error — transactions
              carry a payload and nothing about who is behind it. Pick a role
              below and say where your payloads name it, and each one resolved
              gets a lasting identity: a history you can read, a risk assessment
              you can record, and facts a rule can ask about that no single
              payload could answer.
            </>
          )}
        </p>

        {canEdit && withdrawn && (
          <div className="mt-3 flex flex-wrap items-center gap-2">
            <Button
              size="sm"
              className="gap-1.5"
              disabled={restoring}
              onClick={onRestore}
            >
              <RotateCcw className="size-4" />
              {restoring ? "Restoring..." : "Restore previous mapping"}
            </Button>
            <span className="text-xs text-slate-400">
              — or collect a role below to declare a new one, which mints
              v{withdrawn.currentVersionNumber + 1} rather than putting this one
              back.
            </span>
          </div>
        )}

        {withdrawn && (
          <Button
            variant="ghost"
            size="sm"
            className="mt-2 h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
            onClick={() => setShowMapping((open) => !open)}
          >
            {showMapping
              ? "Hide the withdrawn mapping"
              : `Show the ${withdrawn.mappings.length} withdrawn ${
                  withdrawn.mappings.length === 1 ? "role" : "roles"
                }`}
          </Button>
        )}
      </div>

      {withdrawn && showMapping && (
        <div className="mt-4 text-left">
          <PartyMappingTable mappings={withdrawn.mappings} />
        </div>
      )}
    </div>
  );
}

function RoleSelector({
  value,
  onChange,
  configured,
  withErrors,
  withBreakages,
}: {
  value: PartyRole;
  onChange: (role: PartyRole) => void;
  configured: PartyRole[];
  withErrors: Set<PartyRole>;
  withBreakages: Set<PartyRole>;
}) {
  const t = useTranslation();
  return (
    <div className="flex flex-wrap items-center gap-2 rounded-lg border border-slate-200 bg-slate-50/60 p-2">
      <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {t("organization.configuring")}
      </span>
      <Select value={value} onValueChange={(v) => onChange(v as PartyRole)}>
        <SelectTrigger className="w-48">
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {/* The role and nothing else. Keep it that way: Radix mirrors the
              selected option's text into the closed trigger, so anything added
              inside an option is also painted into the control. Which roles are
              collected is said in full below, and the ones needing a person are
              chipped beside the control. */}
          {PARTY_ROLES.map((role) => (
            <SelectItem key={role} value={role}>
              {role}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {[...withErrors].map((role) => (
        <span
          key={`error-${role}`}
          className="rounded-md border border-critical-200 bg-critical-50 px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-wide text-critical-700"
        >
          {role} needs fixing
        </span>
      ))}
      {[...withBreakages]
        .filter((role) => !withErrors.has(role))
        .map((role) => (
          <span
            key={`breaks-${role}`}
            className="rounded-md border border-attention-200 bg-attention-50 px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-wide text-attention-700"
          >
            {role} breaks a rule
          </span>
        ))}

      <p className="w-full text-xs text-slate-500">
        {configured.length === 0
          ? "Nothing is collected yet."
          : `Saving writes all ${configured.length} collected ${
              configured.length === 1 ? "role" : "roles"
            } — ${configured.join(", ")} — not just this one.`}
      </p>
    </div>
  );
}

function RoleNotCollected({
  role,
  canEdit,
  onCollect,
}: {
  role: PartyRole;
  canEdit: boolean;
  onCollect: () => void;
}) {
  return (
    <div className="rounded-lg border border-dashed border-slate-300 bg-slate-50/60 px-6 py-10 text-center">
      <p className="text-sm font-medium text-slate-600">
        {role} is not collected
      </p>
      <p className="mx-auto mt-2 max-w-lg text-xs text-slate-500">
        {ROLE_HINTS[role]} Nothing is resolved in this position, and no
        transaction records one — which is the right answer where your payloads
        do not carry it.
      </p>
      {canEdit && (
        <Button size="sm" className="mt-4 gap-1.5" onClick={onCollect}>
          <Plus className="size-4" />
          Collect {role}
        </Button>
      )}
    </div>
  );
}

function RoleForm({
  draft,
  schemaFields,
  readOnly,
  errors,
  attributeErrors,
  onChange,
  onStopCollecting,
  onAddAttribute,
  onChangeAttribute,
  onRemoveAttribute,
}: {
  draft: DraftMapping;
  schemaFields: SchemaField[] | null;
  readOnly: boolean;
  errors: Partial<Record<MappingField, string>> | undefined;
  attributeErrors: Record<string, string>;
  onChange: (id: string, updates: Partial<DraftMapping>) => void;
  onStopCollecting: (id: string) => void;
  onAddAttribute: (id: string) => void;
  onChangeAttribute: (
    mappingId: string,
    attributeId: string,
    updates: { name?: string; path?: string }
  ) => void;
  onRemoveAttribute: (mappingId: string, attributeId: string) => void;
}) {
  const t = useTranslation();
  const many = draft.path.includes("[*]");
  const anyError = errors && Object.keys(errors).length > 0;

  const preview = (relative: string): string | null => {
    const joined = absolutePath({ path: draft.path }, relative);
    return joined && joined !== relative.trim() ? joined : null;
  };

  const containers = useMemo(
    () => partyContainerPaths(schemaFields),
    [schemaFields]
  );

  const members = useMemo(
    () => partyMemberPaths(schemaFields, draft.path),
    [schemaFields, draft.path]
  );

  const problemWith = (
    relative: string,
    purpose: "identify" | "name" | "describe"
  ): string | null =>
    memberSchemaProblem(
      schemaFields,
      absolutePath({ path: draft.path }, relative),
      purpose
    );

  const pathUndeclared = isUndeclaredPath(schemaFields, draft.path) === true;

  return (
    <div
      className={cn(
        "rounded-lg border bg-card p-3",
        anyError ? "border-critical-200 ring-1 ring-critical-200" : "border-slate-200"
      )}
    >
      <div className="flex flex-wrap items-center gap-2">
        <h3 className="font-mono text-sm font-medium text-slate-900">
          {draft.role}
        </h3>

        {many && (
          <span className="rounded bg-brand-100 px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-wide text-brand-900">
            many per payload
          </span>
        )}

        {!readOnly && (
          <Button
            variant="ghost"
            size="sm"
            className="ml-auto gap-1.5 text-critical-500 hover:text-critical-700"
            onClick={() => onStopCollecting(draft.id)}
            title={`Stop collecting the ${draft.role.toLowerCase()}`}
          >
            <Trash2 className="size-4" />
            {t("organization.stopCollecting")}
          </Button>
        )}
      </div>

      <p className="mt-1.5 text-xs text-slate-400">{ROLE_HINTS[draft.role]}</p>
      {errors?.role && (
        <p className="mt-1 text-xs text-critical-700">{errors.role}</p>
      )}

      <div className="mt-3 space-y-1.5">
        <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
          {t("organization.whereDoesThePartySit")}
        </label>
        <SchemaFieldCombobox
          value={draft.path}
          onChange={(path) => onChange(draft.id, { path })}
          disabled={readOnly}
          fields={schemaFields}
          paths={containers}
          placeholder={t("organization.leaveEmptyForThePayload")}
          tone={
            errors?.path ? "critical" : pathUndeclared ? "warning" : "default"
          }
          className="w-full max-w-md"
        />
        {errors?.path ? (
          <p className="text-xs text-critical-700">{errors.path}</p>
        ) : pathUndeclared ? (
          <p className="text-xs text-attention-700">
            The transaction schema declares nothing at{" "}
            <span className="font-mono">{draft.path.trim()}</span>, so nothing
            inside it resolves either and this save will be refused. The API
            never objects to this box directly — it objects once per path read
            within the party, each naming this prefix — so fixing it here clears
            all of them.
          </p>
        ) : (
          <p className="text-xs text-slate-400">
            The object holding this party —{" "}
            <span className="font-mono text-slate-500">sender</span>, or{" "}
            <span className="font-mono text-slate-500">parties[*]</span> where
            the payload carries a list of them. This is the only place a{" "}
            <span className="font-mono text-slate-500">[*]</span> may go. Leave
            it empty for a flat payload that names the party&apos;s fields at the
            top level.
            {containers.length > 0 && (
              <>
                {" "}
                {containers.length}{" "}
                {containers.length === 1 ? "object is" : "objects are"} declared
                on your transaction schema — start typing to pick one.
              </>
            )}
          </p>
        )}
      </div>

      <div className="mt-3 space-y-3 border-l-2 border-slate-100 pl-3">
        <span className="block font-mono text-xs uppercase tracking-widest text-slate-400">
          {draft.path.trim()
            ? "Read inside each party found there"
            : "Read from the payload"}
        </span>

        <RelativeField
          label={t("organization.whatIdentifiesIt")}
          required
          value={draft.identifierPath}
          placeholder={t("organization.accountid")}
          error={errors?.identifierPath}
          schemaProblem={problemWith(draft.identifierPath, "identify")}
          preview={preview(draft.identifierPath)}
          hint={t("organization.identityItselfSameValueLater")}
          readOnly={readOnly}
          schemaFields={schemaFields}
          options={members}
          onChange={(identifierPath) => onChange(draft.id, { identifierPath })}
        />

        <RelativeField
          label={t("organization.whatNamesIt")}
          value={draft.displayNamePath}
          placeholder={t("organization.name")}
          error={errors?.displayNamePath}
          schemaProblem={problemWith(draft.displayNamePath, "name")}
          preview={preview(draft.displayNamePath)}
          hint={t("organization.personReadOptionalPartyNo")}
          readOnly={readOnly}
          schemaFields={schemaFields}
          options={members}
          onChange={(displayNamePath) => onChange(draft.id, { displayNamePath })}
        />

        <div className="space-y-1.5">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("organization.whatDescribesIt")}
            </span>
            {!readOnly && (
              <Button
                variant="outline"
                size="sm"
                className="h-7 gap-1 text-xs"
                onClick={() => onAddAttribute(draft.id)}
              >
                <Plus className="size-3" />
                {t("organization.addAttribute")}
              </Button>
            )}
          </div>

          {draft.attributes.length === 0 ? (
            <p className="text-xs text-slate-400">
              None. Attributes are observed values kept beside the party for
              whoever reads it — a country, a bank, a device. Rules do not
              address them: everything the payload carries is already addressable
              as an ordinary field, and a rule reading a stored copy would be
              reading whatever the last payment happened to say.
            </p>
          ) : (
            <div className="space-y-1.5">
              {draft.attributes.map((attribute) => {
                const error = attributeErrors[attribute.id];
                return (
                  <div key={attribute.id} className="space-y-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <Input
                        placeholder={t("common.name")}
                        value={attribute.name}
                        disabled={readOnly}
                        onChange={(event) =>
                          onChangeAttribute(draft.id, attribute.id, {
                            name: event.target.value,
                          })
                        }
                        className={cn(
                          "w-40 font-mono",
                          error && "border-critical-500"
                        )}
                      />
                      <span
                        aria-hidden
                        className="font-mono text-xs text-slate-300"
                      >
                        ←
                      </span>
                      <SchemaFieldCombobox
                        value={attribute.path}
                        onChange={(path) =>
                          onChangeAttribute(draft.id, attribute.id, { path })
                        }
                        disabled={readOnly}
                        fields={schemaFields}
                        paths={members}
                        placeholder={t("organization.country")}
                        tone={
                          error
                            ? "critical"
                            : problemWith(attribute.path, "describe")
                              ? "warning"
                              : "default"
                        }
                        className="w-56"
                      />
                      {preview(attribute.path) && (
                        <span className="font-mono text-[11px] text-slate-400">
                          {preview(attribute.path)}
                        </span>
                      )}
                      {!readOnly && (
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-8 w-8 text-critical-500 hover:text-critical-700"
                          onClick={() =>
                            onRemoveAttribute(draft.id, attribute.id)
                          }
                          title={t("organization.removeAttribute")}
                        >
                          <Trash2 className="size-4" />
                        </Button>
                      )}
                    </div>
                    {error && (
                      <p className="text-xs text-critical-700">{error}</p>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function RelativeField({
  label,
  required = false,
  value,
  placeholder,
  error,
  schemaProblem = null,
  preview,
  hint,
  readOnly,
  schemaFields,
  options,
  onChange,
}: {
  label: string;
  required?: boolean;
  value: string;
  placeholder: string;
  error?: string;
  schemaProblem?: string | null;
  preview: string | null;
  hint: string;
  readOnly: boolean;
  schemaFields: SchemaField[] | null;
  options: SchemaPath[];
  onChange: (value: string) => void;
}) {
  return (
    <div className="space-y-1.5">
      <label className="flex items-baseline gap-2 font-mono text-xs uppercase tracking-widest text-slate-400">
        {label}
        {required && (
          <span className="text-attention-700 normal-case tracking-normal">
            required
          </span>
        )}
      </label>
      <div className="flex flex-wrap items-center gap-2">
        <SchemaFieldCombobox
          value={value}
          onChange={onChange}
          disabled={readOnly}
          fields={schemaFields}
          paths={options}
          placeholder={placeholder}
          tone={error ? "critical" : schemaProblem ? "warning" : "default"}
          className="w-56"
        />
        {preview && (
          <span className="font-mono text-[11px] text-slate-400">
            {preview}
          </span>
        )}
      </div>
      {error ? (
        <p className="text-xs text-critical-700">{error}</p>
      ) : schemaProblem ? (
        <p className="text-xs text-attention-700">
          <span className="font-mono">{preview ?? value.trim()}</span>:{" "}
          {schemaProblem}
        </p>
      ) : (
        <p className="text-xs text-slate-400">{hint}</p>
      )}
    </div>
  );
}

export function PartyMappingTable({
  mappings,
  emptyLabel,
}: {
  mappings: PartyMappingEntry[];
  emptyLabel?: string;
}) {
  const t = useTranslation();
  if (mappings.length === 0) {
    return (
      <p className="text-sm text-slate-400">
        {emptyLabel ?? t("partyMapping.noRolesDeclared")}
      </p>
    );
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-card">
      <Table>
        <TableHeader>
          <TableRow className="bg-slate-50 hover:bg-slate-50">
            <TableHead className="w-32">{t("organization.role")}</TableHead>
            <TableHead>{t("organization.partySitsAt")}</TableHead>
            <TableHead>{t("organization.identifiedBy")}</TableHead>
            <TableHead>{t("organization.namedBy")}</TableHead>
            <TableHead>{t("organization.attributes")}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {mappings.map((entry) => (
            <TableRow key={entry.role} className="hover:bg-slate-50/50">
              <TableCell className="font-mono text-sm text-slate-900">
                {entry.role}
                {isManyParty(entry) && (
                  <span className="mt-0.5 block text-[11px] font-sans text-brand-800">
                    many per payload
                  </span>
                )}
              </TableCell>
              <TableCell className="font-mono text-xs text-slate-600">
                {entry.path?.trim() || (
                  <span className="font-sans text-slate-400">
                    the payload itself
                  </span>
                )}
              </TableCell>
              <TableCell className="font-mono text-xs text-slate-900">
                {entry.identifierPath}
                <span className="mt-0.5 block text-[11px] text-slate-400">
                  {absolutePath(entry, entry.identifierPath)}
                </span>
              </TableCell>
              <TableCell className="font-mono text-xs text-slate-600">
                {entry.displayNamePath?.trim() || (
                  <span className="font-sans text-slate-400">—</span>
                )}
              </TableCell>
              <TableCell className="font-mono text-xs text-slate-600">
                {entry.attributePaths?.length ? (
                  <ul className="space-y-0.5">
                    {entry.attributePaths.map((attribute) => (
                      <li key={attribute.name}>
                        {attribute.name}{" "}
                        <span className="text-slate-400">
                          ← {attribute.path}
                        </span>
                      </li>
                    ))}
                  </ul>
                ) : (
                  <span className="font-sans text-slate-400">—</span>
                )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

function VersionsSection({
  base,
  refreshKey,
  currentVersionNumber,
  onUnauthorized,
}: {
  base: string;
  refreshKey: number;
  currentVersionNumber: number | null;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [versions, setVersions] = useState<PartyMappingVersion[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [pageNumber, setPageNumber] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [expanded, setExpanded] = useState<number | null>(null);

  useEffect(() => {
    let cancelled = false;

    const fetchVersions = async () => {
      setLoading(true);
      try {
        const params = new URLSearchParams({
          pageNumber: String(pageNumber),
          pageSize: String(VERSIONS_PAGE_SIZE),
        });
        const response = await apiFetch(`${base}/versions?${params}`, {
          credentials: "include",
        });

        if (cancelled) return;

        if (response.ok) {
          const page = await unwrapPage<PartyMappingVersionResource>(response);
          setVersions(page.data.map((r) => r.partyMappingVersion));
          setTotalPages(page.metadata.pageCount || 1);
          return;
        }

        if (response.status === 404) {
          setVersions([]);
          setTotalPages(1);
          return;
        }

        await reportApiError(response, {
          fallback: t("organization.failedLoadMappingSVersions"),
          onUnauthorized,
          toastId: "error-party-mapping-versions",
        });
      } catch {
        if (!cancelled) {
          toast.error(t("organization.failedLoadMappingSVersions"), {
            id: "error-party-mapping-versions",
          });
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    fetchVersions();
    return () => {
      cancelled = true;
    };
  }, [base, pageNumber, refreshKey, onUnauthorized]);

  return (
    <Card>
      <CardContent className="space-y-4 pt-6">
        <div>
          <h2 className="text-sm font-semibold text-slate-900">
            {t("organization.versionHistory")}
          </h2>
          <p className="mt-0.5 text-sm text-slate-500">
            Every save snapshots the whole declaration. Open one to read the
            mapping as it stood then — which is what a party resolved on that
            day was resolved by.
          </p>
        </div>

        {loading ? (
          <div className="space-y-2">
            {Array.from({ length: 3 }).map((_, index) => (
              <Skeleton key={index} className="h-10 w-full" />
            ))}
          </div>
        ) : versions.length === 0 ? (
          <p className="py-10 text-center font-mono text-xs uppercase tracking-widest text-slate-400">
            {t("organization.noVersionsYet")}
          </p>
        ) : (
          <div className="space-y-2">
            {versions.map((version) => {
              const open = expanded === version.versionNumber;
              return (
                <div
                  key={version.partyMappingVersionId}
                  className="rounded-lg border border-slate-200 bg-card"
                >
                  <div className="flex flex-wrap items-center gap-3 px-3 py-2">
                    <span className="font-mono text-sm font-medium text-slate-900">
                      v{version.versionNumber}
                    </span>
                    {version.versionNumber === currentVersionNumber && (
                      <StatusBadge status="Live" label={t("organization.current")} />
                    )}
                    <span className="text-sm text-slate-600">
                      <LocalTime value={version.createdAt} />
                    </span>
                    <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
                      {version.mappings.length}{" "}
                      {version.mappings.length === 1 ? "role" : "roles"}
                    </span>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="ml-auto h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
                      onClick={() =>
                        setExpanded(open ? null : version.versionNumber)
                      }
                    >
                      {open ? "Hide" : "Show"}
                    </Button>
                  </div>
                  {open && (
                    <div className="border-t border-slate-100 p-3">
                      <PartyMappingTable mappings={version.mappings} />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        )}

        {versions.length > 0 && totalPages > 1 && (
          <Pagination
            handlePrevPage={() => setPageNumber((p) => Math.max(1, p - 1))}
            pageNumber={pageNumber}
            totalPages={totalPages}
            handleNextPage={() =>
              setPageNumber((p) => Math.min(totalPages, p + 1))
            }
          />
        )}
      </CardContent>
    </Card>
  );
}

const MAPPINGS_AUDIT_FIELD = "Mappings";

const CHANGE_STYLES: Record<string, string> = {
  added: "border-settled-200 bg-settled-50 text-settled-700",
  removed: "border-critical-200 bg-critical-50 text-critical-700",
  changed: "border-attention-200 bg-attention-50 text-attention-700",
  unchanged: "border-slate-200 bg-slate-50 text-slate-500",
};

function PartyMappingsChange({
  change,
  isInitialSnapshot,
}: {
  change: AuditLogChange;
  isInitialSnapshot: boolean;
}) {
  const t = useTranslation();
  const before = isInitialSnapshot ? null : parsePartyMappings(change.oldValue);
  const after = parsePartyMappings(change.newValue);

  if (after === null && before === null) {
    return <p className="text-sm text-slate-400">{t("organization.unreadablePartyMapping")}</p>;
  }

  const rows = diffPartyMappings(before, after).filter(
    (row) => isInitialSnapshot || row.change !== "unchanged"
  );

  if (rows.length === 0) {
    return (
      <p className="text-sm text-slate-400">
        {t("organization.theMappingWasRewrittenWithout")}
      </p>
    );
  }

  return (
    <ul className="space-y-2">
      {rows.map((row) => (
        <li key={row.role} className="space-y-1">
          <div className="flex flex-wrap items-baseline gap-2">
            <span
              className={cn(
                "rounded-md border px-1.5 py-0.5 font-mono text-[11px] uppercase tracking-wide",
                CHANGE_STYLES[row.change]
              )}
            >
              {isInitialSnapshot ? "role" : row.change}
            </span>
            <span className="font-mono text-sm text-slate-900">{row.role}</span>
          </div>
          {row.before && (
            <p className="text-xs text-slate-500">
              <span className="text-slate-400">was</span>{" "}
              <span className="font-mono">
                {describeEntry(row.before)}
              </span>
            </p>
          )}
          {row.after && (
            <p className="text-xs text-slate-500">
              <span className="text-slate-400">
                {row.before ? "now" : ""}
              </span>{" "}
              <span className="font-mono">{describeEntry(row.after)}</span>
            </p>
          )}
        </li>
      ))}
    </ul>
  );
}

const describeEntry = (entry: PartyMappingEntry): string => {
  const where = entry.path?.trim() || "(payload)";
  const parts = [entry.identifierPath];
  if (entry.displayNamePath?.trim()) parts.push(entry.displayNamePath.trim());
  const attributes = (entry.attributePaths ?? [])
    .map((attribute) => `+${attribute.name}`)
    .join(" ");
  return `${where} → ${parts.join(" / ")}${attributes ? ` ${attributes}` : ""}`;
};

function PartyMappingHistory({
  organizationSegment,
  onUnauthorized,
}: {
  organizationSegment: string;
  onUnauthorized: () => void;
}) {
  return (
    <HistoryPanel
      entityType="PartyMapping"
      path={historyPath.PartyMapping(organizationSegment)}
      onUnauthorized={onUnauthorized}
      renderChange={(change, { isInitialSnapshot }) =>
        change.field === MAPPINGS_AUDIT_FIELD ? (
          <PartyMappingsChange
            change={change}
            isInitialSnapshot={isInitialSnapshot}
          />
        ) : null
      }
      lifecycleNotes={{
        softDeleted:
          "Resolution stopped — evaluations from here on recorded no parties. Everyone already resolved was kept, links and assessments included.",
        recovered:
          "Resolution resumed, with the declaration exactly as it stood — no new version was created.",
      }}
      scopeNote="Only the declaration is recorded here. The parties it resolved are not — a party's own trail records the risk assessments made against it."
    />
  );
}
