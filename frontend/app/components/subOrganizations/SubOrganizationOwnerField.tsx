"use client";

import { SUB_ORGANIZATION_OWNERSHIP_UNREADABLE } from "@/lib/permissions";
import { useSubOrganizations } from "@/lib/roles";
import { usePermissions } from "@/context/PermissionsContext";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTranslation } from "@/context/LocalizationContext";

export const OWNER_UNCHANGED = "__unchanged__";
export const OWNER_ORGANIZATION = "__organization__";

/** `undefined` leaves both fields off the body; `null` clears; a string sets. */
export type SubOrganizationOwner = string | null | undefined;

export const ownerFromSelection = (value: string): SubOrganizationOwner => {
  if (value === OWNER_UNCHANGED) return undefined;
  if (value === OWNER_ORGANIZATION) return null;
  return value;
};

export const selectionFromOwner = (owner: SubOrganizationOwner): string => {
  if (owner === undefined) return OWNER_UNCHANGED;
  if (owner === null) return OWNER_ORGANIZATION;
  return owner;
};

export const ownerBody = (owner: SubOrganizationOwner): Record<string, unknown> => {
  if (owner === undefined) return {};
  if (owner === null) return { clearSubOrganization: true };
  return { subOrganizationId: owner };
};

export function SubOrganizationOwnerField({
  organizationId,
  value,
  onChange,
  disabled = false,
  allowUnchanged = true,
  label = "Business unit",
}: {
  organizationId: string;
  value: SubOrganizationOwner;
  onChange: (owner: SubOrganizationOwner) => void;
  disabled?: boolean;
  allowUnchanged?: boolean;
  label?: string;
}) {
  const t = useTranslation();
  const { holds } = usePermissions();
  const units = useSubOrganizations(
    organizationId,
    holds("sub-organizations:read")
  );

  if (!units.unavailable && units.data.length === 0) return null;

  return (
    <div className="space-y-1.5">
      <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {label}
      </label>

      {units.unavailable ? (
        <p className="text-sm text-slate-500">
          The business units could not be read, so ownership cannot be set here.
          That needs <span className="font-mono">sub-organizations:read</span>.
        </p>
      ) : (
        <>
          <Select
            value={selectionFromOwner(value)}
            onValueChange={(next) => onChange(ownerFromSelection(next))}
            disabled={disabled || units.loading}
          >
            <SelectTrigger className="w-64">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {allowUnchanged && (
                <SelectItem value={OWNER_UNCHANGED}>{t("subOrganizations.leaveAsItIs")}</SelectItem>
              )}
              <SelectItem value={OWNER_ORGANIZATION}>
                {t("subOrganizations.theWholeOrganisation")}
              </SelectItem>
              {units.data.map((unit) => (
                <SelectItem
                  key={unit.subOrganizationId}
                  value={unit.subOrganizationId}
                >
                  {unit.name}
                  {!unit.isEnabled && (
                    <span className="ml-2 font-mono text-[10px] uppercase tracking-wider text-slate-400">
                      disabled
                    </span>
                  )}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {units.truncated && (
            <p className="text-xs text-slate-500">
              Showing the first {units.data.length}
              {units.totalCount === null ? "" : ` of ${units.totalCount}`}{" "}
              units — one page is all the API will serve.
            </p>
          )}
          <p className="text-xs text-slate-500">
            {allowUnchanged ? SUB_ORGANIZATION_OWNERSHIP_UNREADABLE : null}
          </p>
        </>
      )}
    </div>
  );
}
