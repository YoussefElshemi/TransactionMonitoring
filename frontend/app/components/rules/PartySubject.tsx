"use client";

import {
  PartyMappingEntry,
  PartyNodeDto,
  PartyRole,
  PartyRuleAttribute,
  PARTY_ROLES,
  PARTY_RULE_ATTRIBUTES,
} from "@/types";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { HintTip } from "@/components/rules/LeafParts";
import { PARTY_ATTRIBUTE_HINT, PARTY_ATTRIBUTE_LABEL } from "@/lib/parties";
import { declaredRoles, isManyParty } from "@/lib/partyMapping";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function PartySubject({
  party,
  mappings,
  disabled,
  tone = "default",
  onChange,
}: {
  party: PartyNodeDto;
  mappings: PartyMappingEntry[] | null;
  disabled: boolean;
  tone?: "default" | "warning" | "critical";
  onChange: (party: PartyNodeDto) => void;
}) {
  const t = useTranslation();
  const declared = declaredRoles(mappings);
  const roles: string[] = [
    ...new Set<string>([
      ...(party.role ? [String(party.role)] : []),
      ...declared,
    ]),
  ];
  const offered = roles.length > 0 ? roles : (PARTY_ROLES as string[]);

  const entry = mappings?.find((mapping) => mapping.role === party.role);

  return (
    <div className="flex min-w-0 flex-wrap items-center gap-1.5">
      <Select
        value={String(party.role ?? "")}
        onValueChange={(value) =>
          onChange({ ...party, role: value as PartyRole })
        }
        disabled={disabled}
      >
        <SelectTrigger
          className={cn(
            "w-auto min-w-[9rem] shrink-0",
            tone === "critical" && "border-critical-500",
            tone === "warning" && "border-attention-500"
          )}
        >
          <SelectValue placeholder={t("rules.role")} />
        </SelectTrigger>
        <SelectContent>
          {offered.map((role) => (
            <SelectItem
              key={role}
              value={role}
              // Outside the mirrored text: Radix paints an option's own content
              // into the closed trigger, so this used to read as part of the
              // selected value — "Remitter not declared" crammed into a box
              // sized for "Remitter".
              hint={
                mappings && !declared.includes(role as PartyRole) ? (
                  <span className="ml-2 text-xs text-attention-700">
                    not declared
                  </span>
                ) : null
              }
            >
              {role}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <span aria-hidden className="font-mono text-xs text-slate-300">
        ·
      </span>

      <Select
        value={String(party.attribute ?? "")}
        onValueChange={(value) =>
          onChange({ ...party, attribute: value as PartyRuleAttribute })
        }
        disabled={disabled}
      >
        <SelectTrigger className="w-auto min-w-[12rem] shrink-0">
          <SelectValue placeholder={t("rules.whatAboutThem")} />
        </SelectTrigger>
        <SelectContent>
          {PARTY_RULE_ATTRIBUTES.map((attribute) => (
            <SelectItem key={attribute} value={attribute}>
              {PARTY_ATTRIBUTE_LABEL[attribute]}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {party.attribute &&
        PARTY_ATTRIBUTE_HINT[party.attribute as PartyRuleAttribute] && (
          <HintTip
            label={`What ${PARTY_ATTRIBUTE_LABEL[party.attribute as PartyRuleAttribute]} means`}
          >
            {PARTY_ATTRIBUTE_HINT[party.attribute as PartyRuleAttribute]}
          </HintTip>
        )}

      {entry && (
        <span className="inline-flex shrink-0 items-center gap-1 rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-wide text-slate-500">
          Number
          {isManyParty(entry) && (
            <span
              className="text-brand-800"
              title={t("rules.theMappingDeclaresThisRole")}
            >
              many
            </span>
          )}
        </span>
      )}
    </div>
  );
}
