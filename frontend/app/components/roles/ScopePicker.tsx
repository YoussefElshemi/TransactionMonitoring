"use client";

import { PermissionScopeType, PERMISSION_SCOPE_TYPES } from "@/types";
import {
  SCOPE_TYPE_DESCRIPTIONS,
  SCOPE_TYPE_LABELS,
} from "@/lib/permissions";
import {
  SCOPE_LOOKUP_PERMISSIONS,
  useQueueOptions,
  useRuleSetOptions,
  useSubOrganizations,
} from "@/lib/roles";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTranslation } from "@/context/LocalizationContext";

export interface ScopeValue {
  scopeType: PermissionScopeType;
  subOrganizationId: string;
  ruleSetId: string;
  queueId: string;
}

export const EMPTY_SCOPE: ScopeValue = {
  scopeType: "Organization",
  subOrganizationId: "",
  ruleSetId: "",
  queueId: "",
};

export const scopeValueId = (scope: ScopeValue): string => {
  switch (scope.scopeType) {
    case "SubOrganization":
      return scope.subOrganizationId;
    case "RuleSet":
      return scope.ruleSetId;
    case "Queue":
      return scope.queueId;
    default:
      return "";
  }
};

export const scopeIncomplete = (scope: ScopeValue): boolean =>
  scope.scopeType !== "Organization" && scopeValueId(scope).trim() === "";

export const scopeBody = (scope: ScopeValue): Record<string, string> => {
  switch (scope.scopeType) {
    case "SubOrganization":
      return {
        scopeType: "SubOrganization",
        subOrganizationId: scope.subOrganizationId.trim(),
      };
    case "RuleSet":
      return { scopeType: "RuleSet", ruleSetId: scope.ruleSetId.trim() };
    case "Queue":
      return { scopeType: "Queue", queueId: scope.queueId.trim() };
    default:
      return {};
  }
};

