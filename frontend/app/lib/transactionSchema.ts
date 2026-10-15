import { SchemaField, SchemaFieldType } from "@/types";
import { parsePath } from "@/lib/payloadPath";

export const SCHEMA_FIELD_TYPES: SchemaFieldType[] = [
  "String",
  "Number",
  "Boolean",
  "DateTime",
  "Object",
  "Array",
];

export const ELEMENT_TYPES: SchemaFieldType[] = SCHEMA_FIELD_TYPES.filter(
  (type) => type !== "Array"
);

export const SCALAR_TYPES: SchemaFieldType[] = [
  "String",
  "Number",
  "Boolean",
  "DateTime",
];

export const MAX_FIELDS = 200;
export const MAX_NESTING = 5;
export const MAX_FIELD_NAME_LENGTH = 128;
export const MAX_ALLOWED_VALUES = 1000;
export const MAX_ALLOWED_VALUE_LENGTH = 256;

export const isScalar = (type: SchemaFieldType): boolean =>
  SCALAR_TYPES.includes(type);

export const declaresMembers = (
  type: SchemaFieldType,
  elementType: SchemaFieldType | null
): boolean =>
  type === "Object" || (type === "Array" && elementType === "Object");

export interface DraftField {
  id: string;
  name: string;
  type: SchemaFieldType;
  isRequired: boolean;
  allowedValues: string[];
  elementType: SchemaFieldType | null;
  fields: DraftField[];
}

let nextDraftId = 0;
const draftId = (): string => `field-${nextDraftId++}`;

export const newDraftField = (): DraftField => ({
  id: draftId(),
  name: "",
  type: "String",
  isRequired: false,
  allowedValues: [],
  elementType: null,
  fields: [],
});

export const toDrafts = (fields: SchemaField[]): DraftField[] =>
  fields.map((field) => ({
    id: draftId(),
    name: field.name,
    type: field.type,
    isRequired: field.isRequired,
    allowedValues: field.allowedValues ?? [],
    elementType: field.elementType ?? null,
    fields: toDrafts(field.fields ?? []),
  }));

export const toRequestFields = (drafts: DraftField[]): SchemaField[] =>
  drafts.map((draft) => {
    const field: SchemaField = {
      name: draft.name.trim(),
      type: draft.type,
      isRequired: draft.isRequired,
    };

    if (draft.type === "String" && draft.allowedValues.length > 0) {
      field.allowedValues = draft.allowedValues;
    }

    if (draft.type === "Array" && draft.elementType) {
      field.elementType = draft.elementType;
    }

    if (declaresMembers(draft.type, draft.elementType) && draft.fields.length) {
      field.fields = toRequestFields(draft.fields);
    }

    return field;
  });

export interface DraftValidation {
  fieldErrors: Record<string, string>;
  formError: string | null;
}

const countDrafts = (drafts: DraftField[]): number =>
  drafts.reduce((total, draft) => total + 1 + countDrafts(draft.fields), 0);

const nestingOf = (drafts: DraftField[]): number =>
  drafts.length === 0
    ? 0
    : Math.max(...drafts.map((draft) => 1 + nestingOf(draft.fields)));

