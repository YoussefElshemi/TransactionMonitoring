"use client";

import { useCallback, useEffect, useState } from "react";
import { Check, ChevronDown, X } from "lucide-react";

import { Paged, Rule, RuleResource, RuleSet, RuleSetResource } from "@/types";
import { API_BASE, unwrap } from "@/lib/api";
import { shortId } from "@/lib/resources";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { apiFetch } from "@/lib/http";
import { mapBounded } from "@/lib/rateLimit";
import { useTranslation } from "@/context/LocalizationContext";

export function RuleMultiSelect({
  value,
  onChange,
  disabled = false,
}: {
  value: string[];
  onChange: (ruleIds: string[]) => void;
  disabled?: boolean;
}) {
  const t = useTranslation();
  const [open, setOpen] = useState(false);
  const [ruleSets, setRuleSets] = useState<RuleSet[] | null>(null);
  const [ruleSetId, setRuleSetId] = useState<string>("");
  const [rules, setRules] = useState<Rule[] | null>(null);
  const [search, setSearch] = useState("");

  const [names, setNames] = useState<Record<string, string>>({});

  const fetchRuleSets = useCallback(async () => {
    try {
      const response = await apiFetch(`${API_BASE}/rule-sets?PageSize=100`, {
        credentials: "include",
      });
      if (!response.ok) {
        setRuleSets([]);
        return;
      }
      const page = (await response.json()) as Paged<RuleSetResource>;
      setRuleSets(page.data.map((resource) => resource.ruleSet));
    } catch {
      setRuleSets([]);
    }
  }, []);

  const fetchRules = useCallback(async (setId: string, term: string) => {
    if (!setId) {
      setRules(null);
      return;
    }
    try {
      const response = await apiFetch(
        `${API_BASE}/rule-sets/${setId}/rules?PageSize=100&FriendlyName=${encodeURIComponent(term)}`,
        { credentials: "include" }
      );
      if (!response.ok) {
        setRules([]);
        return;
      }
      const page = (await response.json()) as Paged<RuleResource>;
      const listed = page.data.map((resource) => resource.rule);
      setRules(listed);
      setNames((current) => ({
        ...current,
        ...Object.fromEntries(
          listed.map((rule) => [rule.ruleId, rule.friendlyName])
        ),
      }));
    } catch {
      setRules([]);
    }
  }, []);

  useEffect(() => {
    if (open && ruleSets === null) fetchRuleSets();
  }, [open, ruleSets, fetchRuleSets]);

  useEffect(() => {
    const timer = setTimeout(
      () => fetchRules(ruleSetId, search),
      search ? 300 : 0
    );
    return () => clearTimeout(timer);
  }, [ruleSetId, search, fetchRules]);

  useEffect(() => {
    const unknown = value.filter((ruleId) => !names[ruleId]);
    if (unknown.length === 0) return;

    let cancelled = false;

    mapBounded(
      unknown,
      async (ruleId) => {
        try {
          const response = await apiFetch(`${API_BASE}/rules/${ruleId}`, {
            credentials: "include",
          });
          if (!response.ok) return null;
          const { rule } = await unwrap<RuleResource>(response);
          return [ruleId, rule.friendlyName] as const;
        } catch {
          return null;
        }
      }
    ).then((resolved) => {
      if (cancelled) return;
      const found = resolved.filter(
        (entry): entry is readonly [string, string] => entry !== null
      );
      if (found.length > 0) {
        setNames((current) => ({ ...current, ...Object.fromEntries(found) }));
      }
    });

    return () => {
      cancelled = true;
    };
  }, [value, names]);

  const toggle = (ruleId: string) => {
    onChange(
      value.includes(ruleId)
        ? value.filter((id) => id !== ruleId)
        : [...value, ruleId]
    );
  };

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-1.5">
        {value.map((ruleId) => (
          <span
            key={ruleId}
            className="flex items-center gap-1 rounded-full border border-slate-200 bg-slate-50 py-0.5 pl-2.5 pr-1 text-xs text-slate-700"
          >
            {names[ruleId] ?? shortId(ruleId)}
            {!disabled && (
              <button
                type="button"
                onClick={() => toggle(ruleId)}
                className="rounded-full p-0.5 text-slate-400 hover:bg-slate-200 hover:text-slate-700"
                aria-label={`Remove ${names[ruleId] ?? shortId(ruleId)}`}
              >
                <X className="size-3" />
              </button>
            )}
          </span>
        ))}
        {value.length === 0 && (
          <span className="text-sm text-slate-500">
            {t("queues.anyRuleNotConstraining")}
          </span>
        )}
      </div>

      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5"
            disabled={disabled}
          >
            {t("queues.chooseRules")}
            <ChevronDown className="size-4" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-96 p-3" align="start">
          <p className="mb-2 text-xs text-slate-500">
            A case matches if <span className="font-medium">any</span> of these
            rules fired on it. Ticking more widens the queue.
          </p>

          <Select
            value={ruleSetId}
            onValueChange={(next) => {
              setRuleSetId(next);
              setSearch("");
            }}
          >
            <SelectTrigger className="w-full">
              <SelectValue placeholder={t("queues.pickARuleSet")} />
            </SelectTrigger>
            <SelectContent>
              {(ruleSets ?? []).map((ruleSet) => (
                <SelectItem key={ruleSet.ruleSetId} value={ruleSet.ruleSetId}>
                  {ruleSet.friendlyName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          {ruleSetId && (
            <>
              <Input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder={t("queues.searchRulesInThisSet")}
                className="mt-2"
              />
              <ul className="mt-2 max-h-56 space-y-0.5 overflow-y-auto">
                {rules === null ? (
                  Array.from({ length: 4 }).map((_, index) => (
                    <li key={index} className="px-1 py-1.5">
                      <Skeleton className="h-4 w-full" />
                    </li>
                  ))
                ) : rules.length === 0 ? (
                  <li className="px-1 py-6 text-center text-sm text-slate-500">
                    No rules in this set
                    {search ? ` matching "${search}"` : ""}
                  </li>
                ) : (
                  rules.map((rule) => {
                    const picked = value.includes(rule.ruleId);
                    return (
                      <li key={rule.ruleId}>
                        <button
                          type="button"
                          onClick={() => toggle(rule.ruleId)}
                          className="flex w-full items-center gap-2 rounded px-1.5 py-1.5 text-left text-sm hover:bg-slate-100"
                        >
                          <span
                            className={
                              picked
                                ? "flex size-4 items-center justify-center rounded border border-brand-800 bg-brand-800 text-white"
                                : "size-4 rounded border border-slate-300"
                            }
                          >
                            {picked && <Check className="size-3" />}
                          </span>
                          <span className="min-w-0 flex-1 truncate text-slate-900">
                            {rule.friendlyName}
                          </span>
                          {!rule.isEnabled && (
                            <span className="font-mono text-[10px] uppercase tracking-widest text-slate-400">
                              disabled
                            </span>
                          )}
                        </button>
                      </li>
                    );
                  })
                )}
              </ul>
            </>
          )}
        </PopoverContent>
      </Popover>
    </div>
  );
}
