"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import {
  ArrowDown,
  ArrowUp,
  ChevronDown,
  ChevronRight,
  Plus,
  RotateCcw,
  Trash2,
  X,
} from "lucide-react";

import {
  AuditLogChange,
  SchemaField,
  SchemaFieldType,
  TransactionSchema,
  TransactionSchemaResource,
  TransactionSchemaVersion,
  TransactionSchemaVersionResource,
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
  DraftField,
  ELEMENT_TYPES,
  MAX_ALLOWED_VALUES,
  MAX_ALLOWED_VALUE_LENGTH,
  MAX_FIELDS,
  MAX_NESTING,
  SCHEMA_FIELD_TYPES,
  addDraftUnder,
  declaresMembers,
  describeSchemaField,
  diffSchemaFields,
  moveDraftAt,
  newDraftField,
  parseSchemaFields,
  removeDraftAt,
  retypeDraft,
  sameFields,
  toDrafts,
  toRequestFields,
  updateDraftAt,
  validateDrafts,
} from "@/lib/transactionSchema";
import { invalidateTransactionSchema } from "@/lib/useTransactionSchema";
import { cn } from "@/lib/utils";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

const SECTIONS = ["Schema", "Versions", "History"] as const;
type Section = (typeof SECTIONS)[number];

const VERSIONS_PAGE_SIZE = 10;

const TYPE_HINTS: Record<SchemaFieldType, string> = {
  String: "Text. The only type that may enumerate its permitted values.",
  Number: "Numeric — one of the two types the ordering comparisons can be used on.",
  Boolean: "True or false.",
  DateTime:
    "An ISO-8601 timestamp. Read as UTC when it carries no offset, so the same payload evaluates identically wherever the engine is running. This is what the time-of-day and account-age functions read.",
  Object: "A nested object. Declare the fields it carries below.",
  Array:
    "A list. Say what it holds — a rule addresses its contents as name[0] or name[*].",
};

const ROW_KEY = /^Body((?:\.Fields\[\d+\])+)/;
const ROW_INDEX = /\[(\d+)\]/g;

const PROP_LABELS: Record<string, string> = {
  Name: "name",
  Type: "type",
  IsRequired: "required",
  AllowedValues: "allowed values",
  ElementType: "element type",
  Fields: "fields",
};

const countDrafts = (drafts: DraftField[]): number =>
  drafts.reduce((total, draft) => total + 1 + countDrafts(draft.fields), 0);

const countFields = (fields: SchemaField[]): number =>
  fields.reduce(
    (total, field) => total + 1 + countFields(field.fields ?? []),
    0
  );

const errorWithin = (
  drafts: DraftField[],
  errors: Record<string, string>
): boolean =>
  drafts.some((draft) => !!errors[draft.id] || errorWithin(draft.fields, errors));

const draftAtIndices = (
  drafts: DraftField[],
  indices: number[]
): DraftField | undefined => {
  let level = drafts;
  let found: DraftField | undefined;

  for (const index of indices) {
    found = level[index];
    if (!found) return undefined;
    level = found.fields;
  }

  return found;
};

const validationLabels = (drafts: DraftField[]): Record<string, string> => {
  const labels: Record<string, string> = { "Body.Fields": "Fields" };

  const walk = (level: DraftField[], keyPrefix: string, namePrefix: string) => {
    level.forEach((draft, index) => {
      const key = `${keyPrefix}[${index}]`;
      const own = draft.name.trim() || `Field ${index + 1}`;
      const who = namePrefix ? `${namePrefix}.${own}` : own;

      labels[key] = who;
      for (const [property, label] of Object.entries(PROP_LABELS)) {
        labels[`${key}.${property}`] = `${who} ${label}`;
      }

      if (draft.fields.length) walk(draft.fields, `${key}.Fields`, who);
    });
  };

  walk(drafts, "Body.Fields", "");

  return labels;
};

