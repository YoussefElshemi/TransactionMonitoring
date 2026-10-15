import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { JsonEditor } from "@/components/JsonEditor";
import { toast } from "sonner";

import {
  PartyMappingEntry,
  RuleAction,
  RuleBodyDto,
  RuleBuilderProps,
  RuleCondition,
  RuleMatchPolarity,
  RuleQuantifier,
  RuleSeverity,
  RuleTestResponse,
  SchemaField,
  SchemaViolation,
  VelocityMetric,
  VelocityThreshold
} from "@/types";
import {
  AlertTriangle,
  ChevronDown,
  ChevronRight,
  Copy,
  FlaskConical,
  GripVertical,
  Plus,
  RotateCcw,
  RotateCw,
  Save,
  Sparkles,
  Trash2,
} from "lucide-react";
import { useAuth } from "@/context/AuthContext";
import { usePermissions } from "@/context/PermissionsContext";
import { HoldPolicy, HoldPolicyFields } from "@/components/HoldPolicyFields";
import {
  EMPTY_RULE_ALERT_POLICY,
  RuleAlertPolicy,
  RuleAlertPolicyFields,
} from "@/components/RuleAlertPolicyFields";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  MatchPolarityBadge,
  POLARITY_OPTIONS,
  POLARITY_OPTION_LABEL,
} from "@/components/MatchPolarityBadge";
import { cn } from "@/lib/utils";
import {
  API_BASE,
  reportApiError,
  unwrap,
  violationsByField,
} from "@/lib/api";
import { generalisePath } from "@/lib/payloadPath";
import { loginHref } from "@/lib/authRedirect";
import { SchemaFieldCombobox } from "@/components/SchemaFieldCombobox";
import { ConditionValue } from "@/components/rules/ConditionValue";
import { ExpressionBuilder } from "@/components/rules/ExpressionBuilder";
import {
  HintTip,
  LeafMessage,
  LeafMessages,
  SubjectKind,
  SubjectKindToggle,
  TypeChip,
  ZoneDivider,
} from "@/components/rules/LeafParts";
import { PartySubject } from "@/components/rules/PartySubject";
import { VelocitySubject } from "@/components/rules/VelocitySubject";
import { TooltipProvider } from "@/components/ui/tooltip";
import {
  closestSchemaPath,
  resolveSchemaPath,
  schemaPaths,
} from "@/lib/transactionSchema";
import { hasWildcard, isAddressable } from "@/lib/payloadPath";
import { describeExpression, validateExpression } from "@/lib/ruleExpression";
import {
  EXPRESSION_RESULT_TYPE,
  OPERATOR,
  conditionsFor,
  shapeOf,
} from "@/lib/ruleOperators";
import { useTransactionSchema } from "@/lib/useTransactionSchema";
import { usePartyMapping } from "@/lib/usePartyMapping";
import { useVelocityProfile } from "@/lib/useVelocityProfile";
import { PARTY_ATTRIBUTE_TYPE, partyLeafName } from "@/lib/parties";
import { entryForRole, isManyParty } from "@/lib/partyMapping";
import {
  VELOCITY_RESULT_TYPE,
  declares,
  newVelocityCondition,
  validateVelocity,
  velocityLeafName,
} from "@/lib/velocityProfile";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const conditionDisplayMap: Record<string, string> = Object.fromEntries(
  Object.values(OPERATOR).map((spec) => [spec.condition, spec.label])
);

const ACTION_OPTIONS: RuleAction[] = ["Allow", "Flag", "Review", "Block"];
const SEVERITY_OPTIONS: RuleSeverity[] = [
  "Info",
  "Low",
  "Medium",
  "High",
  "Critical",
];

type ContainerKey = "and" | "or" | "not";
type GroupKey = "and" | "or";

const GROUP_LABELS: Record<GroupKey, string> = {
  and: "ALL of",
  or: "ANY of",
};

const CONTAINER_STYLES: Record<
  GroupKey,
  { border: string; badge: string; ring: string }
> = {
  and: {
    border: "border-l-brand-800",
    badge: "bg-brand-100 text-brand-900",
    ring: "focus-within:ring-brand-200",
  },
  or: {
    border: "border-l-brand-400",
    badge: "bg-brand-50 text-brand-800",
    ring: "focus-within:ring-brand-200",
  },
};

const AND_OR_LIMIT = 10;

const REMOVED = Symbol("removed");

const childKeyOf = (rule: RuleBodyDto): ContainerKey | null =>
  rule.and ? "and" : rule.or ? "or" : rule.not ? "not" : null;

const samePath = (a: number[], b: number[]): boolean =>
  a.length === b.length && a.every((v, i) => v === b[i]);

const isPrefixPath = (a: number[], b: number[]): boolean =>
  a.length <= b.length && a.every((v, i) => v === b[i]);

const getNode = (rules: RuleBodyDto[], path: number[]): RuleBodyDto | undefined => {
  let list = rules;
  let node: RuleBodyDto | undefined;
  for (const idx of path) {
    node = list[idx];
    if (!node) return undefined;
    list = node.and || node.or || node.not || [];
  }
  return node;
};

const childrenAt = (rules: RuleBodyDto[], parentPath: number[]): RuleBodyDto[] => {
  if (parentPath.length === 0) return rules;
  const node = getNode(rules, parentPath);
  if (!node) return [];
  const key = childKeyOf(node);
  return (key && node[key]) || [];
};

const updateNode = (
  rules: RuleBodyDto[],
  path: number[],
  updates: Partial<RuleBodyDto>
): RuleBodyDto[] => {
  const [i, ...rest] = path;
  return rules.map((rule, idx) => {
    if (idx !== i) return rule;
    if (rest.length === 0) return { ...rule, ...updates };
    const key = childKeyOf(rule);
    if (!key) return rule;
    return { ...rule, [key]: updateNode(rule[key] || [], rest, updates) };
  });
};

const removeNode = (
  rules: RuleBodyDto[],
  path: number[]
): { rules: RuleBodyDto[]; node?: RuleBodyDto } => {
  let removed: RuleBodyDto | undefined;
  const helper = (list: RuleBodyDto[], p: number[]): RuleBodyDto[] => {
    const [i, ...rest] = p;
    return list
      .map((rule, idx) => {
        if (idx !== i) return rule;
        if (rest.length === 0) {
          removed = rule;
          return REMOVED as unknown as RuleBodyDto;
        }
        const key = childKeyOf(rule);
        if (!key) return rule;
        return { ...rule, [key]: helper(rule[key] || [], rest) };
      })
      .filter((rule) => (rule as unknown) !== REMOVED);
  };
  return { rules: helper(rules, path), node: removed };
};

const insertNode = (
  rules: RuleBodyDto[],
  parentPath: number[],
  index: number,
  node: RuleBodyDto
): RuleBodyDto[] => {
  if (parentPath.length === 0) {
    const copy = [...rules];
    copy.splice(index, 0, node);
    return copy;
  }
  const [i, ...rest] = parentPath;
  return rules.map((rule, idx) => {
    if (idx !== i) return rule;
    const key = childKeyOf(rule);
    if (!key) return rule;
    return { ...rule, [key]: insertNode(rule[key] || [], rest, index, node) };
  });
};

const cloneRule = (rule: RuleBodyDto): RuleBodyDto =>
  JSON.parse(JSON.stringify(rule));

const transformNode = (
  rules: RuleBodyDto[],
  path: number[],
  fn: (node: RuleBodyDto) => RuleBodyDto
): RuleBodyDto[] => {
  const [i, ...rest] = path;
  return rules.map((rule, idx) => {
    if (idx !== i) return rule;
    if (rest.length === 0) return fn(rule);
    const key = childKeyOf(rule);
    if (!key) return rule;
    return { ...rule, [key]: transformNode(rule[key] || [], rest, fn) };
  });
};

const toggleNegate = (rules: RuleBodyDto[], path: number[]): RuleBodyDto[] =>
  transformNode(rules, path, (node) =>
    childKeyOf(node) === "not"
      ? node.not?.[0] ?? { condition: null, field: null, value: null }
      : { not: [node] }
  );