export const validateDrafts = (drafts: DraftField[]): DraftValidation => {
  const fieldErrors: Record<string, string> = {};
  let formError: string | null = null;

  const total = countDrafts(drafts);

  if (drafts.length === 0) {
    formError = "A schema must declare at least one field.";
  } else if (total > MAX_FIELDS) {
    formError = `A schema must not declare more than ${MAX_FIELDS} fields, across every level. This one declares ${total}.`;
  } else if (nestingOf(drafts) > MAX_NESTING) {
    formError = `A schema must not nest fields more than ${MAX_NESTING} deep.`;
  }

  const walk = (level: DraftField[]): void => {
    const seen = new Set<string>();

    for (const draft of level) {
      const name = draft.name.trim();
      const members = declaresMembers(draft.type, draft.elementType);

      if (!name) {
        fieldErrors[draft.id] = "Field name must be provided.";
      } else if (name.length > MAX_FIELD_NAME_LENGTH) {
        fieldErrors[draft.id] =
          `Field name must not be longer than ${MAX_FIELD_NAME_LENGTH} characters.`;
      } else if (seen.has(name)) {
        fieldErrors[draft.id] = `'${name}' is already declared alongside this one. Field names must be unique within their parent, and are compared case-sensitively.`;
      } else if (draft.type === "Array" && !draft.elementType) {
        fieldErrors[draft.id] = "An array must say what it holds.";
      } else if (members && draft.fields.length === 0) {
        fieldErrors[draft.id] =
          draft.type === "Object"
            ? "An object must declare the fields it carries."
            : "An array of objects must declare the fields each one carries.";
      } else if (draft.type === "String") {
        const values = draft.allowedValues;
        if (values.length > MAX_ALLOWED_VALUES) {
          fieldErrors[draft.id] =
            `A field must not allow more than ${MAX_ALLOWED_VALUES} values.`;
        } else if (
          values.some((value) => value.length > MAX_ALLOWED_VALUE_LENGTH)
        ) {
          fieldErrors[draft.id] =
            `An allowed value must not be longer than ${MAX_ALLOWED_VALUE_LENGTH} characters.`;
        }
      }

      if (name) seen.add(name);

      if (draft.fields.length) walk(draft.fields);
    }
  };

  walk(drafts);

  return { fieldErrors, formError };
};

export const move = <T>(items: T[], from: number, to: number): T[] => {
  if (to < 0 || to >= items.length) return items;
  const next = [...items];
  const [item] = next.splice(from, 1);
  next.splice(to, 0, item);
  return next;
};

export const sameFields = (a: SchemaField[], b: SchemaField[]): boolean =>
  JSON.stringify(a) === JSON.stringify(b);

export const updateDraftAt = (
  drafts: DraftField[],
  id: string,
  updates: Partial<DraftField>
): DraftField[] =>
  drafts.map((draft) =>
    draft.id === id
      ? { ...draft, ...updates }
      : draft.fields.length
        ? { ...draft, fields: updateDraftAt(draft.fields, id, updates) }
        : draft
  );

export const removeDraftAt = (
  drafts: DraftField[],
  id: string
): DraftField[] =>
  drafts
    .filter((draft) => draft.id !== id)
    .map((draft) =>
      draft.fields.length
        ? { ...draft, fields: removeDraftAt(draft.fields, id) }
        : draft
    );

export const addDraftUnder = (
  drafts: DraftField[],
  parentId: string | null
): DraftField[] => {
  if (parentId === null) return [...drafts, newDraftField()];

  return drafts.map((draft) =>
    draft.id === parentId
      ? { ...draft, fields: [...draft.fields, newDraftField()] }
      : draft.fields.length
        ? { ...draft, fields: addDraftUnder(draft.fields, parentId) }
        : draft
  );
};

export const moveDraftAt = (
  drafts: DraftField[],
  id: string,
  offset: number
): DraftField[] => {
  const index = drafts.findIndex((draft) => draft.id === id);
  if (index >= 0) return move(drafts, index, index + offset);

  return drafts.map((draft) =>
    draft.fields.length
      ? { ...draft, fields: moveDraftAt(draft.fields, id, offset) }
      : draft
  );
};

export const retypeDraft = (
  draft: DraftField,
  type: SchemaFieldType
): Partial<DraftField> => ({
  type,
  allowedValues: type === "String" ? draft.allowedValues : [],
  elementType: type === "Array" ? (draft.elementType ?? "String") : null,
});

export const parseSchemaFields = (value: unknown): SchemaField[] | null => {
  let raw: unknown = value;

  if (typeof value === "string") {
    try {
      raw = JSON.parse(value);
    } catch {
      return null;
    }
  }

  const read = (candidate: unknown): SchemaField[] | null => {
    if (!Array.isArray(candidate)) return null;

    const fields: SchemaField[] = [];
    for (const entry of candidate) {
      if (entry === null || typeof entry !== "object") return null;
      const record = entry as Record<string, unknown>;
      const pick = (pascal: string, camel: string): unknown =>
        record[pascal] !== undefined ? record[pascal] : record[camel];

      const allowedValues = pick("AllowedValues", "allowedValues");
      const elementType = pick("ElementType", "elementType");
      const members = pick("Fields", "fields");

      const field: SchemaField = {
        name: String(pick("Name", "name") ?? ""),
        type: (pick("Type", "type") as SchemaFieldType) ?? "String",
        isRequired: Boolean(pick("IsRequired", "isRequired")),
        allowedValues: Array.isArray(allowedValues)
          ? allowedValues.map(String)
          : undefined,
        elementType: elementType
          ? (String(elementType) as SchemaFieldType)
          : undefined,
      };

      if (Array.isArray(members)) {
        const nested = read(members);
        if (nested === null) return null;
        field.fields = nested;
      }

      fields.push(field);
    }

    return fields;
  };

  return read(raw);
};

