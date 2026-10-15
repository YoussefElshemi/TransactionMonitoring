import {
  ConditionMatch,
  RuleBodyDto,
  RuleCondition,
  RuleMatchPolarity,
  RuleQuantifier,
} from "@/types";
import { resolveAll } from "@/lib/payloadPath";

export interface ConditionRow extends ConditionMatch {
  negated: boolean | null;
  present: boolean;
  isExpression: boolean;
  isParty: boolean;
  isVelocity: boolean;
  weighed: unknown[] | null;
}

const PARTY_LEAF = /^party\.[A-Za-z]+\.[A-Za-z]+$/;

const VELOCITY_LEAF = /velocity\.[A-Za-z]+\./;

interface Leaf {
  body: RuleBodyDto;
  negated: boolean;
}

const collectLeaves = (
  body: RuleBodyDto,
  negated: boolean,
  leaves: Leaf[]
): void => {
  if (body.not && body.not.length > 0) {
    collectLeaves(body.not[0], !negated, leaves);
    return;
  }

  if (
    body.field != null ||
    body.expression != null ||
    body.party != null ||
    body.velocity != null
  ) {
    leaves.push({ body, negated });
    return;
  }

  if (body.and) {
    body.and.forEach((child) => collectLeaves(child, negated, leaves));
    return;
  }

  if (body.or) {
    body.or.forEach((child) => collectLeaves(child, negated, leaves));
  }
};

export const leafNegations = (
  ruleBody: RuleBodyDto,
  matchPolarity: RuleMatchPolarity | (string & {})
): boolean[] => {
  const leaves: Leaf[] = [];
  collectLeaves(ruleBody, matchPolarity === "MatchesWhenFalse", leaves);
  return leaves.map((leaf) => leaf.negated);
};

export const conditionRows = (
  conditionMatches: ConditionMatch[],
  ruleBody: RuleBodyDto,
  matchPolarity: RuleMatchPolarity | (string & {}),
  payload: Record<string, unknown> | null | undefined
): ConditionRow[] => {
  const leaves: Leaf[] = [];
  collectLeaves(ruleBody, matchPolarity === "MatchesWhenFalse", leaves);
  const aligned = leaves.length === conditionMatches.length;

  return conditionMatches.map((conditionMatch, index) => {
    const leaf = aligned ? leaves[index] : null;

    const quantifier =
      conditionMatch.quantifier ?? leaf?.body.quantifier ?? null;

    const isParty = leaf
      ? leaf.body.party != null
      : PARTY_LEAF.test(conditionMatch.field);

    const isVelocity = leaf
      ? leaf.body.velocity != null
      : !isParty && VELOCITY_LEAF.test(conditionMatch.field);

    const isExpression = leaf
      ? leaf.body.expression != null
      : !isParty &&
        !isVelocity &&
        quantifier == null &&
        /\(.*\)/.test(conditionMatch.field);

    const weighed =
      quantifier != null && Array.isArray(conditionMatch.actual)
        ? (conditionMatch.actual as unknown[])
        : null;

    return {
      ...conditionMatch,
      quantifier,
      negated: leaf ? leaf.negated : null,
      isExpression,
      isParty,
      isVelocity,
      weighed,
      present:
        isExpression || isParty || isVelocity
          ? true
          : resolveAll(payload, conditionMatch.field).length > 0,
    };
  });
};

const CONDITION_PREDICATE: Record<RuleCondition, [string, string]> = {
  IsNull: ["have no value", "have a value"],
  EqualTo: ["equal {value}", "not equal {value}"],
  NotEqualTo: ["differ from {value}", "not differ from {value}"],
  GreaterThan: ["be over {value}", "not be over {value}"],
  LessThan: ["be under {value}", "not be under {value}"],
  GreaterThanEqual: ["be {value} or more", "not be {value} or more"],
  LessThanEqual: ["be {value} or less", "not be {value} or less"],
  In: ["be one of {value}", "not be one of {value}"],
  NotIn: ["fall outside {value}", "not fall outside {value}"],
  Between: ["be between {value}", "not be between {value}"],
  Contains: ["contain {value}", "not contain {value}"],
  StartsWith: ["start with {value}", "not start with {value}"],
  EndsWith: ["end with {value}", "not end with {value}"],
  Matches: ["match the pattern {value}", "not match the pattern {value}"],
};

export const formatJsonValue = (value: unknown): string => {
  if (value === undefined) return "—";
  if (Array.isArray(value)) return `[${value.map(formatJsonValue).join(", ")}]`;
  if (value !== null && typeof value === "object") {
    return JSON.stringify(value);
  }
  return JSON.stringify(value) ?? String(value);
};

const formatExpected = (
  condition: RuleCondition | (string & {}) | null,
  expected: unknown
): string => {
  if (condition === "Between" && Array.isArray(expected) && expected.length === 2) {
    return `${formatJsonValue(expected[0])} and ${formatJsonValue(expected[1])}`;
  }
  return formatJsonValue(expected);
};

const QUANTIFIER_SUBJECT: Record<RuleQuantifier, [string, string]> = {
  Any: ["any of them must", "none of them may"],
  All: ["all of them must", "not all of them may"],
};

export const describeCondition = (row: ConditionRow): string | null => {
  if (row.negated === null) return null;

  const value = formatExpected(row.condition, row.expected);
  const predicates = CONDITION_PREDICATE[row.condition as RuleCondition];

  if (!predicates) {
    const verb = row.condition ? `satisfy ${row.condition} ${value}` : "match";
    return row.negated ? `must not ${verb}` : `must ${verb}`;
  }

  const quantifier = row.quantifier as RuleQuantifier | null;

  if (quantifier && QUANTIFIER_SUBJECT[quantifier]) {
    const subject = QUANTIFIER_SUBJECT[quantifier][row.negated ? 1 : 0];
    return `${subject} ${predicates[0].replace("{value}", value)}`;
  }

  return `must ${predicates[row.negated ? 1 : 0].replace("{value}", value)}`;
};

export const describeQuantifier = (row: ConditionRow): string | null => {
  const quantifier = row.quantifier as RuleQuantifier | null;
  if (!quantifier || !row.weighed) return null;

  const count = row.weighed.length;
  const nothing = row.isParty || row.isVelocity
    ? "No party stood in that role on this payment"
    : "The path resolved to nothing";

  if (count === 0) {
    return quantifier === "All"
      ? `${nothing}. 'All of' answers false over an empty list rather than being vacuously true, so this could not contribute.`
      : `${nothing}, so there was nothing for 'any of' to match.`;
  }

  if (row.isParty || row.isVelocity) {
    return `Weighed ${count} ${count === 1 ? "party" : "parties"} in that role, each over its own window.`;
  }

  return `Weighed ${count} ${count === 1 ? "value" : "values"} the path resolved to.`;
};