export function ScopePicker({
  organizationId,
  value,
  onChange,
  disabled = false,
}: {
  organizationId: string;
  value: ScopeValue;
  onChange: (scope: ScopeValue) => void;
  disabled?: boolean;
}) {
  const t = useTranslation();
  const units = useSubOrganizations(
    organizationId,
    value.scopeType === "SubOrganization"
  );
  const ruleSets = useRuleSetOptions(value.scopeType === "RuleSet");
  const queues = useQueueOptions(organizationId, value.scopeType === "Queue");

  const setType = (scopeType: PermissionScopeType) =>
    // Clears the ids as well as the type: a queue id left behind from a
    // previous choice would ride along in state and be sent with the next
    // scope that happens to want the same field.
    onChange({ ...EMPTY_SCOPE, scopeType });

  return (
    <div className="space-y-3">
      <div className="space-y-1.5">
        <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
          {t("roles.reach")}
        </label>
        <Select
          value={value.scopeType}
          onValueChange={(next) => setType(next as PermissionScopeType)}
          disabled={disabled}
        >
          <SelectTrigger>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {PERMISSION_SCOPE_TYPES.map((scopeType) => (
              <SelectItem key={scopeType} value={scopeType}>
                {SCOPE_TYPE_LABELS[scopeType]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <p className="text-xs text-slate-500">
          {SCOPE_TYPE_DESCRIPTIONS[value.scopeType]}
        </p>
      </div>

      {value.scopeType === "SubOrganization" && (
        <DependentPicker
          label={t("roles.businessUnit")}
          permission={SCOPE_LOOKUP_PERMISSIONS.SubOrganization}
          loading={units.loading}
          unavailable={units.unavailable}
          truncated={units.truncated}
          totalCount={units.totalCount}
          disabled={disabled}
          options={units.data.map((unit) => ({
            id: unit.subOrganizationId,
            label: unit.name,
            note: unit.isEnabled ? undefined : "disabled",
          }))}
          value={value.subOrganizationId}
          onChange={(subOrganizationId) =>
            onChange({ ...value, subOrganizationId })
          }
        />
      )}

      {value.scopeType === "RuleSet" && (
        <DependentPicker
          label={t("roles.ruleSet")}
          permission={SCOPE_LOOKUP_PERMISSIONS.RuleSet}
          loading={ruleSets.loading}
          unavailable={ruleSets.unavailable}
          truncated={ruleSets.truncated}
          totalCount={ruleSets.totalCount}
          disabled={disabled}
          options={ruleSets.data.map((ruleSet) => ({
            id: ruleSet.ruleSetId,
            label: ruleSet.friendlyName,
            note: ruleSet.isEnabled ? undefined : "disabled",
          }))}
          value={value.ruleSetId}
          onChange={(ruleSetId) => onChange({ ...value, ruleSetId })}
        />
      )}

      {value.scopeType === "Queue" && (
        <DependentPicker
          label={t("roles.queue")}
          permission={SCOPE_LOOKUP_PERMISSIONS.Queue}
          loading={queues.loading}
          unavailable={queues.unavailable}
          truncated={queues.truncated}
          totalCount={queues.totalCount}
          disabled={disabled}
          options={queues.data.map((queue) => ({
            id: queue.queueId,
            label: queue.name,
            note: queue.isEnabled ? undefined : "retired",
          }))}
          value={value.queueId}
          onChange={(queueId) => onChange({ ...value, queueId })}
        />
      )}
    </div>
  );
}

interface Option {
  id: string;
  label: string;
  note?: string;
}

function DependentPicker({
  label,
  permission,
  loading,
  unavailable,
  truncated,
  totalCount,
  disabled,
  options,
  value,
  onChange,
}: {
  label: string;
  permission: string;
  loading: boolean;
  unavailable: boolean;
  truncated: boolean;
  totalCount: number | null;
  disabled: boolean;
  options: Option[];
  value: string;
  onChange: (id: string) => void;
}) {
  const t = useTranslation();

  if (unavailable) {
    return (
      <div className="space-y-1.5">
        <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
          {label} ID
        </label>
        <Input
          value={value}
          onChange={(event) => onChange(event.target.value)}
          placeholder="00000000-0000-0000-0000-000000000000"
          disabled={disabled}
          className="font-mono text-xs"
        />
        <p className="text-xs text-slate-500">
          The list could not be read — that needs{" "}
          <span className="font-mono">{permission}</span>, which granting a role
          does not include. The id works just as well here.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-1.5">
      <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
        {label}
      </label>
      <Select
        value={value}
        onValueChange={onChange}
        disabled={disabled || loading || options.length === 0}
      >
        <SelectTrigger>
          <SelectValue
            placeholder={
              loading
                ? "Loading…"
                : options.length === 0
                  ? `No ${label.toLowerCase()} to choose`
                  : `Choose a ${label.toLowerCase()}`
            }
          />
        </SelectTrigger>
        <SelectContent>
          {options.map((option) => (
            <SelectItem key={option.id} value={option.id}>
              {option.label}
              {option.note && (
                <span className="ml-2 font-mono text-[10px] uppercase tracking-wider text-slate-400">
                  {option.note}
                </span>
              )}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {!loading && options.length === 0 && (
        <p className="text-xs text-slate-500">
          {t("roles.noneToScopeTo")}
        </p>
      )}

      {truncated && (
        <div className="space-y-1.5 rounded-md border border-attention-200 bg-attention-50 px-3 py-2.5 ring-1 ring-inset ring-attention-200">
          <p className="text-xs text-slate-600">
            Showing {options.length}
            {totalCount === null ? "" : ` of ${totalCount}`} — one page is all
            the API will serve. If the {label.toLowerCase()} you want is not in
            the list, paste its id.
          </p>
          <Input
            value={value}
            onChange={(event) => onChange(event.target.value)}
            placeholder="00000000-0000-0000-0000-000000000000"
            disabled={disabled}
            className="font-mono text-xs"
          />
        </div>
      )}
    </div>
  );
}