export interface SchemaPath {
  path: string;
  type: SchemaFieldType;
  multiple: boolean;
  isRequired: boolean;
  allowedValues?: string[];
  depth: number;
}

export const schemaPaths = (fields: SchemaField[]): SchemaPath[] => {
  const paths: SchemaPath[] = [];

  const walk = (
    level: SchemaField[],
    prefix: string,
    depth: number,
    multiple: boolean
  ): void => {
    for (const field of level) {
      const path = prefix ? `${prefix}.${field.name}` : field.name;

      paths.push({
        path,
        type: field.type,
        multiple,
        isRequired: field.isRequired,
        allowedValues: field.allowedValues,
        depth,
      });

      if (field.type === "Object") {
        walk(field.fields ?? [], path, depth + 1, multiple);
        continue;
      }

      if (field.type !== "Array") continue;

      const element = field.elementType ?? "String";
      const wildcard = `${path}[*]`;

      paths.push({
        path: wildcard,
        type: element,
        multiple: true,
        isRequired: field.isRequired,
        depth: depth + 1,
      });

      if (element === "Object") {
        walk(field.fields ?? [], wildcard, depth + 2, true);
      }
    }
  };

  walk(fields, "", 0, false);

  return paths;
};

export interface SchemaResolution {
  type: SchemaFieldType;
  multiple: boolean;
  isRequired: boolean;
  allowedValues?: string[];
}

export const resolveSchemaPath = (
  fields: SchemaField[],
  path: string
): SchemaResolution | null => {
  const declared = fields.find((field) => field.name === path);
  if (declared) {
    return {
      type: declared.type,
      multiple: false,
      isRequired: declared.isRequired,
      allowedValues: declared.allowedValues,
    };
  }

  const segments = parsePath(path);
  if (!segments) return null;

  let declarations: SchemaField[] | undefined = fields;
  let current: SchemaField | undefined;
  let type: SchemaFieldType = "Object";
  let multiple = false;

  for (let i = 0; i < segments.length; i++) {
    const segment = segments[i];
    current = declarations?.find((field) => field.name === segment.name);
    if (!current) return null;

    type = current.type;
    declarations = current.fields;

    for (const accessor of segment.accessors) {
      if (type !== "Array") return null;
      type = current.elementType ?? "String";
      multiple ||= accessor.isWildcard;
    }

    if (i < segments.length - 1 && type !== "Object") return null;
  }

  return {
    type,
    multiple,
    isRequired: current?.isRequired ?? false,
    allowedValues: type === "String" ? current?.allowedValues : undefined,
  };
};

export const findSchemaField = (
  fields: SchemaField[],
  name: string
): SchemaField | null => fields.find((field) => field.name === name) ?? null;

const editDistance = (a: string, b: string, limit: number): number => {
  if (Math.abs(a.length - b.length) > limit) return limit + 1;

  let previous = Array.from({ length: b.length + 1 }, (_, i) => i);

  for (let i = 1; i <= a.length; i++) {
    const current = [i];
    let best = i;

    for (let j = 1; j <= b.length; j++) {
      const cost = a[i - 1] === b[j - 1] ? 0 : 1;
      current[j] = Math.min(
        current[j - 1] + 1,
        previous[j] + 1,
        previous[j - 1] + cost
      );
      best = Math.min(best, current[j]);
    }

    if (best > limit) return limit + 1;
    previous = current;
  }

  return previous[b.length];
};

