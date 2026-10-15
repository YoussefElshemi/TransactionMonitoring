import { RuleExpressionDto, RuleFunction, SchemaFieldType } from "@/types";
import { hasWildcard, isAddressable, parsePath } from "@/lib/payloadPath";
import { SchemaPath } from "@/lib/transactionSchema";
import { partyLeafName } from "@/lib/parties";
import { velocityLeafName } from "@/lib/velocityProfile";

export const MAX_EXPRESSION_DEPTH = 5;

export type FunctionArgument =
  | "number"
  | "string"
  | "collection"
  | "dateTime";

export interface FunctionSpec {
  function: RuleFunction;
  arity: 1 | 2;
  takes: FunctionArgument;
  isAggregate: boolean;
  label: string;
  hint: string;
}

export const FUNCTIONS: FunctionSpec[] = [
  {
    function: "Add",
    arity: 2,
    takes: "number",
    isAggregate: false,
    label: "Add",
    hint: "The two arguments summed.",
  },
  {
    function: "Subtract",
    arity: 2,
    takes: "number",
    isAggregate: false,
    label: "Subtract",
    hint: "The second argument taken from the first.",
  },
  {
    function: "Multiply",
    arity: 2,
    takes: "number",
    isAggregate: false,
    label: "Multiply",
    hint: "The two arguments multiplied.",
  },
  {
    function: "Divide",
    arity: 2,
    takes: "number",
    isAggregate: false,
    label: "Divide",
    hint: "The first divided by the second. Dividing by zero computes nothing, and a leaf comparing nothing never matches.",
  },
  {
    function: "Modulo",
    arity: 2,
    takes: "number",
    isAggregate: false,
    label: "Modulo",
    hint: "The remainder of the first divided by the second.",
  },
  {
    function: "Abs",
    arity: 1,
    takes: "number",
    isAggregate: false,
    label: "Absolute value",
    hint: "The argument without its sign.",
  },
  {
    function: "Length",
    arity: 1,
    takes: "string",
    isAggregate: false,
    label: "Length",
    hint: "How many characters the text holds.",
  },
  {
    function: "Count",
    arity: 1,
    takes: "collection",
    isAggregate: true,
    label: "Count",
    hint: "How many values the path resolved to. The one aggregate that may be taken over a list of objects — count(parties[*]) counts parties — and the one that answers 0 rather than nothing over an empty list, because 'no beneficiary at all' is worth a rule.",
  },
  {
    function: "Sum",
    arity: 1,
    takes: "collection",
    isAggregate: true,
    label: "Sum",
    hint: "The values added together. Computes nothing over an empty list — a sum of nothing is not zero pounds.",
  },
  {
    function: "Min",
    arity: 1,
    takes: "collection",
    isAggregate: true,
    label: "Minimum",
    hint: "The smallest value. Computes nothing over an empty list.",
  },
  {
    function: "Max",
    arity: 1,
    takes: "collection",
    isAggregate: true,
    label: "Maximum",
    hint: "The largest value. Computes nothing over an empty list.",
  },
  {
    function: "Average",
    arity: 1,
    takes: "collection",
    isAggregate: true,
    label: "Average",
    hint: "The mean of the values. Computes nothing over an empty list.",
  },
  {
    function: "HourOfDay",
    arity: 1,
    takes: "dateTime",
    isAggregate: false,
    label: "Hour of day",
    hint: "0–23, read in the offset the payload carries — so 'between one and four in the morning' means the local hour where the transaction was taken.",
  },
  {
    function: "MinuteOfDay",
    arity: 1,
    takes: "dateTime",
    isAggregate: false,
    label: "Minute of day",
    hint: "0–1439, read in the offset the payload carries.",
  },
  {
    function: "DayOfWeek",
    arity: 1,
    takes: "dateTime",
    isAggregate: false,
    label: "Day of week",
    hint: "Monday is 1 and Sunday is 7, so the weekend is 6 and 7.",
  },
  {
    function: "DaysSince",
    arity: 1,
    takes: "dateTime",
    isAggregate: false,
    label: "Days since",
    hint: "How long ago, in days, fractional. Measured against one clock taken per request, so two temporal rules in a set cannot answer as though they ran on different days.",
  },
  {
    function: "HoursSince",
    arity: 1,
    takes: "dateTime",
    isAggregate: false,
    label: "Hours since",
    hint: "How long ago, in hours, fractional.",
  },
];

export const FUNCTION_SPEC: Record<string, FunctionSpec> = Object.fromEntries(
  FUNCTIONS.map((spec) => [spec.function, spec])
);

export const FUNCTION_GROUPS: { label: string; functions: FunctionSpec[] }[] = [
  {
    label: "Over a list",
    functions: FUNCTIONS.filter((spec) => spec.isAggregate),
  },
  {
    label: "Dates and times",
    functions: FUNCTIONS.filter((spec) => spec.takes === "dateTime"),
  },
  {
    label: "Arithmetic",
    functions: FUNCTIONS.filter(
      (spec) => !spec.isAggregate && spec.takes === "number"
    ),
  },
  {
    label: "Text",
    functions: FUNCTIONS.filter((spec) => spec.takes === "string"),
  },
];

export const argumentType = (spec: FunctionSpec): SchemaFieldType | null => {
  switch (spec.takes) {
    case "number":
      return "Number";
    case "string":
      return "String";
    case "dateTime":
      return "DateTime";
    default:
      return spec.function === "Count" ? null : "Number";
  }
};

