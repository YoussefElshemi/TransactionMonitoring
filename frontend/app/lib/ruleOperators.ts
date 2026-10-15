import { RuleCondition, SchemaFieldType } from "@/types";

export type ValueShape =
  | "none"
  | "single"
  | "list"
  | "pair"
  | "text"
  | "pattern";

export interface OperatorSpec {
  condition: RuleCondition;
  label: string;
  shape: ValueShape;
  types: SchemaFieldType[];
  hint?: string;
}

const SCALARS: SchemaFieldType[] = ["String", "Number", "Boolean", "DateTime"];
const ORDERABLE: SchemaFieldType[] = ["String", "Number", "DateTime"];
const TEXT: SchemaFieldType[] = ["String"];

export const OPERATORS: OperatorSpec[] = [
  {
    condition: "IsNull",
    label: "Is Null",
    shape: "none",
    types: [...SCALARS, "Object", "Array"],
    hint: "The only condition that can hold of a field the payload does not carry. On a required top-level field it can never match, and the API rejects it.",
  },
  {
    condition: "EqualTo",
    label: "=",
    shape: "single",
    types: SCALARS,
  },
  {
    condition: "NotEqualTo",
    label: "≠",
    shape: "single",
    types: SCALARS,
    hint: "On a field the payload does not carry this answers false, not true — a missing field never fires a rule on its own. For the strict logical negation, wrap an = condition in NOT.",
  },
  {
    condition: "GreaterThan",
    label: ">",
    shape: "single",
    types: ORDERABLE,
    hint: "A string bound orders lexicographically, which is how an ISO-8601 timestamp compares.",
  },
  {
    condition: "LessThan",
    label: "<",
    shape: "single",
    types: ORDERABLE,
  },
  {
    condition: "GreaterThanEqual",
    label: "≥",
    shape: "single",
    types: ORDERABLE,
  },
  {
    condition: "LessThanEqual",
    label: "≤",
    shape: "single",
    types: ORDERABLE,
  },
  {
    condition: "Between",
    label: "Between",
    shape: "pair",
    types: ORDERABLE,
    hint: "Inclusive at both ends. Both bounds must be numbers or both strings, and the lower one goes first — a reversed range can never match, so the API rejects it.",
  },
  {
    condition: "In",
    label: "In",
    shape: "list",
    types: SCALARS,
  },
  {
    condition: "NotIn",
    label: "Not in",
    shape: "list",
    types: SCALARS,
    hint: "Like ≠, this answers false on a field the payload does not carry rather than true.",
  },
  {
    condition: "Contains",
    label: "Contains",
    shape: "text",
    types: TEXT,
    hint: "Case-insensitive.",
  },
  {
    condition: "StartsWith",
    label: "Starts with",
    shape: "text",
    types: TEXT,
    hint: "Case-insensitive.",
  },
  {
    condition: "EndsWith",
    label: "Ends with",
    shape: "text",
    types: TEXT,
    hint: "Case-insensitive.",
  },
  {
    condition: "Matches",
    label: "Matches pattern",
    shape: "pattern",
    types: TEXT,
    hint: "A regular expression, up to 512 characters, run without backtracking — so backreferences and lookarounds are rejected when the rule is saved rather than risked on the authorisation path.",
  },
];

export const OPERATOR: Record<string, OperatorSpec> = Object.fromEntries(
  OPERATORS.map((spec) => [spec.condition, spec])
);

export const MAX_LIST_VALUES = 1000;
export const MAX_PATTERN_LENGTH = 512;

export const conditionsFor = (
  type: SchemaFieldType | null,
  current?: RuleCondition | (string & {}) | null
): OperatorSpec[] => {
  const offered =
    type === null
      ? OPERATORS
      : OPERATORS.filter((spec) => spec.types.includes(type));

  if (!current || offered.some((spec) => spec.condition === current)) {
    return offered;
  }

  const kept = OPERATOR[String(current)];
  return kept ? [...offered, kept] : offered;
};

export const EXPRESSION_RESULT_TYPE: SchemaFieldType = "Number";

export const shapeOf = (
  condition: RuleCondition | (string & {}) | null | undefined
): ValueShape => OPERATOR[String(condition)]?.shape ?? "single";

export const readTypedValue = (
  raw: string
): { value: unknown; json: boolean } => {
  try {
    return { value: JSON.parse(raw), json: true };
  } catch {
    return { value: raw, json: false };
  }
};

export const writeTypedValue = (value: unknown, json?: boolean): string => {
  if (value === null || value === undefined) return "";
  return json ? JSON.stringify(value) : String(value);
};