export const closestSchemaPath = (
  fields: SchemaField[],
  name: string
): SchemaPath | null => {
  const candidates = schemaPaths(fields);
  const folded = name.toLowerCase();

  const sameLetters = candidates.find(
    (candidate) => candidate.path.toLowerCase() === folded
  );
  if (sameLetters) return sameLetters;

  if (name.length < 4) return null;

  let best: SchemaPath | null = null;
  let bestDistance = Number.POSITIVE_INFINITY;

  for (const candidate of candidates) {
    const distance = editDistance(folded, candidate.path.toLowerCase(), 2);
    if (distance <= 2 && distance < bestDistance) {
      best = candidate;
      bestDistance = distance;
    }
  }

  return best;
};

export const closestSchemaField = (
  fields: SchemaField[],
  name: string
): SchemaField | null => {
  const match = closestSchemaPath(fields, name);
  if (!match) return null;
  return findSchemaField(fields, match.path);
};

export const schemaPlaceholder = (
  field: SchemaField,
  type: SchemaFieldType,
  now: Date = new Date()
): unknown => {
  switch (type) {
    case "String":
      return field.allowedValues?.[0] ?? "";
    case "Number":
      return 0;
    case "Boolean":
      return false;
    case "DateTime":
      return now.toISOString();
    case "Object":
      return samplePayload(field.fields ?? [], now);
    case "Array":
      return [schemaPlaceholder(field, field.elementType ?? "String", now)];
  }
};

export const valueMatchesType = (
  value: unknown,
  type: SchemaFieldType
): boolean => {
  switch (type) {
    case "String":
    case "DateTime":
      return typeof value === "string";
    case "Number":
      return typeof value === "number" && Number.isFinite(value);
    case "Boolean":
      return typeof value === "boolean";
    case "Object":
      return typeof value === "object" && value !== null && !Array.isArray(value);
    case "Array":
      return Array.isArray(value);
  }
};

export const samplePayload = (
  fields: SchemaField[],
  now: Date = new Date()
): Record<string, unknown> => {
  const payload: Record<string, unknown> = {};

  for (const field of fields) {
    payload[field.name] = field.isRequired
      ? schemaPlaceholder(field, field.type, now)
      : null;
  }

  return payload;
};

export const samplePayloadText = (
  fields: SchemaField[],
  now: Date = new Date()
): string => JSON.stringify(samplePayload(fields, now), null, 2);

export const describeSchemaField = (field: SchemaField): string => {
  const shape =
    field.type === "Array"
      ? `Array of ${field.elementType ?? "?"}`
      : field.type;

  const parts = [shape, field.isRequired ? "required" : "optional"];

  if (field.allowedValues?.length) {
    parts.push(`one of ${field.allowedValues.map((v) => `'${v}'`).join(", ")}`);
  }

  if (field.fields?.length) {
    parts.push(
      `${field.fields.length} ${field.fields.length === 1 ? "member" : "members"}`
    );
  }

  return parts.join(" · ");
};

export type SchemaFieldChange = "added" | "removed" | "changed" | "unchanged";

export interface SchemaFieldDiffRow {
  name: string;
  change: SchemaFieldChange;
  before: SchemaField | null;
  after: SchemaField | null;
  depth: number;
}

export const diffSchemaFields = (
  before: SchemaField[] | null,
  after: SchemaField[] | null
): SchemaFieldDiffRow[] => {
  const rows: SchemaFieldDiffRow[] = [];

  const walk = (
    from: SchemaField[],
    to: SchemaField[],
    prefix: string,
    depth: number
  ): void => {
    const beforeByName = new Map(from.map((f) => [f.name, f]));
    const afterByName = new Map(to.map((f) => [f.name, f]));

    const names = [
      ...afterByName.keys(),
      ...[...beforeByName.keys()].filter((name) => !afterByName.has(name)),
    ];

    for (const name of names) {
      const a = beforeByName.get(name) ?? null;
      const b = afterByName.get(name) ?? null;
      const path = prefix ? `${prefix}.${name}` : name;

      const change: SchemaFieldChange =
        a === null
          ? "added"
          : b === null
            ? "removed"
            : JSON.stringify(a) === JSON.stringify(b)
              ? "unchanged"
              : "changed";

      rows.push({ name: path, change, before: a, after: b, depth });

      if (a && b && (a.fields?.length || b.fields?.length)) {
        walk(a.fields ?? [], b.fields ?? [], path, depth + 1);
      }
    }
  };

  walk(before ?? [], after ?? [], "", 0);

  return rows;
};