export function TransactionSchemaPanel({
  organizationSegment,
  canEdit = true,
  onUnauthorized,
}: {
  organizationSegment: string;
  canEdit?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const base = `${API_BASE}/organizations/${organizationSegment}/transaction-schema`;

  const [section, setSection] = useState<Section>("Schema");
  const [schema, setSchema] = useState<TransactionSchema | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [failed, setFailed] = useState<boolean>(false);
  const [drafts, setDrafts] = useState<DraftField[]>([]);
  const [saving, setSaving] = useState<boolean>(false);
  const [rowErrors, setRowErrors] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [conflicted, setConflicted] = useState<boolean>(false);
  const [versionsKey, setVersionsKey] = useState<number>(0);
  const [withdrawing, setWithdrawing] = useState<boolean>(false);
  const [restoring, setRestoring] = useState<boolean>(false);
  const [confirmingWithdraw, setConfirmingWithdraw] = useState<boolean>(false);

  const fetchSchema = useCallback(
    async ({ keepDrafts = false }: { keepDrafts?: boolean } = {}) => {
      setLoading(true);
      try {
        const response = await apiFetch(`${base}?includeDeleted=true`, {
          credentials: "include",
        });

        if (response.ok) {
          const resource = await unwrap<TransactionSchemaResource>(response);
          setSchema(resource.transactionSchema);
          setFailed(false);
          if (!keepDrafts) {
            setDrafts(
              resource.transactionSchema.isDeleted
                ? []
                : toDrafts(resource.transactionSchema.fields)
            );
          }
          return;
        }

        if (response.status === 404) {
          setSchema(null);
          setFailed(false);
          if (!keepDrafts) setDrafts([]);
          return;
        }

        setFailed(true);
        await reportApiError(response, {
          fallback: t("organization.failedLoadTransactionSchema"),
          onUnauthorized,
          toastId: "error-transaction-schema",
        });
      } catch {
        setFailed(true);
        toast.error(t("organization.failedLoadTransactionSchema"), {
          id: "error-transaction-schema",
        });
      } finally {
        setLoading(false);
      }
    },
    [base, onUnauthorized]
  );

  useEffect(() => {
    fetchSchema();
  }, [fetchSchema]);

  const enforcing = schema !== null && !schema.isDeleted;
  const withdrawn = schema !== null && schema.isDeleted;
  const everExisted = schema !== null;

  const requestFields = useMemo(() => toRequestFields(drafts), [drafts]);
  const fieldCount = useMemo(() => countDrafts(drafts), [drafts]);
  const dirty = !sameFields(requestFields, enforcing ? schema.fields : []);
  const defining = !enforcing && drafts.length > 0;

  const editDrafts = (next: (prev: DraftField[]) => DraftField[]) => {
    setDrafts(next);
    setFormError(null);
  };

  const updateDraft = (id: string, updates: Partial<DraftField>) => {
    editDrafts((prev) => updateDraftAt(prev, id, updates));
    setRowErrors((prev) => {
      if (!prev[id]) return prev;
      const next = { ...prev };
      delete next[id];
      return next;
    });
  };

  const reset = () => {
    setDrafts(enforcing ? toDrafts(schema.fields) : []);
    setRowErrors({});
    setFormError(null);
    setConflicted(false);
  };

  const save = async () => {
    const validation = validateDrafts(drafts);
    setRowErrors(validation.fieldErrors);
    setFormError(validation.formError);

    if (validation.formError || Object.keys(validation.fieldErrors).length > 0) {
      toast.error(
        validation.formError ?? t("organization.someFieldsNeedFixingBefore")
      );
      return;
    }

    setSaving(true);

    try {
      const response = await apiFetch(base, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ fields: requestFields }),
        credentials: "include",
      });

      if (response.ok) {
        const resource = await unwrap<TransactionSchemaResource>(response);
        setSchema(resource.transactionSchema);
        setDrafts(toDrafts(resource.transactionSchema.fields));
        setConflicted(false);
        setVersionsKey((key) => key + 1);
        invalidateTransactionSchema();
        toast.success(
          t(
          "organization.transactionSchemaSavedAsVersion",
          resource.transactionSchema.currentVersionNumber
        )
        );
        return;
      }

      if (response.status === 409) {
        setConflicted(true);
        await fetchSchema({ keepDrafts: true });
        toast.error(t("organization.schemaChangedYouWereEditing"), {
          description:
            t("organization.yourChangesHaveNotBeen"),
        });
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
          const next: Record<string, string> = {};
          for (const [key, messages] of Object.entries(errors)) {
            const chain = ROW_KEY.exec(key)?.[1];
            if (chain === undefined) continue;

            const indices = [...chain.matchAll(ROW_INDEX)].map((match) =>
              Number(match[1])
            );
            const draft = draftAtIndices(drafts, indices);
            if (draft) {
              next[draft.id] = [next[draft.id], messages.join(" ")]
                .filter(Boolean)
                .join(" ");
            }
          }
          setRowErrors(next);
        }
      }

      await reportApiError(response, {
        fallback: t("organization.failedSaveTransactionSchema"),
        onUnauthorized,
        fieldLabels: validationLabels(drafts),
      });
    } catch {
      toast.error(t("organization.failedSaveTransactionSchema"));
    } finally {
      setSaving(false);
    }
  };

  const reportStaleState = async (message: string) => {
    toast.error(message, {
      description: t("organization.reloadingCurrentStateSchema"),
    });
    await fetchSchema();
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
        setFormError(null);
        setConflicted(false);
        await fetchSchema();
        setVersionsKey((key) => key + 1);
        invalidateTransactionSchema();
        toast.success(t("organization.organisationNoLongerEnforcingSchema"), {
          description:
            t("organization.existingRulesAreUnaffectedField"),
        });
        return;
      }

      if (response.status === 404) {
        setConfirmingWithdraw(false);
        await reportStaleState("There is no schema in force to withdraw");
        return;
      }

      await reportApiError(response, {
        fallback: t("organization.failedWithdrawTransactionSchema"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("organization.failedWithdrawTransactionSchema"));
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
        await fetchSchema();
        setVersionsKey((key) => key + 1);
        invalidateTransactionSchema();
        toast.success(t("organization.transactionSchemaRestored"), {
          description:
            t("organization.backForceExactlyStoodNo"),
        });
        return;
      }

      if (response.status === 400) {
        await reportStaleState("This schema is already being enforced");
        return;
      }

      if (response.status === 404) {
        await reportStaleState("There is no withdrawn schema to restore");
        return;
      }

      await reportApiError(response, {
        fallback: t("organization.failedRestoreTransactionSchema"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("organization.failedRestoreTransactionSchema"));
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
              status={enforcing ? "Enabled" : "Disabled"}
              label={enforcing ? "Enforcing" : t("organization.notEnforcing")}
            />
            {schema && (
              <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {enforcing
                  ? `Current version v${schema.currentVersionNumber}`
                  : `Last version v${schema.currentVersionNumber}`}
              </span>
            )}
          </div>
        )}
      </div>

      {section === "Schema" && (
        <Card>
          <CardContent className="space-y-4 pt-6">
            <div>
              <h2 className="text-sm font-semibold text-slate-900">
                {t("organization.transactionSchema")}
              </h2>
              <p className="mt-0.5 text-sm text-slate-500">
                The fields your transaction payloads carry. Once declared, the
                API rejects a rule that names a field which isn&apos;t here, and
                a payload that doesn&apos;t match — instead of a misspelled field
                quietly evaluating to false forever while the rule looks live.
              </p>
            </div>

            {loading ? (
              <div className="space-y-2">
                {Array.from({ length: 3 }).map((_, index) => (
                  <Skeleton key={index} className="h-12 w-full" />
                ))}
              </div>
            ) : failed ? (
              <div className="rounded-md border border-dashed border-slate-200 py-12 text-center">
                <p className="text-sm text-slate-500">
                  {t("organization.theSchemaCouldNotBe")}
                </p>
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-3"
                  onClick={() => fetchSchema()}
                >
                  {t("common.retry")}
                </Button>
              </div>
            ) : !enforcing && !defining ? (
              <NoSchemaInForce
                canEdit={canEdit}
                withdrawn={withdrawn ? schema : null}
                restoring={restoring}
                onDefine={() => editDrafts(() => [newDraftField()])}
                onRestore={restore}
              />
            ) : (
              <>
                {conflicted && (
                  <div className="rounded-md border border-attention-200 bg-attention-50 px-3 py-2 text-xs text-attention-700">
                    Someone else saved this schema while you had it open. What
                    you see below is still your version — the stored one is now
                    v{schema?.currentVersionNumber}. Saving again replaces it.
                  </div>
                )}

                <div className="space-y-2">
                  {drafts.map((draft, index) => (
                    <FieldRow
                      key={draft.id}
                      draft={draft}
                      index={index}
                      total={drafts.length}
                      depth={0}
                      readOnly={!canEdit || saving}
                      errors={rowErrors}
                      onChange={updateDraft}
                      onRemove={(id) =>
                        editDrafts((prev) => removeDraftAt(prev, id))
                      }
                      onMove={(id, offset) =>
                        editDrafts((prev) => moveDraftAt(prev, id, offset))
                      }
                      onAddMember={(id) =>
                        editDrafts((prev) => addDraftUnder(prev, id))
                      }
                    />
                  ))}
                </div>

                {formError && (
                  <p className="text-xs text-critical-700">{formError}</p>
                )}

                {canEdit && (
                  <div className="flex flex-wrap items-center gap-2 border-t border-slate-100 pt-4">
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      disabled={saving || fieldCount >= MAX_FIELDS}
                      onClick={() => editDrafts((prev) => addDraftUnder(prev, null))}
                    >
                      <Plus className="size-4" />
                      {t("organization.addField")}
                    </Button>
                    <Button
                      size="sm"
                      onClick={save}
                      disabled={saving || !dirty}
                    >
                      {saving ? "Saving..." : "Save schema"}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={reset}
                      disabled={saving || !dirty}
                    >
                      {t("common.reset")}
                    </Button>
                    <span className="ml-auto font-mono text-xs uppercase tracking-widest text-slate-400">
                      {fieldCount} of {MAX_FIELDS}
                    </span>
                    {enforcing && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-critical-500 hover:text-critical-700"
                        disabled={saving || withdrawing}
                        onClick={() => setConfirmingWithdraw(true)}
                      >
                        {t("organization.stopEnforcing")}
                      </Button>
                    )}
                  </div>
                )}

                <p className="text-xs text-slate-400">
                  Saving replaces the schema outright and snapshots a new
                  version — a field removed here stops being addressable by any
                  rule, and existing rules that name it will be rejected the
                  next time they are edited.
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
          currentVersionNumber={schema?.currentVersionNumber ?? null}
          onUnauthorized={onUnauthorized}
        />
      )}

      {section === "History" && everExisted && (
        <TransactionSchemaHistory
          organizationSegment={organizationSegment}
          onUnauthorized={onUnauthorized}
        />
      )}

      <WithdrawDialog
        open={confirmingWithdraw}
        withdrawing={withdrawing}
        fieldCount={schema ? countFields(schema.fields) : 0}
        discardsEdits={dirty}
        onOpenChange={setConfirmingWithdraw}
        onConfirm={withdraw}
      />
    </div>
  );
}