const switchGroupType = (
  rules: RuleBodyDto[],
  path: number[],
  next: GroupKey
): RuleBodyDto[] =>
  transformNode(rules, path, (node) => {
    const key = childKeyOf(node);
    if (key !== "and" && key !== "or") return node;
    const children = node[key] || [];
    const { and: _a, or: _o, ...rest } = node; // eslint-disable-line @typescript-eslint/no-unused-vars
    return { ...rest, [next]: children };
  });

const describeRule = (rule: RuleBodyDto): string => {
  const key = childKeyOf(rule);
  if (key) {
    const children = rule[key] || [];
    if (key === "not") {
      return `NOT ( ${children.length ? describeRule(children[0]) : "…"} )`;
    }
    if (children.length === 0) return "( empty group )";
    if (children.length === 1) return describeRule(children[0]);
    const joiner = key === "and" ? " AND " : " OR ";
    return `( ${children.map(describeRule).join(joiner)} )`;
  }
  const subject = rule.velocity
    ? velocityLeafName(rule.velocity)
    : rule.party
      ? partyLeafName(rule.party)
      : rule.expression
        ? describeExpression(rule.expression) || "expression"
        : rule.field || "field";

  const quantified = rule.quantifier
    ? `${String(rule.quantifier).toLowerCase()} of ${subject}`
    : subject;

  if (rule.condition === "IsNull") return `${quantified} is null`;

  const sym = rule.condition ? (conditionDisplayMap[rule.condition] ?? rule.condition) : "?";
  const val =
    rule.value === null || rule.value === undefined || rule.value === ""
      ? "…"
      : Array.isArray(rule.value)
        ?
          rule.condition === "Between" && rule.value.length === 2
          ? `${JSON.stringify(rule.value[0])} and ${JSON.stringify(rule.value[1])}`
          : JSON.stringify(rule.value)
        : rule.json
          ? JSON.stringify(rule.value)
          : String(rule.value);

  return `${quantified} ${sym} ${val}`;
};

interface PreviewLine {
  depth: number;
  text: string;
  isGroup: boolean;
}

const previewLines = (
  rule: RuleBodyDto,
  depth = 0,
  lines: PreviewLine[] = []
): PreviewLine[] => {
  const key = childKeyOf(rule);

  if (key === "not") {
    const inner = rule.not?.[0];
    if (!inner) {
      lines.push({ depth, text: "NOT …", isGroup: true });
      return lines;
    }
    if (childKeyOf(inner) === null) {
      lines.push({ depth, text: `NOT ${describeRule(inner)}`, isGroup: false });
      return lines;
    }
    lines.push({ depth, text: "NOT", isGroup: true });
    previewLines(inner, depth + 1, lines);
    return lines;
  }

  if (key === "and" || key === "or") {
    const children = rule[key] ?? [];
    if (children.length === 0) {
      lines.push({ depth, text: `${GROUP_LABELS[key]} …`, isGroup: true });
      return lines;
    }
    lines.push({ depth, text: GROUP_LABELS[key], isGroup: true });
    for (const child of children) previewLines(child, depth + 1, lines);
    return lines;
  }

  lines.push({ depth, text: describeRule(rule), isGroup: false });
  return lines;
};

type DropInfo = { parentPath: number[]; index: number };

const sameDrop = (a: DropInfo | null, b: DropInfo): boolean =>
  !!a && a.index === b.index && samePath(a.parentPath, b.parentPath);

const isValidDropTarget = (from: number[], info: DropInfo): boolean => {
  if (isPrefixPath(from, info.parentPath)) return false;
  const fromParent = from.slice(0, -1);
  const fromIndex = from[from.length - 1];
  if (
    samePath(fromParent, info.parentPath) &&
    (info.index === fromIndex || info.index === fromIndex + 1)
  ) {
    return false;
  }
  return true;
};

interface NodeCtx {
  readOnly: boolean;
  violationsFor: (subject: string) => string[] | undefined;
  schemaFields: SchemaField[] | null;
  partyMappings: PartyMappingEntry[] | null;
  partyOffered: boolean;
  partyMappingAbsent: boolean;
  velocityMetrics: VelocityMetric[] | null;
  velocityThresholds: VelocityThreshold[] | null;
  velocityOffered: boolean;
  velocityRefused: boolean;
  velocityProfileAbsent: boolean;
  dragPath: number[] | null;
  activeDrop: DropInfo | null;
  setDragPath: (p: number[] | null) => void;
  setActiveDrop: (d: DropInfo | null) => void;
  onDropAt: (info: DropInfo) => void;
  addBasicRule: (parentPath: number[]) => void;
  addContainer: (type: GroupKey, parentPath: number[]) => void;
  removeNode: (path: number[]) => void;
  duplicateNode: (path: number[]) => void;
  updateNode: (path: number[], updates: Partial<RuleBodyDto>) => void;
  toggleNegate: (path: number[]) => void;
  switchGroupType: (path: number[], next: GroupKey) => void;
}

// Always rendered (never returns null) so that starting/ending a drag never
// adds or removes DOM nodes around the card being dragged — inserting siblings
// during `dragstart` makes Chrome cancel the drag.
const DropZone: React.FC<{ info: DropInfo; ctx: NodeCtx }> = ({ info, ctx }) => {
  const dragging = !ctx.readOnly && ctx.dragPath !== null;
  const valid = dragging && !!ctx.dragPath && isValidDropTarget(ctx.dragPath, info);
  const active = valid && sameDrop(ctx.activeDrop, info);
  return (
    <div
      onDragOver={
        dragging
          ? (e) => {
              // Not calling preventDefault marks this zone as non-droppable
              // (browser shows a "no-drop" cursor) and we skip the highlight.
              if (!valid) {
                e.dataTransfer.dropEffect = "none";
                return;
              }
              e.preventDefault();
              e.dataTransfer.dropEffect = "move";
              if (!sameDrop(ctx.activeDrop, info)) ctx.setActiveDrop(info);
            }
          : undefined
      }
      onDrop={
        dragging && valid
          ? (e) => {
              e.preventDefault();
              ctx.onDropAt(info);
            }
          : undefined
      }
      className={cn(
        "rounded-full transition-all duration-100",
        active
          ? "h-7 bg-brand-500/80"
          : dragging && valid
            ? "h-4 bg-transparent hover:bg-slate-200/70"
            : dragging
              ? "h-4 bg-transparent"
              : "h-1.5 bg-transparent"
      )}
    />
  );
};