export const fitsOperand = (path: SchemaPath): boolean =>
  !path.multiple && path.type !== "Object" && path.type !== "Array";

export const fitsArgument = (spec: FunctionSpec, path: SchemaPath): boolean => {
  const type = argumentType(spec);

  if (spec.isAggregate) {
    return path.multiple && (type === null || path.type === type);
  }

  return fitsOperand(path) && path.type === type;
};

const camel = (name: string): string =>
  name.charAt(0).toLowerCase() + name.slice(1);

export const describeExpression = (
  expression: RuleExpressionDto | null | undefined
): string => {
  if (!expression) return "";

  if (expression.field) return expression.field;

  if (expression.party) return partyLeafName(expression.party);

  if (expression.velocity) return velocityLeafName(expression.velocity);

  if (!expression.function) {
    const value = expression.value;
    if (value === null || value === undefined) return "null";
    if (typeof value === "boolean") return value ? "True" : "False";
    return String(value);
  }

  const args = (expression.arguments ?? []).map(describeExpression).join(", ");
  return `${camel(String(expression.function))}(${args})`;
};

export const expressionDepth = (
  expression: RuleExpressionDto | null | undefined
): number => {
  if (!expression) return 0;
  const args = expression.arguments ?? [];
  return 1 + (args.length === 0 ? 0 : Math.max(...args.map(expressionDepth)));
};

export type ExpressionForm =
  | "field"
  | "value"
  | "function"
  | "party"
  | "velocity"
  | "none";

export const expressionForm = (
  expression: RuleExpressionDto | null | undefined
): ExpressionForm => {
  if (!expression) return "none";
  if (expression.velocity) return "velocity";
  if (expression.party) return "party";
  if (expression.field) return "field";
  if (expression.function) return "function";
  if (expression.value !== null && expression.value !== undefined) {
    return "value";
  }
  return "none";
};

export const validateExpression = (
  expression: RuleExpressionDto | null | undefined
): string[] => {
  const problems: string[] = [];

  const walk = (node: RuleExpressionDto | null | undefined): void => {
    if (!node) return;

    const forms = [
      Boolean(node.field),
      node.value !== null && node.value !== undefined,
      Boolean(node.function),
      Boolean(node.party),
      Boolean(node.velocity),
    ].filter(Boolean).length;

    if (forms === 0) {
      problems.push("Every part of an expression must be a field, a value or a function.");
      return;
    }

    if (forms > 1) {
      problems.push(
        "A part of an expression must be a field, a value or a function, and only one of the three."
      );
      return;
    }

    if (node.party) {
      if (node.arguments?.length) {
        problems.push("A party cannot take arguments.");
      }
      return;
    }

    if (node.velocity) {
      if (node.arguments?.length) {
        problems.push("A velocity window cannot take arguments.");
      }
      return;
    }

    if (node.field) {
      if (!parsePath(node.field)) {
        problems.push(`'${node.field}' is not a readable field path.`);
      }
      if (node.arguments?.length) {
        problems.push("A field cannot take arguments.");
      }
      return;
    }

    if (!node.function) {
      if (node.arguments?.length) {
        problems.push("A value cannot take arguments.");
      }
      if (
        typeof node.value !== "string" &&
        typeof node.value !== "number" &&
        typeof node.value !== "boolean"
      ) {
        problems.push("A value must be a single string, number or true/false.");
      }
      return;
    }

    const spec = FUNCTION_SPEC[String(node.function)];
    if (!spec) {
      problems.push(`'${node.function}' is not a function the engine knows.`);
      return;
    }

    const args = node.arguments ?? [];
    if (args.length !== spec.arity) {
      problems.push(
        `'${spec.function}' takes ${spec.arity} argument${
          spec.arity === 1 ? "" : "s"
        } but was given ${args.length}.`
      );
      return;
    }

    for (const argument of args) {
      const wildcard = Boolean(
        argument.field && hasWildcard(argument.field)
      );

      if (argument.party || argument.velocity) {
        walk(argument);
        continue;
      }

      if (spec.isAggregate && !wildcard) {
        problems.push(
          `'${spec.function}' reduces many values, so its argument must be a field path carrying '[*]'.`
        );
      }

      if (!spec.isAggregate && wildcard) {
        problems.push(
          `'${spec.function}' reads a single value, so its argument must not be a field path carrying '[*]'.`
        );
      }

      walk(argument);
    }
  };

  if (expressionDepth(expression) > MAX_EXPRESSION_DEPTH) {
    problems.push(
      `An expression cannot be nested more than ${MAX_EXPRESSION_DEPTH} deep.`
    );
    return problems;
  }

  walk(expression);

  return [...new Set(problems)];
};

export const newExpression = (form: ExpressionForm): RuleExpressionDto => {
  switch (form) {
    case "field":
      return { field: "" };
    case "value":
      return { value: 0 };
    case "function":
      return { function: "DaysSince", arguments: [{ field: "" }] };
    default:
      return {};
  }
};

export const withFunction = (
  node: RuleExpressionDto,
  next: RuleFunction
): RuleExpressionDto => {
  const spec = FUNCTION_SPEC[next];
  const existing = node.arguments ?? [];
  const args: RuleExpressionDto[] = [];

  for (let i = 0; i < (spec?.arity ?? 1); i++) {
    args.push(existing[i] ?? { field: "" });
  }

  return { function: next, arguments: args };
};

export const isExpressionField = (path: string): boolean =>
  isAddressable(path) && parsePath(path) !== null;