function WithdrawDialog({
  open,
  withdrawing,
  fieldCount,
  discardsEdits,
  onOpenChange,
  onConfirm,
}: {
  open: boolean;
  withdrawing: boolean;
  fieldCount: number;
  discardsEdits: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: () => void;
}) {
  const t = useTranslation();
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t("organization.stopEnforcingThisSchema")}</DialogTitle>
        </DialogHeader>

        <div className="space-y-3 text-sm text-slate-600">
          <p>
            This organisation goes back to being schema-less. Rules and payloads
            stop being checked against it immediately, on every path — authoring
            a rule, saving a version, validate, test, and every evaluate route.
          </p>
          <ul className="space-y-2">
            <li className="flex gap-2">
              <span aria-hidden className="text-settled-500">
                ✓
              </span>
              <span>
                <span className="font-medium text-slate-900">
                  {t("organization.nothingAlreadyAuthoredBreaks")}
                </span>{" "}
                Every existing rule keeps working exactly as it does now,
                including the ones that name fields from this schema.
              </span>
            </li>
            <li className="flex gap-2">
              <span aria-hidden className="text-settled-500">
                ✓
              </span>
              <span>
                <span className="font-medium text-slate-900">
                  {t("organization.theFieldListIsKept")}
                </span>{" "}
                All {fieldCount} {fieldCount === 1 ? "field" : "fields"} stay
                stored, and restoring puts them back exactly as they are without
                creating a new version.
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
              and restorable is the schema as last saved.
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
            {withdrawing ? "Stopping..." : "Stop enforcing"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function NoSchemaInForce({
  canEdit,
  withdrawn,
  restoring,
  onDefine,
  onRestore,
}: {
  canEdit: boolean;
  withdrawn: TransactionSchema | null;
  restoring: boolean;
  onDefine: () => void;
  onRestore: () => void;
}) {
  const [showFields, setShowFields] = useState<boolean>(false);

  return (
    <div className="rounded-lg border border-dashed border-slate-300 bg-slate-50/60 px-6 py-12">
      <div className="text-center">
        <p className="text-sm font-medium text-slate-600">
          {withdrawn
            ? "This schema is not being enforced"
            : "No transaction schema yet"}
        </p>
        <p className="mx-auto mt-2 max-w-lg text-xs text-slate-500">
          {withdrawn ? (
            <>
              It was withdrawn
              {withdrawn.deletedAt && (
                <> on <LocalTime value={withdrawn.deletedAt} /></>
              )}
              , so rules and payloads are no longer checked against it. Every
              rule written against it still works — nothing broke when it was
              withdrawn, and nothing breaks while it stays this way. The field
              list from v{withdrawn.currentVersionNumber} is kept, and restoring
              puts it back exactly as it was.
            </>
          ) : (
            <>
              Nothing is being enforced. Rules may name any field they like, and
              payloads are accepted whatever they carry — which is also how a
              misspelled field ends up in a rule that never fires. Declare the
              fields your payloads use and the API starts rejecting both.
            </>
          )}
        </p>

        {canEdit && (
          <div className="mt-4 flex flex-wrap items-center justify-center gap-2">
            {withdrawn && (
              <Button
                size="sm"
                className="gap-1.5"
                disabled={restoring}
                onClick={onRestore}
              >
                <RotateCcw className="size-4" />
                {restoring ? "Restoring..." : "Restore previous schema"}
              </Button>
            )}
            <Button
              size="sm"
              variant={withdrawn ? "outline" : "default"}
              className="gap-1.5"
              onClick={onDefine}
            >
              <Plus className="size-4" />
              {withdrawn ? "Declare a new one instead" : "Define a schema"}
            </Button>
          </div>
        )}

        {withdrawn && (
          <Button
            variant="ghost"
            size="sm"
            className="mt-2 h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
            onClick={() => setShowFields((open) => !open)}
          >
            {showFields
              ? "Hide the withdrawn field list"
              : `Show the ${withdrawn.fields.length} withdrawn ${
                  withdrawn.fields.length === 1 ? "field" : "fields"
                }`}
          </Button>
        )}
      </div>

      {withdrawn && showFields && (
        <div className="mt-4 text-left">
          <SchemaFieldTable fields={withdrawn.fields} />
        </div>
      )}
    </div>
  );
}

function FieldRow({
  draft,
  index,
  total,
  depth,
  readOnly,
  errors,
  onChange,
  onRemove,
  onMove,
  onAddMember,
}: {
  draft: DraftField;
  index: number;
  total: number;
  depth: number;
  readOnly: boolean;
  errors: Record<string, string>;
  onChange: (id: string, updates: Partial<DraftField>) => void;
  onRemove: (id: string) => void;
  onMove: (id: string, offset: number) => void;
  onAddMember: (parentId: string) => void;
}) {
  const t = useTranslation();
  const error = errors[draft.id];
  const members = declaresMembers(draft.type, draft.elementType);
  const canNest = depth + 1 < MAX_NESTING;

  const [collapsed, setCollapsed] = useState(false);
  const nestedError = members && errorWithin(draft.fields, errors);
  useEffect(() => {
    if (nestedError) setCollapsed(false);
  }, [nestedError]);

  return (
    <div
      className={cn(
        "rounded-lg border bg-card p-3",
        error ? "border-critical-200 ring-1 ring-critical-200" : "border-slate-200"
      )}
    >
      <div className="flex flex-wrap items-center gap-2">
        {!readOnly && (
          <div className="flex shrink-0 flex-col">
            <button
              type="button"
              aria-label={t("organization.moveUp")}
              disabled={index === 0}
              onClick={() => onMove(draft.id, -1)}
              className="rounded p-0.5 text-slate-300 hover:text-slate-600 disabled:opacity-30"
            >
              <ArrowUp className="size-3.5" />
            </button>
            <button
              type="button"
              aria-label={t("organization.moveDown")}
              disabled={index === total - 1}
              onClick={() => onMove(draft.id, 1)}
              className="rounded p-0.5 text-slate-300 hover:text-slate-600 disabled:opacity-30"
            >
              <ArrowDown className="size-3.5" />
            </button>
          </div>
        )}

        {members ? (
          <button
            type="button"
            aria-expanded={!collapsed}
            aria-label={collapsed ? t("organization.showFields") : t("organization.hideFields")}
            title={collapsed ? t("organization.showFields") : t("organization.hideFields")}
            onClick={() => setCollapsed((value) => !value)}
            className="shrink-0 rounded p-0.5 text-slate-400 hover:text-slate-700"
          >
            {collapsed ? (
              <ChevronRight className="size-4" />
            ) : (
              <ChevronDown className="size-4" />
            )}
          </button>
        ) : (
          <span className="size-5 shrink-0" aria-hidden />
        )}

        <Input
          placeholder={t("organization.fieldName")}
          value={draft.name}
          onChange={(event) => onChange(draft.id, { name: event.target.value })}
          disabled={readOnly}
          className="w-56 font-mono"
        />

        <Select
          value={draft.type}
          onValueChange={(value) =>
            onChange(draft.id, retypeDraft(draft, value as SchemaFieldType))
          }
          disabled={readOnly}
        >
          <SelectTrigger className="w-32">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {SCHEMA_FIELD_TYPES.map((type) => (
              <SelectItem
                key={type}
                value={type}
                disabled={!canNest && (type === "Object" || type === "Array")}
              >
                {type}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {draft.type === "Array" && (
          <div className="flex items-center gap-1.5">
            <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
              of
            </span>
            <Select
              value={draft.elementType ?? "String"}
              onValueChange={(value) =>
                onChange(draft.id, { elementType: value as SchemaFieldType })
              }
              disabled={readOnly}
            >
              <SelectTrigger className="w-32">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {ELEMENT_TYPES.map((type) => (
                  <SelectItem
                    key={type}
                    value={type}
                    disabled={!canNest && type === "Object"}
                  >
                    {type}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}

        <label className="flex items-center gap-2 font-mono text-sm text-slate-700">
          <input
            type="checkbox"
            checked={draft.isRequired}
            onChange={(event) =>
              onChange(draft.id, { isRequired: event.target.checked })
            }
            disabled={readOnly}
            className="h-4 w-4 rounded border-slate-300 accent-brand-800"
          />
          {t("organization.required")}
        </label>

        {!readOnly && (
          <Button
            variant="ghost"
            size="icon"
            className="ml-auto h-8 w-8 text-critical-500 hover:text-critical-700"
            onClick={() => onRemove(draft.id)}
            title={t("organization.removeField")}
          >
            <Trash2 className="size-4" />
          </Button>
        )}
      </div>

      {!(members && collapsed) && (
        <p className="mt-1.5 text-xs text-slate-400">
          {TYPE_HINTS[draft.type]}
          {draft.type === "Array" && draft.elementType && draft.name.trim() && (
            <>
              {" "}
              Rules address it as{" "}
              <span className="font-mono text-slate-500">
                {draft.name.trim()}
                {draft.elementType === "Object" ? "[*].field" : "[*]"}
              </span>
              .
            </>
          )}
        </p>
      )}

      {draft.type === "String" && (
        <AllowedValuesEditor
          values={draft.allowedValues}
          readOnly={readOnly}
          onChange={(allowedValues) => onChange(draft.id, { allowedValues })}
        />
      )}

      {error && <p className="mt-2 text-xs text-critical-700">{error}</p>}

      {members && collapsed && (
        <button
          type="button"
          onClick={() => setCollapsed(false)}
          className="mt-2 font-mono text-xs uppercase tracking-widest text-slate-400 hover:text-slate-600"
        >
          {draft.fields.length === 0
            ? "No fields declared yet — show"
            : `${draft.fields.length} ${
                draft.fields.length === 1 ? "field" : "fields"
              } hidden — show`}
        </button>
      )}

      {members && !collapsed && (
        <div className="mt-3 space-y-2 border-l-2 border-slate-100 pl-3">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {draft.type === "Object"
                ? "Fields it carries"
                : "Fields each element carries"}
            </span>
            {!readOnly && (
              <Button
                variant="outline"
                size="sm"
                className="h-7 gap-1 text-xs"
                onClick={() => onAddMember(draft.id)}
              >
                <Plus className="size-3" />
                {t("organization.addField")}
              </Button>
            )}
          </div>

          {draft.fields.length === 0 ? (
            <p className="text-xs text-slate-400">
              None yet — {draft.type === "Object" ? "an object" : "an array of objects"}{" "}
              must declare what it carries, or the save is rejected.
            </p>
          ) : (
            draft.fields.map((member, memberIndex) => (
              <FieldRow
                key={member.id}
                draft={member}
                index={memberIndex}
                total={draft.fields.length}
                depth={depth + 1}
                readOnly={readOnly}
                errors={errors}
                onChange={onChange}
                onRemove={onRemove}
                onMove={onMove}
                onAddMember={onAddMember}
              />
            ))
          )}
        </div>
      )}

      {!members && draft.fields.length > 0 && (
        <p className="mt-2 text-xs text-slate-400">
          {draft.fields.length}{" "}
          {draft.fields.length === 1 ? "field" : "fields"} declared underneath
          are kept but not saved while this is{" "}
          {draft.type === "Array" ? `an array of ${draft.elementType}` : `a ${draft.type}`}.
        </p>
      )}
    </div>
  );
}

function AllowedValuesEditor({
  values,
  readOnly,
  onChange,
}: {
  values: string[];
  readOnly: boolean;
  onChange: (values: string[]) => void;
}) {
  const t = useTranslation();
  const [pending, setPending] = useState<string>("");

  const add = () => {
    const value = pending.trim();
    if (!value) return;
    if (values.includes(value)) {
      setPending("");
      return;
    }
    onChange([...values, value]);
    setPending("");
  };

  return (
    <div className="mt-2 space-y-1.5 border-t border-slate-100 pt-2">
      <span className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {t("organization.allowedValues")}
      </span>

      {values.length > 0 && (
        <div className="flex flex-wrap gap-1.5">
          {values.map((value) => (
            <span
              key={value}
              className="inline-flex items-center gap-1 rounded-md bg-slate-100 px-2 py-0.5 font-mono text-xs text-slate-700"
            >
              {value}
              {!readOnly && (
                <button
                  type="button"
                  aria-label={`Remove ${value}`}
                  onClick={() => onChange(values.filter((v) => v !== value))}
                  className="text-slate-400 hover:text-critical-700"
                >
                  <X className="size-3" />
                </button>
              )}
            </span>
          ))}
        </div>
      )}

      {!readOnly && (
        <div className="flex items-center gap-2">
          <Input
            placeholder={t("organization.addAPermittedValue")}
            value={pending}
            maxLength={MAX_ALLOWED_VALUE_LENGTH}
            disabled={values.length >= MAX_ALLOWED_VALUES}
            onChange={(event) => setPending(event.target.value)}
            onKeyDown={(event) => {
              if (event.key !== "Enter") return;
              // The row sits inside no form, but Enter here would still bubble
              // to whatever dialog is hosting the panel.
              event.preventDefault();
              add();
            }}
            className="w-64 font-mono"
          />
          <Button
            variant="outline"
            size="sm"
            onClick={add}
            disabled={!pending.trim() || values.length >= MAX_ALLOWED_VALUES}
          >
            {t("organization.add")}
          </Button>
        </div>
      )}

      {values.length === 0 && (
        <p className="text-xs text-slate-400">
          None — the field accepts any text. Add values to restrict it, and both
          rules and payloads will be held to the list.
        </p>
      )}
    </div>
  );
}

interface FlatField {
  field: SchemaField;
  depth: number;
  path: string;
  key: string;
  hasMembers: boolean;
  descendants: number;
}

const flattenFields = (
  fields: SchemaField[],
  depth = 0,
  prefix = "",
  keyPrefix = ""
): FlatField[] =>
  fields.flatMap((field, index) => {
    const wildcard = field.type === "Array" ? "[*]" : "";
    const path = prefix ? `${prefix}.${field.name}` : field.name;
    const key = `${keyPrefix}${index}-${field.name}`;

    const members = field.fields ?? [];
    const row: FlatField = {
      field,
      depth,
      path,
      key,
      hasMembers: members.length > 0,
      descendants: countFields(members),
    };

    if (members.length === 0) return [row];

    return [
      row,
      ...flattenFields(members, depth + 1, `${path}${wildcard}`, `${key}/`),
    ];
  });

export function SchemaFieldTable({
  fields,
  emptyLabel,
}: {
  fields: SchemaField[];
  emptyLabel?: string;
}) {
  const t = useTranslation();
  const [collapsed, setCollapsed] = useState<string[]>([]);

  const toggle = (key: string) =>
    setCollapsed((keys) =>
      keys.includes(key) ? keys.filter((k) => k !== key) : [...keys, key]
    );

  if (fields.length === 0) {
    return (
      <p className="text-sm text-slate-400">
        {emptyLabel ?? t("transactionSchema.noFields")}
      </p>
    );
  }

  const rows = flattenFields(fields).filter(
    (row) => !collapsed.some((key) => row.key.startsWith(`${key}/`))
  );

  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-card">
      <Table>
        <TableHeader>
          <TableRow className="bg-slate-50 hover:bg-slate-50">
            <TableHead>{t("organization.field")}</TableHead>
            <TableHead className="w-32">{t("organization.type")}</TableHead>
            <TableHead className="w-28">{t("organization.required")}</TableHead>
            <TableHead>{t("organization.allowedValues")}</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.map(({ field, depth, path, key, hasMembers, descendants }) => (
            <TableRow key={key} className="hover:bg-slate-50/50">
              <TableCell
                className="font-mono text-sm text-slate-900"
                style={{ paddingLeft: `${1.5 + depth * 1.25}rem` }}
              >
                <span className="flex items-center gap-1">
                  {hasMembers ? (
                    <button
                      type="button"
                      aria-expanded={!collapsed.includes(key)}
                      aria-label={
                        collapsed.includes(key)
                          ? `Show the fields under ${field.name}`
                          : `Hide the fields under ${field.name}`
                      }
                      onClick={() => toggle(key)}
                      className="shrink-0 rounded text-slate-400 hover:text-slate-700"
                    >
                      {collapsed.includes(key) ? (
                        <ChevronRight className="size-4" />
                      ) : (
                        <ChevronDown className="size-4" />
                      )}
                    </button>
                  ) : (
                    <span className="size-4 shrink-0" aria-hidden />
                  )}
                  {field.name}
                  {depth > 0 && (
                    <span className="ml-1 text-xs text-slate-400">{path}</span>
                  )}
                  {collapsed.includes(key) && (
                    <span className="text-xs text-slate-400">
                      {descendants} hidden
                    </span>
                  )}
                </span>
              </TableCell>
              <TableCell className="font-mono text-xs text-slate-600">
                {field.type === "Array"
                  ? `Array of ${field.elementType ?? "?"}`
                  : field.type}
              </TableCell>
              <TableCell
                className={cn(
                  "font-mono text-xs",
                  field.isRequired ? "text-attention-700" : "text-slate-400"
                )}
              >
                {field.isRequired ? "Required" : "Optional"}
              </TableCell>
              <TableCell className="font-mono text-xs text-slate-600">
                {field.allowedValues?.length
                  ? field.allowedValues.join(", ")
                  : "—"}
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
  const [versions, setVersions] = useState<TransactionSchemaVersion[]>([]);
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
          const page =
            await unwrapPage<TransactionSchemaVersionResource>(response);
          setVersions(page.data.map((r) => r.transactionSchemaVersion));
          setTotalPages(page.metadata.pageCount || 1);
          return;
        }

        if (response.status === 404) {
          setVersions([]);
          setTotalPages(1);
          return;
        }

        await reportApiError(response, {
          fallback: t("organization.failedLoadSchemaSVersions"),
          onUnauthorized,
          toastId: "error-transaction-schema-versions",
        });
      } catch {
        if (!cancelled) {
          toast.error(t("organization.failedLoadSchemaSVersions"), {
            id: "error-transaction-schema-versions",
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
            Every save snapshots the whole field list. Open one to read the
            schema as it stood then.
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
                  key={version.transactionSchemaVersionId}
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
                      {version.fields.length}{" "}
                      {version.fields.length === 1 ? "field" : "fields"}
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
                      <SchemaFieldTable fields={version.fields} />
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

const FIELDS_AUDIT_FIELD = "Fields";

const CHANGE_STYLES: Record<string, string> = {
  added: "border-settled-200 bg-settled-50 text-settled-700",
  removed: "border-critical-200 bg-critical-50 text-critical-700",
  changed: "border-attention-200 bg-attention-50 text-attention-700",
  unchanged: "border-slate-200 bg-slate-50 text-slate-500",
};

function SchemaFieldsChange({
  change,
  isInitialSnapshot,
}: {
  change: AuditLogChange;
  isInitialSnapshot: boolean;
}) {
  const t = useTranslation();
  const before = isInitialSnapshot ? null : parseSchemaFields(change.oldValue);
  const after = parseSchemaFields(change.newValue);

  if (after === null && before === null) {
    return (
      <p className="text-sm text-slate-400">{t("organization.unreadableSchemaFieldList")}</p>
    );
  }

  const rows = diffSchemaFields(before, after).filter(
    (row) => isInitialSnapshot || row.change !== "unchanged"
  );

  if (rows.length === 0) {
    return (
      <p className="text-sm text-slate-400">
        {t("organization.theFieldListWasRewritten")}
      </p>
    );
  }

  return (
    <ul className="space-y-1.5">
      {rows.map((row) => (
        <li
          key={row.name}
          className="flex flex-wrap items-baseline gap-2"
          style={{ paddingLeft: `${row.depth * 1.25}rem` }}
        >
          <span
            className={cn(
              "rounded-md border px-1.5 py-0.5 font-mono text-[11px] uppercase tracking-wide",
              CHANGE_STYLES[row.change]
            )}
          >
            {isInitialSnapshot ? "field" : row.change}
          </span>
          <span className="font-mono text-sm text-slate-900">{row.name}</span>
          {row.before && row.after && (
            <span className="text-xs text-slate-500">
              {describeSchemaField(row.before)}{" "}
              <span aria-hidden className="text-slate-300">
                →
              </span>{" "}
              {describeSchemaField(row.after)}
            </span>
          )}
          {row.before && !row.after && (
            <span className="text-xs text-slate-500">
              {describeSchemaField(row.before)}
            </span>
          )}
          {!row.before && row.after && (
            <span className="text-xs text-slate-500">
              {describeSchemaField(row.after)}
            </span>
          )}
        </li>
      ))}
    </ul>
  );
}

function TransactionSchemaHistory({
  organizationSegment,
  onUnauthorized,
}: {
  organizationSegment: string;
  onUnauthorized: () => void;
}) {
  return (
    <HistoryPanel
      entityType="TransactionSchema"
      path={historyPath.TransactionSchema(organizationSegment)}
      onUnauthorized={onUnauthorized}
      renderChange={(change, { isInitialSnapshot }) =>
        change.field === FIELDS_AUDIT_FIELD ? (
          <SchemaFieldsChange
            change={change}
            isInitialSnapshot={isInitialSnapshot}
          />
        ) : null
      }
      lifecycleNotes={{
        softDeleted:
          "Enforcement stopped — rules and payloads were no longer checked against this schema. Existing rules were unaffected, and the field list was kept.",
        recovered:
          "Enforcement resumed, with the field list exactly as it stood — no new version was created.",
      }}
      scopeNote="Only the schema itself is recorded here. A rule rejected for not matching it leaves no entry — nothing was written."
    />
  );
}