const RuleNode: React.FC<{ rule: RuleBodyDto; path: number[]; ctx: NodeCtx }> = ({
  rule,
  path,
  ctx,
}) => {
  const t = useTranslation();
  const cardRef = useRef<HTMLDivElement>(null);
  const [collapsed, setCollapsed] = useState(false);
  // The whole card is always draggable, but a drag only "counts" when it was
  // armed by pressing the grip handle. Using a ref (not state) means no
  // re-render happens between mousedown and dragstart, so it fires reliably for
  // every node — not just the first child of a group.
  const dragArmed = useRef(false);
  const negated = childKeyOf(rule) === "not";
  const effective: RuleBodyDto = negated
    ? rule.not?.[0] ?? { condition: null, field: null, value: null }
    : rule;
  const effectivePath = negated ? [...path, 0] : path;
  const innerKey = childKeyOf(effective);
  const groupKey: GroupKey | null =
    innerKey === "and" || innerKey === "or" ? innerKey : null;
  const isDragging = ctx.dragPath !== null && samePath(ctx.dragPath, path);

  const handleDragStart = (e: React.DragEvent) => {
    if (ctx.readOnly || !dragArmed.current) {
      e.preventDefault();
      return;
    }
    e.stopPropagation();
    e.dataTransfer.effectAllowed = "move";
    e.dataTransfer.setData("text/plain", path.join("."));
    if (cardRef.current) e.dataTransfer.setDragImage(cardRef.current, 16, 16);
    // Defer the state update so `dragstart` finishes before React re-renders;
    // a synchronous re-render here cancels the native drag in Chrome.
    requestAnimationFrame(() => ctx.setDragPath(path));
  };
  const handleDragEnd = () => {
    dragArmed.current = false;
    ctx.setDragPath(null);
    ctx.setActiveDrop(null);
  };

  const dragProps = ctx.readOnly
    ? {}
    : {
        draggable: true,
        onDragStart: handleDragStart,
        onDragEnd: handleDragEnd,
      };

  const arm = () => {
    dragArmed.current = true;
  };
  const disarm = () => {
    dragArmed.current = false;
  };

  const dragHandle = !ctx.readOnly && (
    <span
      role="button"
      aria-label={t("ruleBuilder.dragToMove")}
      onMouseDown={arm}
      onMouseUp={disarm}
      onTouchStart={arm}
      onTouchEnd={disarm}
      className="shrink-0 cursor-grab select-none rounded p-0.5 text-slate-300 hover:text-slate-500 active:cursor-grabbing"
    >
      <GripVertical className="size-4" />
    </span>
  );

  const negateControl = ctx.readOnly ? (
    negated ? (
      <span className="shrink-0 rounded bg-attention-100 px-1.5 py-0.5 font-mono text-[11px] font-medium uppercase tracking-wide text-attention-700">
        {t("ruleBuilder.not")}
      </span>
    ) : null
  ) : (
    <button
      type="button"
      onClick={() => ctx.toggleNegate(path)}
      title={negated ? t("ruleBuilder.removeNotNegation") : t("ruleBuilder.negateWrapNot")}
      className={cn(
        "shrink-0 rounded px-1.5 py-0.5 font-mono text-[11px] font-medium uppercase tracking-wide transition-colors",
        negated
          ? "bg-attention-100 text-attention-700 ring-1 ring-attention-200"
          : "text-slate-300 hover:bg-slate-100 hover:text-slate-500"
      )}
    >
      {t("ruleBuilder.not")}
    </button>
  );

  const nodeActions = !ctx.readOnly && (
    <>
      <Button
        variant="ghost"
        size="icon"
        className="h-7 w-7 text-slate-400 hover:text-slate-600"
        onClick={() => ctx.duplicateNode(path)}
        title={t("ruleBuilder.duplicate")}
      >
        <Copy className="size-4" />
      </Button>
      <Button
        variant="ghost"
        size="icon"
        className="h-7 w-7 text-critical-500 hover:text-critical-700"
        onClick={() => ctx.removeNode(path)}
        title={t("common.delete")}
      >
        <Trash2 className="size-4" />
      </Button>
    </>
  );

  if (groupKey) {
    const children = effective[groupKey] || [];
    const style = CONTAINER_STYLES[groupKey];
    const canAdd = children.length < AND_OR_LIMIT;

    return (
      <div
        ref={cardRef}
        {...dragProps}
        className={cn(
          "relative rounded-lg border border-slate-200 border-l-4 bg-slate-50/70 p-3 transition-all",
          style.border,
          negated && "ring-1 ring-attention-200",
          isDragging && "opacity-40"
        )}
      >
        {!ctx.readOnly && (
          <div className="absolute right-2 top-2 flex items-center gap-0.5">
            {nodeActions}
          </div>
        )}

        <div
          className={cn(
            "flex flex-wrap items-center gap-2",
            !ctx.readOnly && "pr-[4.5rem]"
          )}
        >
          {dragHandle}
          <button
            type="button"
            onClick={() => setCollapsed((c) => !c)}
            className="shrink-0 rounded p-0.5 text-slate-400 hover:text-slate-600"
            aria-label={collapsed ? t("ruleBuilder.expandGroup") : t("ruleBuilder.collapseGroup")}
          >
            {collapsed ? (
              <ChevronRight className="size-4" />
            ) : (
              <ChevronDown className="size-4" />
            )}
          </button>
          {negateControl}
          {ctx.readOnly ? (
            <span
              className={cn(
                "rounded px-2 py-0.5 font-mono text-xs font-medium uppercase tracking-wide",
                style.badge
              )}
            >
              {GROUP_LABELS[groupKey]}
            </span>
          ) : (
            <Select
              value={groupKey}
              onValueChange={(v) => ctx.switchGroupType(effectivePath, v as GroupKey)}
            >
              <SelectTrigger className="h-7 w-28 font-mono text-xs">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="and">{GROUP_LABELS.and}</SelectItem>
                <SelectItem value="or">{GROUP_LABELS.or}</SelectItem>
              </SelectContent>
            </Select>
          )}
          <Badge variant="secondary" className="text-[10px]">
            {children.length}
          </Badge>

          {!ctx.readOnly && (
            <div className="ml-auto flex items-center gap-1">
              <Button
                variant="outline"
                size="sm"
                disabled={!canAdd}
                onClick={() => ctx.addBasicRule(effectivePath)}
                className="h-7 gap-1 text-xs"
              >
                <Plus className="size-3" /> {t("ruleBuilder.condition")}
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={!canAdd}
                onClick={() => ctx.addContainer("and", effectivePath)}
                className="h-7 text-xs"
              >
                + Group
              </Button>
            </div>
          )}
        </div>

        {!collapsed && (
          <div className="mt-2 pl-3">
            <DropZone info={{ parentPath: effectivePath, index: 0 }} ctx={ctx} />
            {children.map((child, i) => (
              <React.Fragment key={i}>
                <RuleNode rule={child} path={[...effectivePath, i]} ctx={ctx} />
                <DropZone info={{ parentPath: effectivePath, index: i + 1 }} ctx={ctx} />
              </React.Fragment>
            ))}
            {children.length === 0 && (
              <div className="rounded-md border border-dashed border-slate-300 bg-card/60 px-3 py-2 text-xs text-slate-400">
                {t("ruleBuilder.emptyGroupAddACondition")}
              </div>
            )}
          </div>
        )}
      </div>
    );
  }

  const isExpressionLeaf = effective.expression != null;
  const isPartyLeaf = effective.party != null;
  const isVelocityLeaf = effective.velocity != null;
  const fieldPath = effective.field ?? "";

  const subject = isVelocityLeaf
    ? velocityLeafName(effective.velocity!)
    : isPartyLeaf
      ? partyLeafName(effective.party!)
      : isExpressionLeaf
        ? describeExpression(effective.expression)
        : fieldPath;

  const violations = subject ? ctx.violationsFor(subject) : undefined;

  const isPathLeaf = !isExpressionLeaf && !isPartyLeaf && !isVelocityLeaf;

  const panelSubject = isExpressionLeaf || isVelocityLeaf;

  const resolution =
    ctx.schemaFields && fieldPath && isPathLeaf
      ? resolveSchemaPath(ctx.schemaFields, fieldPath)
      : null;

  const unknownField = Boolean(
    ctx.schemaFields && fieldPath && isPathLeaf && !resolution
  );
  const suggestion =
    unknownField && ctx.schemaFields
      ? closestSchemaPath(ctx.schemaFields, fieldPath)
      : null;

  const malformedPath =
    isPathLeaf && fieldPath.length > 0 && !isAddressable(fieldPath);

  const roleNamed = isPartyLeaf
    ? String(effective.party!.role ?? "")
    : isVelocityLeaf
      ? String(effective.velocity!.role ?? "")
      : "";
  const partyEntry = roleNamed
    ? entryForRole(ctx.partyMappings, roleNamed)
    : null;
  const undeclaredRole = Boolean(
    (isPartyLeaf || isVelocityLeaf) &&
      ctx.partyMappings &&
      roleNamed &&
      !partyEntry
  );

  const declaredMetricNames = ctx.velocityMetrics
    ? ctx.velocityMetrics.map((metric) => metric.name)
    : null;
  const undeclaredMetric = Boolean(
    isVelocityLeaf &&
      declaredMetricNames &&
      effective.velocity!.metric &&
      !declares(declaredMetricNames, effective.velocity!.metric)
  );

  const wildcard = isPathLeaf && hasWildcard(fieldPath);
  const manyParties = Boolean(partyEntry && isManyParty(partyEntry));
  const resolvesMany = wildcard || manyParties;

  const partyRequirementKnown =
    isPartyLeaf || isVelocityLeaf ? Boolean(partyEntry) : true;

  const missingQuantifier =
    partyRequirementKnown && resolvesMany && !effective.quantifier;
  const strayQuantifier =
    Boolean(effective.quantifier) &&
    partyRequirementKnown &&
    !resolvesMany &&
    (isExpressionLeaf || isPathLeaf || isPartyLeaf || isVelocityLeaf);

  const subjectType = isPartyLeaf
    ? PARTY_ATTRIBUTE_TYPE
    : isVelocityLeaf
      ? VELOCITY_RESULT_TYPE
      : isExpressionLeaf
        ? EXPRESSION_RESULT_TYPE
        : (resolution?.type ?? null);
  const operators = conditionsFor(subjectType, effective.condition);
  const operator = effective.condition
    ? OPERATOR[effective.condition]
    : undefined;
  const shape = shapeOf(effective.condition);

  const expressionProblems = isExpressionLeaf
    ? validateExpression(effective.expression)
    : [];

  const velocityProblems = isVelocityLeaf
    ? validateVelocity(effective.velocity, {
        metrics: declaredMetricNames,
        thresholds: ctx.velocityThresholds
          ? ctx.velocityThresholds.map((threshold) => threshold.name)
          : null,
      })
    : [];

  const setLeafKind = (next: SubjectKind) => {
    if (next === "expression") {
      ctx.updateNode(effectivePath, {
        field: null,
        party: null,
        velocity: null,
        quantifier: null,
        expression: { function: "DaysSince", arguments: [{ field: "" }] },
      });
      return;
    }

    if (next === "party") {
      const first = ctx.partyMappings?.[0]?.role ?? "Remitter";
      ctx.updateNode(effectivePath, {
        field: null,
        expression: null,
        velocity: null,
        quantifier: null,
        party: { role: first, attribute: "RiskScore" },
      });
      return;
    }

    if (next === "velocity") {
      const first = ctx.partyMappings?.[0]?.role ?? "Remitter";
      ctx.updateNode(effectivePath, {
        field: null,
        expression: null,
        party: null,
        quantifier: null,
        velocity: newVelocityCondition(first),
      });
      return;
    }

    ctx.updateNode(effectivePath, {
      expression: null,
      party: null,
      velocity: null,
      quantifier: null,
      field: "",
    });
  };

  const messages: LeafMessage[] = [];

  if (violations) {
    for (const message of violations) {
      messages.push({
        tone: "critical",
        body: (
          <>
            <span className="font-mono">{subject}</span> {message}
          </>
        ),
      });
    }
  } else {
    if (undeclaredRole) {
      messages.push({
        tone: "attention",
        body: (
          <>
            The party mapping does not declare{" "}
            <span className="font-mono">{roleNamed}</span>, so there is nobody
            for this to read. Saving will be rejected until the role is declared
            or this names one that is.
          </>
        ),
      });
    }

    if ((isPartyLeaf || isVelocityLeaf) && ctx.partyMappingAbsent) {
      messages.push({
        tone: "attention",
        body: isVelocityLeaf ? (
          <>
            This organisation declares no party mapping, so nobody is resolved —
            and a window is keyed to a party in a role, so there is nothing for
            this one to be about. Declare a mapping under My Organization first.
          </>
        ) : (
          <>
            This organisation declares no party mapping, so nobody is resolved
            and a rule naming a party is refused outright. Declare a mapping
            under My Organization first.
          </>
        ),
      });
    }

    if (isVelocityLeaf && ctx.velocityRefused) {
      messages.push({
        tone: "attention",
        body: (
          <>
            A queue matcher cannot ask about velocity. Routing runs after the
            engine has finished with the velocity facts, so this window would
            resolve to nothing and the queue would never match — silently.
            Replace it with a field, party or computed condition.
          </>
        ),
      });
    } else if (isVelocityLeaf && ctx.velocityProfileAbsent) {
      messages.push({
        tone: "attention",
        body: (
          <>
            This organisation declares no velocity profile, so nothing is being
            remembered between payments and there are no observations for this
            window to aggregate. Declare a profile under My Organization first.
          </>
        ),
      });
    }

    for (const problem of velocityProblems) {
      messages.push({ tone: "attention", body: problem });
    }

    if (missingQuantifier) {
      messages.push({
        tone: "attention",
        body: isVelocityLeaf ? (
          <>
            {t("ruleBuilder.theMappingDeclares")} <span className="font-mono">{roleNamed}</span>{" "}
            with a wildcard, so a payload can name several. The window is worked
            out per party and the quantifier applied to the answers — say
            whether any or all of them must match.
          </>
        ) : isPartyLeaf ? (
          <>
            {t("ruleBuilder.theMappingDeclares")} <span className="font-mono">{roleNamed}</span>{" "}
            with a wildcard, so a payload can name several — say whether any or
            all of them must match.
          </>
        ) : (
          <>
            <span className="font-mono">{fieldPath}</span> resolves to many
            values — say whether any or all of them must match.
          </>
        ),
      });
    }

    if (strayQuantifier) {
      messages.push({
        tone: "attention",
        body: isPartyLeaf || isVelocityLeaf ? (
          <>
            {t("ruleBuilder.theMappingDeclares")} <span className="font-mono">{roleNamed}</span>{" "}
            as exactly one party per payload, so there is nothing for a
            quantifier to choose between.{" "}
            <button
              type="button"
              disabled={ctx.readOnly}
              onClick={() => ctx.updateNode(effectivePath, { quantifier: null })}
              className="underline underline-offset-2 hover:text-attention-500"
            >
              {t("ruleBuilder.removeIt")}
            </button>
            .
          </>
        ) : (
          <>
            A quantifier only means something on a path carrying{" "}
            <span className="font-mono">[*]</span>.{" "}
            <button
              type="button"
              disabled={ctx.readOnly}
              onClick={() => ctx.updateNode(effectivePath, { quantifier: null })}
              className="underline underline-offset-2 hover:text-attention-500"
            >
              {t("ruleBuilder.removeIt")}
            </button>
            .
          </>
        ),
      });
    }

    if (malformedPath) {
      messages.push({
        tone: "attention",
        body: (
          <>
            <span className="font-mono">{fieldPath}</span> carries a{" "}
            <span className="font-mono">*</span> but is not a path the engine can
            read. Wildcards go inside brackets, as{" "}
            <span className="font-mono">parties[*].country</span>.
          </>
        ),
      });
    }

    for (const problem of expressionProblems) {
      messages.push({ tone: "attention", body: problem });
    }

    if (unknownField && !malformedPath) {
      messages.push({
        tone: "attention",
        body: (
          <>
            <span className="font-mono">{fieldPath}</span> is not on the
            transaction schema. Saving will be rejected until it is.
            {suggestion && (
              <>
                {" "}
                Did you mean{" "}
                <button
                  type="button"
                  disabled={ctx.readOnly}
                  onClick={() =>
                    ctx.updateNode(effectivePath, {
                      field: suggestion.path,
                      ...(hasWildcard(suggestion.path)
                        ? {}
                        : { quantifier: null }),
                    })
                  }
                  className="font-mono underline underline-offset-2 hover:text-attention-500"
                >
                  {suggestion.path}
                </button>
                ?
                {suggestion.path.toLowerCase() === fieldPath.toLowerCase() &&
                  " Field names are case-sensitive."}
              </>
            )}
          </>
        ),
      });
    }
  }

  return (
    <div
      ref={cardRef}
      {...dragProps}
      className={cn(
        "relative rounded-lg border border-slate-200 bg-card p-2.5 transition-all focus-within:ring-2 focus-within:ring-brand-200",
        negated && "ring-1 ring-attention-200 focus-within:ring-attention-200",
        (unknownField ||
          malformedPath ||
          missingQuantifier ||
          strayQuantifier ||
          undeclaredRole ||
          undeclaredMetric ||
          velocityProblems.length > 0) &&
          !violations &&
          "border-attention-200 ring-1 ring-attention-200 focus-within:ring-attention-200",
        violations &&
          "border-critical-200 ring-1 ring-critical-200 focus-within:ring-critical-200",
        isDragging && "opacity-40"
      )}
    >
      {!ctx.readOnly && (
        <div className="absolute right-2 top-2 flex items-center gap-0.5">
          {nodeActions}
        </div>
      )}

      <div
        className={cn(
          "flex flex-wrap items-center gap-x-2 gap-y-2",
          !ctx.readOnly && "pr-[4.25rem]"
        )}
      >
        <div className="flex shrink-0 items-center gap-1">
          {dragHandle}
          {negateControl}
        </div>

        <div
          className={cn(
            "flex min-w-0 flex-wrap items-center gap-1.5",
            panelSubject && "basis-full"
          )}
        >
          {!ctx.readOnly && (
            <SubjectKindToggle
              kind={
                isVelocityLeaf
                  ? "velocity"
                  : isPartyLeaf
                    ? "party"
                    : isExpressionLeaf
                      ? "expression"
                      : "field"
              }
              disabled={ctx.readOnly}
              partyOffered={ctx.partyOffered || isPartyLeaf}
              velocityOffered={ctx.velocityOffered || isVelocityLeaf}
              onChange={setLeafKind}
            />
          )}

          {(resolvesMany || strayQuantifier) && (
            <div className="flex shrink-0 items-center gap-1">
              <Select
                value={(effective.quantifier as string) || ""}
                onValueChange={(v) =>
                  ctx.updateNode(effectivePath, {
                    quantifier: v as RuleQuantifier,
                  })
                }
                disabled={ctx.readOnly}
              >
                <SelectTrigger
                  className={cn(
                    "h-9 w-[5.5rem] text-xs",
                    (missingQuantifier || strayQuantifier) &&
                      "border-attention-500"
                  )}
                >
                  <SelectValue placeholder={t("ruleBuilder.anyAll")} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Any">{t("ruleBuilder.anyOf")}</SelectItem>
                  <SelectItem value="All">{t("ruleBuilder.allOf")}</SelectItem>
                </SelectContent>
              </Select>
              <HintTip label={t("ruleBuilder.howTheQuantifierReads")}>
                {isVelocityLeaf
                  ? "The mapping declares this role with a wildcard, so a payload can name several parties in it. Each one gets its own window, and the quantifier is applied to their answers — so 'any of' asks whether one of them ran the total up, not whether they did between them."
                  : isPartyLeaf
                    ? "The mapping declares this role with a wildcard, so a payload can name several parties in it and the condition has to say which it means."
                    : "This path resolves to many values, so the condition has to say which it means."}{" "}
                Both answer false over an empty list — &ldquo;all of&rdquo; is
                deliberately not vacuously true, so a rule cannot fire on a
                payload carrying none of what it is about.
              </HintTip>
            </div>
          )}

          {isVelocityLeaf ? (
            <VelocitySubject
              velocity={effective.velocity!}
              mappings={ctx.partyMappings}
              metrics={ctx.velocityMetrics}
              thresholds={ctx.velocityThresholds}
              disabled={ctx.readOnly}
              tone={
                violations
                  ? "critical"
                  : undeclaredRole || undeclaredMetric
                    ? "warning"
                    : "default"
              }
              onChange={(velocity) =>
                ctx.updateNode(effectivePath, {
                  velocity,
                  ...(isManyParty(
                    entryForRole(ctx.partyMappings, String(velocity.role))
                  )
                    ? {}
                    : { quantifier: null }),
                })
              }
            />
          ) : isPartyLeaf ? (
            <PartySubject
              party={effective.party!}
              mappings={ctx.partyMappings}
              disabled={ctx.readOnly}
              tone={
                violations ? "critical" : undeclaredRole ? "warning" : "default"
              }
              onChange={(party) =>
                ctx.updateNode(effectivePath, {
                  party,
                  ...(isManyParty(
                    entryForRole(ctx.partyMappings, String(party.role))
                  )
                    ? {}
                    : { quantifier: null }),
                })
              }
            />
          ) : isExpressionLeaf ? (
            <div className="w-full min-w-0 flex-1">
              <ExpressionBuilder
                expression={effective.expression!}
                fields={ctx.schemaFields}
                mappings={ctx.partyMappings}
                velocityMetrics={ctx.velocityMetrics}
                velocityThresholds={ctx.velocityThresholds}
                velocityOffered={ctx.velocityOffered}
                disabled={ctx.readOnly}
                onChange={(expression) =>
                  ctx.updateNode(effectivePath, { expression })
                }
              />
            </div>
          ) : (
            <>
              <SchemaFieldCombobox
                value={fieldPath}
                onChange={(field) =>
                  ctx.updateNode(effectivePath, {
                    field,
                    ...(hasWildcard(field) ? {} : { quantifier: null }),
                  })
                }
                disabled={ctx.readOnly}
                fields={ctx.schemaFields}
                tone={
                  violations
                    ? "critical"
                    : unknownField || malformedPath
                      ? "warning"
                      : "default"
                }
                className="w-48"
              />

              {resolution && (
                <TypeChip
                  type={resolution.type}
                  multiple={resolution.multiple}
                  isRequired={resolution.isRequired}
                />
              )}
            </>
          )}
        </div>

        {!panelSubject && <ZoneDivider />}

        <div className="flex min-w-0 flex-1 flex-wrap items-center gap-1.5">
          <Select
            value={(effective.condition as string) || ""}
            onValueChange={(v) => {
              const next = v as RuleCondition;
              const nextShape = shapeOf(next);
              const keepsValue = nextShape === shape && nextShape !== "none";
              ctx.updateNode(effectivePath, {
                condition: next,
                ...(keepsValue ? {} : { value: null, json: false }),
              });
            }}
            disabled={ctx.readOnly}
          >
            <SelectTrigger className="w-auto min-w-[9rem] shrink-0">
              <SelectValue placeholder={t("ruleBuilder.condition")} />
            </SelectTrigger>
            <SelectContent>
              {operators.map((spec) => (
                <SelectItem key={spec.condition} value={spec.condition}>
                  {spec.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          {operator?.hint && (
            <HintTip label={`What ${operator.label} means`}>
              {operator.hint}
            </HintTip>
          )}

          <ConditionValue
            shape={shape}
            node={effective}
            disabled={ctx.readOnly}
            onChange={(updates) => ctx.updateNode(effectivePath, updates)}
          />
        </div>

      </div>

      {messages.length > 0 && (
        <div className="mt-2">
          <LeafMessages messages={messages} />
        </div>
      )}
    </div>
  );
};

const RuleBuilder: React.FC<RuleBuilderProps> = ({
  ruleSetId,
  initialRules,
  submitRuleText,
  readOnly,
  matchPolarity: initialMatchPolarity,
  canEditMatchPolarity = false,
  saveButtonText,
  allowVelocity = true,
  schemaViolations,
  toolbarLeading,
  toolbarActions,
  handleSaveBody,
  handleSubmitRule,
  handleSubmit,
}) => {
  const t = useTranslation();
  const { user } = useAuth();
  const { holds } = usePermissions();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);
  const [rules, setRules] = useState<RuleBodyDto[]>(initialRules);
  const [history, setHistory] = useState<RuleBodyDto[][]>([initialRules]);
  const [historyIndex, setHistoryIndex] = useState<number>(0);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState<boolean>(false);
  const [isTestModalOpen, setIsTestModalOpen] = useState<boolean>(false);
  const [newPayload, setNewPayload] = useState<string>("");
  const [newFriendlyName, setNewFriendlyName] = useState<string>("");
  const [newIsEnabled, setNewIsEnabled] = useState<boolean>(true);
  const [newAction, setNewAction] = useState<RuleAction>("Review");
  const [newSeverity, setNewSeverity] = useState<RuleSeverity>("Medium");
  const [newRiskWeight, setNewRiskWeight] = useState<number>(0);
  const [newHoldPolicy, setNewHoldPolicy] = useState<HoldPolicy>({
    holdWindow: null,
    holdExpiryVerdict: null,
  });
  const [newAlertPolicy, setNewAlertPolicy] = useState<RuleAlertPolicy>(
    EMPTY_RULE_ALERT_POLICY
  );
  const [saving, setSaving] = useState<boolean>(false);
  const [matchPolarity, setMatchPolarity] = useState<RuleMatchPolarity>(
    initialMatchPolarity ?? "MatchesWhenTrue"
  );

  useEffect(() => {
    setMatchPolarity(initialMatchPolarity ?? "MatchesWhenTrue");
  }, [initialMatchPolarity]);

  const [dragPath, setDragPath] = useState<number[] | null>(null);
  const [activeDrop, setActiveDrop] = useState<DropInfo | null>(null);

  const [ruleViolations, setRuleViolations] = useState<SchemaViolation[]>(
    schemaViolations ?? []
  );
  const [violationHeadline, setViolationHeadline] = useState<string | null>(
    null
  );
  const [payloadViolations, setPayloadViolations] = useState<SchemaViolation[]>(
    []
  );

  // Serialised rather than compared by identity: callers that pass a literal
  // would otherwise hand over a new array on every one of their renders.
  const externalViolationsKey = JSON.stringify(schemaViolations ?? []);
  useEffect(() => {
    setRuleViolations(schemaViolations ?? []);
    setViolationHeadline(null);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [externalViolationsKey]);

  const violations = useMemo(
    () => violationsByField(ruleViolations),
    [ruleViolations]
  );

  const violationsFor = useCallback(
    (subject: string): string[] | undefined => {
      const exact = violations.get(subject);
      if (exact) return exact;

      const generalised = generalisePath(subject);
      const collected: string[] = [];

      for (const [field, messages] of violations) {
        if (field === subject) continue;
        if (generalisePath(field) !== generalised) continue;
        collected.push(...messages.map((message) => `at ${field}: ${message}`));
      }

      return collected.length > 0 ? collected : undefined;
    },
    [violations]
  );

  const { fields: schemaFields } = useTransactionSchema(
    !readOnly && holds("transaction-schemas:read")
  );

  const partyLookupEnabled = !readOnly && holds("party-mappings:read");
  const { status: partyStatus, mappings: partyMappings } =
    usePartyMapping(partyLookupEnabled);

  const velocityLookupEnabled = !readOnly && holds("velocity-profiles:read");
  const {
    status: velocityStatus,
    metrics: velocityMetrics,
    thresholds: velocityThresholds,
  } = useVelocityProfile(velocityLookupEnabled);

  const schemaPathCount = useMemo(
    () => (schemaFields ? schemaPaths(schemaFields).length : 0),
    [schemaFields]
  );

  const debounce = <T extends (...args: any[]) => any>( // eslint-disable-line @typescript-eslint/no-explicit-any
    func: T,
    delay: number
  ): ((...args: Parameters<T>) => void) => {
    let timer: ReturnType<typeof setTimeout>;
    return (...args: Parameters<T>) => {
      clearTimeout(timer);
      timer = setTimeout(() => func(...args), delay);
    };
  };

  const saveStateToHistory = useCallback(
    (newState: RuleBodyDto[]) => {
      const newHistory = history.slice(0, historyIndex + 1);
      newHistory.push(newState);
      setHistory(newHistory);
      setHistoryIndex(newHistory.length - 1);
    },
    [history, historyIndex]
  );

  const debouncedSaveStateToHistory = useCallback(
    debounce(saveStateToHistory, 200),
    [saveStateToHistory]
  );

  const handleUndo = useCallback(() => {
    if (historyIndex > 0) {
      setRules(history[historyIndex - 1]);
      setHistoryIndex(historyIndex - 1);
    }
  }, [history, historyIndex]);

  const handleRedo = useCallback(() => {
    if (historyIndex < history.length - 1) {
      setRules(history[historyIndex + 1]);
      setHistoryIndex(historyIndex + 1);
    }
  }, [history, historyIndex]);

  useEffect(() => {
    if (readOnly) return;
    const onKey = (e: KeyboardEvent) => {
      if (!(e.ctrlKey || e.metaKey)) return;
      const k = e.key.toLowerCase();
      if (k === "z" && !e.shiftKey) {
        e.preventDefault();
        handleUndo();
      } else if ((k === "z" && e.shiftKey) || k === "y") {
        e.preventDefault();
        handleRedo();
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [readOnly, handleUndo, handleRedo]);

  const commit = useCallback(
    (updater: (prev: RuleBodyDto[]) => RuleBodyDto[], debounced = false) => {
      setRuleViolations([]);
      setViolationHeadline(null);
      setRules((prev) => {
        const updated = updater(prev);
        if (debounced && history.length > 1) debouncedSaveStateToHistory(updated);
        else saveStateToHistory(updated);
        return updated;
      });
    },
    [history, saveStateToHistory, debouncedSaveStateToHistory]
  );

  const addBasicRule = useCallback(
    (parentPath: number[] = []) => {
      const newRule: RuleBodyDto = { condition: null, field: null, value: null };
      commit((prev) =>
        insertNode(prev, parentPath, childrenAt(prev, parentPath).length, newRule)
      );
    },
    [commit]
  );

  const addContainer = useCallback(
    (type: GroupKey, parentPath: number[] = []) => {
      const newRule: RuleBodyDto = type === "and" ? { and: [] } : { or: [] };
      commit((prev) =>
        insertNode(prev, parentPath, childrenAt(prev, parentPath).length, newRule)
      );
    },
    [commit]
  );

  const removeNodeAt = useCallback(
    (path: number[]) => {
      commit((prev) => removeNode(prev, path).rules);
    },
    [commit]
  );

  const duplicateNodeAt = useCallback(
    (path: number[]) => {
      commit((prev) => {
        const node = getNode(prev, path);
        if (!node) return prev;
        const parentPath = path.slice(0, -1);
        const index = path[path.length - 1] + 1;
        return insertNode(prev, parentPath, index, cloneRule(node));
      });
    },
    [commit]
  );

  const updateNodeAt = useCallback(
    (path: number[], updates: Partial<RuleBodyDto>) => {
      commit((prev) => updateNode(prev, path, updates), true);
    },
    [commit]
  );

  const moveNode = useCallback(
    (from: number[], target: DropInfo) => {
      commit((prev) => {
        // Can't drop a node into its own subtree (or onto itself).
        if (isPrefixPath(from, target.parentPath)) return prev;

        const fromParent = from.slice(0, -1);
        const fromIndex = from[from.length - 1];

        // Capacity guard for the destination container.
        const parentNode =
          target.parentPath.length > 0 ? getNode(prev, target.parentPath) : null;
        const destKey = parentNode ? childKeyOf(parentNode) : null;
        const movingWithinSameParent = samePath(fromParent, target.parentPath);
        const destCount = childrenAt(prev, target.parentPath).length;
        const effectiveCount = movingWithinSameParent ? destCount - 1 : destCount;
        if (destKey === "not" && effectiveCount >= 1) return prev;
        if ((destKey === "and" || destKey === "or") && effectiveCount >= AND_OR_LIMIT)
          return prev;

        const { rules: without, node } = removeNode(prev, from);
        if (!node) return prev;

        // Adjust the destination for the index shift caused by removal.
        let adjParent = target.parentPath;
        let adjIndex = target.index;
        if (movingWithinSameParent && fromIndex < target.index) {
          adjIndex = target.index - 1;
        } else if (
          target.parentPath.length > fromParent.length &&
          isPrefixPath(fromParent, target.parentPath) &&
          target.parentPath[fromParent.length] > fromIndex
        ) {
          adjParent = [...target.parentPath];
          adjParent[fromParent.length] -= 1;
        }

        return insertNode(without, adjParent, adjIndex, node);
      });
    },
    [commit]
  );

  const toggleNegateAt = useCallback(
    (path: number[]) => {
      commit((prev) => toggleNegate(prev, path));
    },
    [commit]
  );

  const switchGroupTypeAt = useCallback(
    (path: number[], next: GroupKey) => {
      commit((prev) => switchGroupType(prev, path, next));
    },
    [commit]
  );

  const ctx: NodeCtx = useMemo(
    () => ({
      readOnly,
      violationsFor,
      schemaFields,
      partyMappings,
      partyOffered: partyStatus === "declared",
      partyMappingAbsent: partyLookupEnabled && partyStatus === "absent",
      velocityMetrics,
      velocityThresholds,
      velocityOffered:
        allowVelocity &&
        velocityStatus === "declared" &&
        partyStatus === "declared",
      velocityRefused: !allowVelocity,
      velocityProfileAbsent:
        velocityLookupEnabled && velocityStatus === "absent",
      dragPath,
      activeDrop,
      setDragPath,
      setActiveDrop,
      onDropAt: (info) => {
        if (dragPath) moveNode(dragPath, info);
        setDragPath(null);
        setActiveDrop(null);
      },
      addBasicRule,
      addContainer,
      removeNode: removeNodeAt,
      duplicateNode: duplicateNodeAt,
      updateNode: updateNodeAt,
      toggleNegate: toggleNegateAt,
      switchGroupType: switchGroupTypeAt,
    }),
    [
      readOnly,
      violationsFor,
      schemaFields,
      partyMappings,
      partyStatus,
      partyLookupEnabled,
      velocityMetrics,
      velocityThresholds,
      velocityStatus,
      velocityLookupEnabled,
      allowVelocity,
      dragPath,
      activeDrop,
      moveNode,
      addBasicRule,
      addContainer,
      removeNodeAt,
      duplicateNodeAt,
      updateNodeAt,
      toggleNegateAt,
      switchGroupTypeAt,
    ]
  );

  const handleTestRule = async () => {
    try {
      if (!rules.length) return toast.error(t("common.thereMustRootRule"));
      if (rules.length > 1) return toast.error(t("common.thereCanOnlyOneRoot"));

      let payload;
      try {
        payload = JSON.parse(newPayload);
      } catch {
        return toast.error(t("common.payloadInvalidJson"));
      }

      setPayloadViolations([]);

      const response = await apiFetch(`${API_BASE}/rules/test`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        // The polarity goes with the body: without it the API assumes
        // `MatchesWhenTrue`, and for an inverted rule every test would report
        // the opposite of what production does.
        body: JSON.stringify({ payload, ruleBody: rules[0], matchPolarity }),
        credentials: "include",
      });

      if (response.ok) {
        const data = await unwrap<RuleTestResponse>(response);
        const conditionHeld =
          matchPolarity === "MatchesWhenTrue" ? data.matched : !data.matched;
        const description = t(
          "ruleBuilder.test.description",
          t(
            conditionHeld
              ? "ruleBuilder.test.conditionMet"
              : "ruleBuilder.test.conditionNotMet"
          ),
          t(POLARITY_OPTION_LABEL[matchPolarity])
        );

        if (data.matched)
          toast.success(t("ruleBuilder.test.fires"), { description });
        else toast.error(t("ruleBuilder.test.doesNotFire"), { description });

        setRuleViolations([]);
        return;
      }

      await reportApiError(response, {
        fallback: t("ruleBuilder.failedTestRule"),
        onUnauthorized: goToLogin,
        onSchemaViolations: (report) => {
          if (report.kind === "payload") {
            setPayloadViolations(report.violations);
            return;
          }
          setRuleViolations(report.violations);
          setViolationHeadline(report.title);
        },
      });
    } catch {
      toast.error(t("ruleBuilder.failedTestRule"));
    }
  };

  const handleValidate = async () => {
    try {
      if (!rules.length) return toast.error(t("common.thereMustRootRule"));
      if (rules.length > 1) return toast.error(t("common.thereCanOnlyOneRoot"));

      const response = await apiFetch(`${API_BASE}/rules/validate`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(rules[0]),
        credentials: "include",
      });

      if (response.ok) {
        setRuleViolations([]);
        toast.success(t("ruleBuilder.ruleValidatedSuccessfully"));
        return;
      }

      await reportApiError(response, {
        fallback: t("ruleBuilder.failedValidateRule"),
        onUnauthorized: goToLogin,
        onSchemaViolations: (report) => {
          setRuleViolations(report.violations);
          setViolationHeadline(report.title);
        },
      });
    } catch {
      toast.error(t("ruleBuilder.failedValidateRule"));
    }
  };

  const closeModal = () => {
    setIsCreateModalOpen(false);
    setIsTestModalOpen(false);
  };

  const handleSave = async () => {
    if (!handleSaveBody) {
      return handleSubmit?.({
        setIsCreateModalOpen,
        setNewFriendlyName,
        setNewIsEnabled,
      });
    }
    if (!rules.length) return toast.error(t("common.thereMustRootRule"));
    if (rules.length > 1) return toast.error(t("common.thereCanOnlyOneRoot"));
    setSaving(true);
    try {
      await handleSaveBody(rules);
    } finally {
      setSaving(false);
    }
  };

  const preview = useMemo(
    () => rules.flatMap((rule) => previewLines(rule)),
    [rules]
  );

  const unplacedViolations = useMemo(() => {
    const subjects = new Set<string>();
    const generalised = new Set<string>();

    const claim = (subject: string) => {
      subjects.add(subject);
      generalised.add(generalisePath(subject));
    };

    const walk = (rule: RuleBodyDto) => {
      if (rule.field) claim(rule.field);
      if (rule.expression) claim(describeExpression(rule.expression));
      if (rule.party) claim(partyLeafName(rule.party));
      if (rule.velocity) claim(velocityLeafName(rule.velocity));
      for (const child of [
        ...(rule.and ?? []),
        ...(rule.or ?? []),
        ...(rule.not ?? []),
      ]) {
        walk(child);
      }
    };

    rules.forEach(walk);

    return ruleViolations.filter(
      (v) =>
        !subjects.has(v.field) && !generalised.has(generalisePath(v.field))
    );
  }, [rules, ruleViolations]);

  return (
    <TooltipProvider delayDuration={150}>
    <div className="space-y-4">
      <div className="flex items-center justify-between gap-2 flex-wrap">
        <div className="flex items-center gap-1.5 flex-wrap">
          {toolbarLeading}
          {!readOnly && (
            <>
              <Button
                variant="outline"
                size="sm"
                className="gap-1"
                onClick={() => addBasicRule()}
              >
                <Plus className="size-4" /> {t("ruleBuilder.condition")}
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="gap-1"
                onClick={() => addContainer("and")}
              >
                <Plus className="size-4" /> {t("ruleBuilder.group")}
              </Button>
            </>
          )}
        </div>
        <div className="flex items-center gap-1.5">
          <Button
            variant="outline"
            size="sm"
            className="gap-1"
            onClick={() => setIsTestModalOpen(true)}
          >
            <FlaskConical className="size-4" /> {t("ruleBuilder.test")}
          </Button>
          <Button variant="outline" size="sm" onClick={handleValidate}>
            {t("ruleBuilder.validate")}
          </Button>
          {!readOnly && (
            <>
              <Button
                variant="outline"
                size="icon"
                disabled={historyIndex <= 0}
                onClick={handleUndo}
                title={t("ruleBuilder.undoCtrlZ")}
                aria-label={t("ruleBuilder.undoCtrlZ")}
              >
                <RotateCcw />
              </Button>
              <Button
                variant="outline"
                size="icon"
                disabled={historyIndex >= history.length - 1}
                onClick={handleRedo}
                title={t("ruleBuilder.redoCtrlShiftZ")}
              >
                <RotateCw />
              </Button>
              <Button
                size="sm"
                className="gap-1.5"
                disabled={saving}
                onClick={handleSave}
              >
                <Save className="size-4" />
                {saving ? "Saving..." : saveButtonText ?? "Save"}
              </Button>
            </>
          )}
          {toolbarActions}
        </div>
      </div>

      {rules.length > 0 && (
        <div className="flex items-start gap-2 rounded-lg border border-brand-200 bg-brand-50 px-3 py-2">
          <Sparkles className="mt-0.5 size-4 shrink-0 text-brand-800" />
          <div className="min-w-0">
            <div className="font-mono text-[11px] font-medium uppercase tracking-widest text-brand-800">
              {matchPolarity === "MatchesWhenFalse"
                ? "Fires when this is NOT true"
                : "Fires when this is true"}
            </div>
            <div className="mt-1 space-y-0.5 overflow-x-auto">
              {preview.map((line, index) => (
                <div
                  key={index}
                  className={cn(
                    "whitespace-pre font-mono text-sm",
                    line.isGroup
                      ? "text-[11px] font-medium uppercase tracking-widest text-brand-800"
                      : "text-slate-700"
                  )}
                  style={{ paddingLeft: `${line.depth * 1.15}rem` }}
                >
                  {line.text}
                </div>
              ))}
            </div>
          </div>
        </div>
      )}

      {ruleViolations.length > 0 && (
        <div className="flex items-start gap-2 rounded-lg border border-critical-200 bg-critical-50 px-3 py-2">
          <AlertTriangle className="mt-0.5 size-4 shrink-0 text-critical-700" />
          <div className="space-y-1">
            <p className="text-sm font-medium text-critical-700">
              {violationHeadline ??
                "This rule does not match the transaction schema"}
            </p>
            <p className="text-xs text-critical-700">
              The conditions it objects to are marked below. Every branch is
              checked, including ones evaluation would short-circuit past — so a
              violation can sit inside an <code className="font-mono">or</code>{" "}
              that never runs today.
            </p>
            {unplacedViolations.length > 0 && (
              <ul className="space-y-0.5 text-xs text-critical-700">
                {unplacedViolations.map((violation, i) => (
                  <li key={i}>
                    <span className="font-mono">{violation.field}</span>{" "}
                    {violation.message}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}

      {!readOnly && (
        <p className="text-xs text-slate-400">
          <GripVertical className="inline size-3 -mt-0.5" />{" "}
          {t("ruleBuilder.dragTheHandleToReorder")}
          {schemaFields && (
            <>
              {" "}
              Fields come from this organisation&apos;s transaction schema (
              {schemaPathCount}{" "}
              {schemaPathCount === 1 ? "path" : "paths"}) — start typing to pick
              one. Nested members and array elements are addressed as{" "}
              <span className="font-mono">card.bin</span> and{" "}
              <span className="font-mono">parties[*].country</span>.
            </>
          )}
        </p>
      )}

      {rules.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-slate-300 bg-slate-50/60 px-6 py-12 text-center">
          <p className="text-sm font-medium text-slate-600">{t("ruleBuilder.noConditionsYet")}</p>
          <p className="max-w-sm text-xs text-slate-400">
            Start with a single condition, or add a group to combine several with
            ALL / ANY logic.
          </p>
          {!readOnly && (
            <div className="flex items-center gap-2">
              <Button size="sm" className="gap-1" onClick={() => addBasicRule()}>
                <Plus className="size-4" /> {t("ruleBuilder.addCondition")}
              </Button>
              <Button variant="outline" size="sm" onClick={() => addContainer("and")}>
                {t("ruleBuilder.addGroup")}
              </Button>
            </div>
          )}
        </div>
      ) : (
        <div>
          <DropZone info={{ parentPath: [], index: 0 }} ctx={ctx} />
          {rules.map((rule, index) => (
            <React.Fragment key={index}>
              <RuleNode rule={rule} path={[index]} ctx={ctx} />
              <DropZone info={{ parentPath: [], index: index + 1 }} ctx={ctx} />
            </React.Fragment>
          ))}
        </div>
      )}

      <Dialog open={isCreateModalOpen} onOpenChange={(open) => !open && closeModal()}>
        <DialogContent className="max-h-[80vh] grid-rows-[auto_minmax(0,1fr)_auto] sm:max-w-3xl">
          <DialogHeader>
            <DialogTitle>
              {t("ruleBuilder.dialogTitle", submitRuleText)}
            </DialogTitle>
          </DialogHeader>
          <div className="space-y-4 overflow-y-auto py-2 pr-1">
            <Input
              placeholder={t("ruleBuilder.friendlyName")}
              value={newFriendlyName}
              onChange={(e) => setNewFriendlyName(e.target.value)}
            />
            <label className="inline-flex items-center gap-2 font-mono text-sm text-slate-700">
              <input
                type="checkbox"
                checked={newIsEnabled}
                onChange={(e) => setNewIsEnabled(e.target.checked)}
                className="h-4 w-4 rounded border-slate-300 accent-brand-800"
              />
              {t("common.enabled")}
            </label>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("ruleBuilder.ruleFiresWhen")}
              </label>
              <Select
                value={matchPolarity}
                onValueChange={(v) => setMatchPolarity(v as RuleMatchPolarity)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {POLARITY_OPTIONS.map((p) => (
                    <SelectItem key={p} value={p}>
                      {t(POLARITY_OPTION_LABEL[p])}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-400">
                Cannot be changed after the rule is created — the logic above
                can be revised through versions, but which way it reads cannot.
              </p>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("ruleBuilder.action")}
                </label>
                <Select
                  value={newAction}
                  onValueChange={(v) => setNewAction(v as RuleAction)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {ACTION_OPTIONS.map((a) => (
                      <SelectItem key={a} value={a}>
                        {a}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("common.severity")}
                </label>
                <Select
                  value={newSeverity}
                  onValueChange={(v) => setNewSeverity(v as RuleSeverity)}
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {SEVERITY_OPTIONS.map((s) => (
                      <SelectItem key={s} value={s}>
                        {s}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("ruleBuilder.riskWeight")}
              </label>
              <Input
                type="number"
                min={0}
                max={100}
                value={newRiskWeight}
                onChange={(e) =>
                  setNewRiskWeight(
                    Math.max(0, Math.min(100, Number(e.target.value) || 0))
                  )
                }
              />
              <p className="text-xs text-slate-400">
                Added to the risk score when this rule fires, and compared
                against the rule set&apos;s review threshold to decide
                whether to escalate to Review. 0 means this rule never
                contributes to escalation.
              </p>
            </div>

            <div className="space-y-1.5 border-t border-slate-100 pt-4">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("ruleBuilder.holdPolicy")}
              </label>
              <p className="pb-1 text-xs text-slate-500">
                What happens to a transaction this rule holds if nobody
                reviews it in time. Only applies when the action is Review.
              </p>
              <HoldPolicyFields
                value={newHoldPolicy}
                onChange={setNewHoldPolicy}
                inheritance={{ organization: user?.organization ?? null }}
              />
            </div>

            <div className="space-y-3 border-t border-slate-100 pt-4">
              <div>
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("ruleBuilder.alerting")}
                </label>
                <p className="pb-1 text-xs text-slate-500">
                  Who this rule&apos;s findings are filed against, and how
                  quickly a repeat of one folds into the alert already open.
                </p>
              </div>
              <RuleAlertPolicyFields
                value={newAlertPolicy}
                onChange={setNewAlertPolicy}
                inheritance={{ organization: user?.organization ?? null }}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={closeModal}>
              {t("common.cancel")}
            </Button>
            <Button
              onClick={() =>
                handleSubmitRule?.({
                  ruleSetId,
                  rules,
                  newFriendlyName,
                  isNewEnabled: newIsEnabled,
                  newMatchPolarity: matchPolarity,
                  newAction,
                  newSeverity,
                  newRiskWeight,
                  newHoldPolicy,
                  newAlertPolicy,
                  setIsCreateModalOpen,
                })
              }
            >
              {submitRuleText}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={isTestModalOpen} onOpenChange={(open) => !open && closeModal()}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>{t("ruleBuilder.testRule")}</DialogTitle>
          </DialogHeader>

          <div className="space-y-1.5">
            <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
              {t("ruleBuilder.ruleFiresWhen")}
            </label>
            {canEditMatchPolarity ? (
              <Select
                value={matchPolarity}
                onValueChange={(v) => setMatchPolarity(v as RuleMatchPolarity)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {POLARITY_OPTIONS.map((p) => (
                    <SelectItem key={p} value={p}>
                      {t(POLARITY_OPTION_LABEL[p])}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            ) : (
              <div className="flex flex-wrap items-center gap-2">
                <MatchPolarityBadge matchPolarity={matchPolarity} />
                <span className="text-xs text-slate-400">
                  {t("ruleBuilder.testingAsThisRuleIs")}
                </span>
              </div>
            )}
          </div>

          <div className="rounded-md border border-slate-200 overflow-hidden">
            <JsonEditor
              value={newPayload}
              onChange={(value) => {
                setNewPayload(value);
                setPayloadViolations([]);
              }}
            />
          </div>

          {payloadViolations.length > 0 && (
            <div className="rounded-md border border-critical-200 bg-critical-50 px-3 py-2">
              <p className="text-xs font-medium text-critical-700">
                {t("ruleBuilder.thisPayloadDoesNotMatch")}
              </p>
              <ul className="mt-1 space-y-0.5 text-xs text-critical-700">
                {payloadViolations.map((violation, i) => (
                  <li key={i}>
                    <span className="font-mono">{violation.field}</span>{" "}
                    {violation.message}
                  </li>
                ))}
              </ul>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={closeModal}>
              {t("common.cancel")}
            </Button>
            <Button onClick={handleTestRule}>{t("ruleBuilder.test")}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
    </TooltipProvider>
  );
};

export default RuleBuilder;
