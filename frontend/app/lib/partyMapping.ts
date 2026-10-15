import {
  PartyAttributePath,
  PartyMappingEntry,
  PartyMappingUsage,
  PartyRole,
  SchemaField,
  SchemaFieldType,
  SchemaViolation,
} from "@/types";
import { parsePath } from "@/lib/payloadPath";
import {
  SchemaPath,
  resolveSchemaPath,
  schemaPaths,
} from "@/lib/transactionSchema";

export interface DraftAttribute {
  id: string;
  name: string;
  path: string;
}

export interface DraftMapping {
  id: string;
  role: PartyRole;
  path: string;
  identifierPath: string;
  displayNamePath: string;
  attributes: DraftAttribute[];
}

let nextDraftId = 0;
const draftId = (prefix: string): string => `${prefix}-${nextDraftId++}`;

export const newDraftMapping = (role: PartyRole): DraftMapping => ({
  id: draftId("mapping"),
  role,
  path: "",
  identifierPath: "",
  displayNamePath: "",
  attributes: [],
});

export const newDraftAttribute = (): DraftAttribute => ({
  id: draftId("attribute"),
  name: "",
  path: "",
});

export const toDrafts = (mappings: PartyMappingEntry[]): DraftMapping[] =>
  mappings.map((mapping) => ({
    id: draftId("mapping"),
    role: mapping.role,
    path: mapping.path ?? "",
    identifierPath: mapping.identifierPath ?? "",
    displayNamePath: mapping.displayNamePath ?? "",
    attributes: (mapping.attributePaths ?? []).map((attribute) => ({
      id: draftId("attribute"),
      name: attribute.name,
      path: attribute.path,
    })),
  }));

export const toRequestMappings = (
  drafts: DraftMapping[]
): PartyMappingEntry[] =>
  drafts.map((draft) => {
    const entry: PartyMappingEntry = {
      role: draft.role,
      identifierPath: draft.identifierPath.trim(),
    };

    const path = draft.path.trim();
    if (path) entry.path = path;

    const displayNamePath = draft.displayNamePath.trim();
    if (displayNamePath) entry.displayNamePath = displayNamePath;

    const attributes = draft.attributes
      .filter((attribute) => attribute.name.trim() || attribute.path.trim())
      .map<PartyAttributePath>((attribute) => ({
        name: attribute.name.trim(),
        path: attribute.path.trim(),
      }));

    if (attributes.length) entry.attributePaths = attributes;

    return entry;
  });

export const sameMappings = (
  a: PartyMappingEntry[],
  b: PartyMappingEntry[]
): boolean => JSON.stringify(a) === JSON.stringify(b);

export const isManyParty = (
  entry: Pick<PartyMappingEntry, "path"> | null | undefined
): boolean => (entry?.path ?? "").includes("[*]");

export const entryForRole = (
  mappings: PartyMappingEntry[] | null | undefined,
  role: string
): PartyMappingEntry | null =>
  mappings?.find((mapping) => mapping.role === role) ?? null;

export const declaredRoles = (
  mappings: PartyMappingEntry[] | null | undefined
): PartyRole[] => (mappings ?? []).map((mapping) => mapping.role);

export const absolutePath = (
  entry: Pick<PartyMappingEntry, "path">,
  relative: string | null | undefined
): string => {
  const relativePath = (relative ?? "").trim();
  if (!relativePath) return "";
  const base = (entry.path ?? "").trim();
  return base ? `${base}.${relativePath}` : relativePath;
};

export const partyContainerPaths = (
  fields: SchemaField[] | null | undefined
): SchemaPath[] =>
  fields ? schemaPaths(fields).filter((path) => path.type === "Object") : [];

export const PARTY_MEMBER_TYPES: SchemaFieldType[] = ["String", "Number"];

export const isPartyMemberType = (type: SchemaFieldType): boolean =>
  PARTY_MEMBER_TYPES.includes(type);

export const partyMemberPaths = (
  fields: SchemaField[] | null | undefined,
  basePath: string
): SchemaPath[] => {
  if (!fields) return [];

  const all = schemaPaths(fields).filter((path) => isPartyMemberType(path.type));
  const base = basePath.trim();

  if (!base) {
    return all.filter((path) => !path.path.includes("[*]"));
  }

  const prefix = `${base}.`;

  return all
    .filter((path) => path.path.startsWith(prefix))
    .map((path) => ({
      ...path,
      path: path.path.slice(prefix.length),
      depth: Math.max(0, path.depth - base.split(".").length),
    }))
    .filter((path) => !path.path.includes("[*]"));
};

export const isUndeclaredPath = (
  fields: SchemaField[] | null | undefined,
  path: string
): boolean | null => {
  if (!fields) return null;
  const trimmed = path.trim();
  if (!trimmed) return null;
  return resolveSchemaPath(fields, trimmed) === null;
};

export const memberSchemaProblem = (
  fields: SchemaField[] | null | undefined,
  absolute: string,
  purpose: "identify" | "name" | "describe"
): string | null => {
  if (!fields) return null;
  const trimmed = absolute.trim();
  if (!trimmed) return null;

  const resolution = resolveSchemaPath(fields, trimmed);
  if (!resolution) {
    return "The transaction schema declares nothing here, and the API refuses a mapping that reads a path the schema does not have.";
  }
  if (!isPartyMemberType(resolution.type)) {
    return `The schema declares this as ${resolution.type}, which cannot ${purpose} a party — only a string or a number can.`;
  }
  return null;
};

export const describeMappingEntry = (entry: PartyMappingEntry): string => {
  const parts = [
    entry.path?.trim() ? entry.path.trim() : "the payload itself",
    `identified by ${entry.identifierPath}`,
  ];

  if (entry.displayNamePath?.trim()) {
    parts.push(`named by ${entry.displayNamePath.trim()}`);
  }

  const attributes = entry.attributePaths?.length ?? 0;
  if (attributes) {
    parts.push(`${attributes} ${attributes === 1 ? "attribute" : "attributes"}`);
  }

  if (isManyParty(entry)) parts.push("many per payload");

  return parts.join(" · ");
};

export type MappingField =
  | "role"
  | "path"
  | "identifierPath"
  | "displayNamePath";

export interface DraftValidation {
  rowErrors: Record<string, Partial<Record<MappingField, string>>>;
  attributeErrors: Record<string, string>;
  formError: string | null;
}

const WILDCARD = "[*]";

const walkable = (path: string): boolean => parsePath(path) !== null;

const checkRelative = (
  path: string,
  what: string
): string | null => {
  if (path.includes(WILDCARD) || path.includes("*")) {
    return `${what} is read inside the party, so it cannot carry a wildcard. Put the [*] on the party's own path instead.`;
  }
  if (!walkable(path)) {
    return `${what} is not a path the engine can read. Members are addressed as account.iban.`;
  }
  return null;
};

export const validateDrafts = (drafts: DraftMapping[]): DraftValidation => {
  const rowErrors: DraftValidation["rowErrors"] = {};
  const attributeErrors: Record<string, string> = {};
  let formError: string | null = null;

  if (drafts.length === 0) {
    formError =
      "A party mapping must declare at least one role. To stop resolving parties altogether, withdraw the mapping instead.";
  }

  const mark = (id: string, field: MappingField, message: string) => {
    rowErrors[id] ??= {};
    rowErrors[id][field] ??= message;
  };

  const seenRoles = new Set<PartyRole>();

  for (const draft of drafts) {
    if (seenRoles.has(draft.role)) {
      mark(
        draft.id,
        "role",
        `${draft.role} is already declared above. A mapping declares each role at most once — one party can stand in several roles, but a role is resolved from one place.`
      );
    }
    seenRoles.add(draft.role);

    const path = draft.path.trim();
    if (path) {
      if (!walkable(path)) {
        mark(
          draft.id,
          "path",
          "This is not a path the engine can read. A list of parties is addressed as parties[*]."
        );
      } else if (path.includes("*") && !path.includes(WILDCARD)) {
        mark(
          draft.id,
          "path",
          "A wildcard goes inside brackets, as parties[*]."
        );
      }
    }

    const identifierPath = draft.identifierPath.trim();
    if (!identifierPath) {
      mark(
        draft.id,
        "identifierPath",
        "A party has to be identified by something — the identifier is its identity, and a party cannot be resolved without one."
      );
    } else {
      const problem = checkRelative(identifierPath, "The identifier");
      if (problem) mark(draft.id, "identifierPath", problem);
    }

    const displayNamePath = draft.displayNamePath.trim();
    if (displayNamePath) {
      const problem = checkRelative(displayNamePath, "The display name");
      if (problem) mark(draft.id, "displayNamePath", problem);
    }

    const seenAttributes = new Set<string>();
    for (const attribute of draft.attributes) {
      const name = attribute.name.trim();
      const attributePath = attribute.path.trim();

      if (!name && !attributePath) continue;

      if (!name) {
        attributeErrors[attribute.id] = "Give the attribute a name.";
      } else if (seenAttributes.has(name)) {
        attributeErrors[attribute.id] =
          `'${name}' is already collected for this role.`;
      } else if (!attributePath) {
        attributeErrors[attribute.id] =
          "Say where in the party this attribute is read from.";
      } else {
        const problem = checkRelative(attributePath, "An attribute");
        if (problem) attributeErrors[attribute.id] = problem;
      }

      if (name) seenAttributes.add(name);
    }
  }

  return { rowErrors, attributeErrors, formError };
};

export const hasErrors = (validation: DraftValidation): boolean =>
  validation.formError !== null ||
  Object.keys(validation.attributeErrors).length > 0 ||
  Object.values(validation.rowErrors).some(
    (row) => Object.keys(row).length > 0
  );

export type BreakageReason = "dropped" | "widened";

export interface Breakage {
  usage: PartyMappingUsage;
  role: PartyRole;
  reason: BreakageReason;
}

export const breakagesOf = (
  proposed: PartyMappingEntry[],
  usages: PartyMappingUsage[],
  current: PartyMappingEntry[] | null
): Breakage[] => {
  const breakages: Breakage[] = [];

  for (const usage of usages) {
    for (const role of usage.roles) {
      const next = entryForRole(proposed, role);

      if (!next) {
        breakages.push({ usage, role, reason: "dropped" });
        continue;
      }

      const before = entryForRole(current, role);
      if (before && !isManyParty(before) && isManyParty(next)) {
        breakages.push({ usage, role, reason: "widened" });
      }
    }
  }

  return breakages;
};

export const describeBreakage = (breakage: Breakage): string =>
  breakage.reason === "dropped"
    ? `reads ${breakage.role}, which this save would stop declaring`
    : `reads ${breakage.role}, which this save would turn into a list — the rule would silently start reading only the first party`;

export const parsePartyMappings = (
  value: unknown
): PartyMappingEntry[] | null => {
  let raw: unknown = value;

  if (typeof value === "string") {
    try {
      raw = JSON.parse(value);
    } catch {
      return null;
    }
  }

  if (!Array.isArray(raw)) return null;

  const entries: PartyMappingEntry[] = [];

  for (const candidate of raw) {
    if (candidate === null || typeof candidate !== "object") return null;
    const record = candidate as Record<string, unknown>;
    const pick = (pascal: string, camel: string): unknown =>
      record[pascal] !== undefined ? record[pascal] : record[camel];

    const attributePaths = pick("AttributePaths", "attributePaths");
    const path = pick("Path", "path");
    const displayNamePath = pick("DisplayNamePath", "displayNamePath");

    const entry: PartyMappingEntry = {
      role: String(pick("Role", "role") ?? "") as PartyRole,
      identifierPath: String(pick("IdentifierPath", "identifierPath") ?? ""),
    };

    if (path) entry.path = String(path);
    if (displayNamePath) entry.displayNamePath = String(displayNamePath);

    if (Array.isArray(attributePaths)) {
      entry.attributePaths = attributePaths.map((attribute) => {
        const attributeRecord = (attribute ?? {}) as Record<string, unknown>;
        return {
          name: String(
            attributeRecord.Name ?? attributeRecord.name ?? ""
          ),
          path: String(
            attributeRecord.Path ?? attributeRecord.path ?? ""
          ),
        };
      });
    }

    entries.push(entry);
  }

  return entries;
};

export type MappingChange = "added" | "removed" | "changed" | "unchanged";

export interface MappingDiffRow {
  role: string;
  change: MappingChange;
  before: PartyMappingEntry | null;
  after: PartyMappingEntry | null;
}

export const diffPartyMappings = (
  before: PartyMappingEntry[] | null,
  after: PartyMappingEntry[] | null
): MappingDiffRow[] => {
  const beforeByRole = new Map((before ?? []).map((e) => [e.role, e]));
  const afterByRole = new Map((after ?? []).map((e) => [e.role, e]));

  const roles = [
    ...afterByRole.keys(),
    ...[...beforeByRole.keys()].filter((role) => !afterByRole.has(role)),
  ];

  return roles.map((role) => {
    const a = beforeByRole.get(role) ?? null;
    const b = afterByRole.get(role) ?? null;

    const change: MappingChange =
      a === null
        ? "added"
        : b === null
          ? "removed"
          : JSON.stringify(a) === JSON.stringify(b)
            ? "unchanged"
            : "changed";

    return { role, change, before: a, after: b };
  });
};

export const updateDraftAt = (
  drafts: DraftMapping[],
  id: string,
  updates: Partial<DraftMapping>
): DraftMapping[] =>
  drafts.map((draft) => (draft.id === id ? { ...draft, ...updates } : draft));

export const removeDraftAt = (
  drafts: DraftMapping[],
  id: string
): DraftMapping[] => drafts.filter((draft) => draft.id !== id);

export const updateAttributeAt = (
  drafts: DraftMapping[],
  mappingId: string,
  attributeId: string,
  updates: Partial<DraftAttribute>
): DraftMapping[] =>
  drafts.map((draft) =>
    draft.id === mappingId
      ? {
          ...draft,
          attributes: draft.attributes.map((attribute) =>
            attribute.id === attributeId
              ? { ...attribute, ...updates }
              : attribute
          ),
        }
      : draft
  );

export const addAttributeTo = (
  drafts: DraftMapping[],
  mappingId: string
): DraftMapping[] =>
  drafts.map((draft) =>
    draft.id === mappingId
      ? { ...draft, attributes: [...draft.attributes, newDraftAttribute()] }
      : draft
  );

export const removeAttributeFrom = (
  drafts: DraftMapping[],
  mappingId: string,
  attributeId: string
): DraftMapping[] =>
  drafts.map((draft) =>
    draft.id === mappingId
      ? {
          ...draft,
          attributes: draft.attributes.filter(
            (attribute) => attribute.id !== attributeId
          ),
        }
      : draft
  );

// Violations name the joined absolute path, so match by recomputing the join for every box.
// A bad `path` produces one violation per child; those are collapsed onto the path box.

type BoxPurpose = "identify" | "name" | "describe";

interface CheckedBox {
  draftId: string;
  field: MappingField | "attribute";
  attributeId?: string;
  combined: string;
  purpose: BoxPurpose;
}

const checkedBoxes = (drafts: DraftMapping[]): CheckedBox[] => {
  const boxes: CheckedBox[] = [];

  for (const draft of drafts) {
    const base = draft.path.trim();
    const combine = (relative: string): string =>
      base ? `${base}.${relative}` : relative;

    const identifier = draft.identifierPath.trim();
    if (identifier) {
      boxes.push({
        draftId: draft.id,
        field: "identifierPath",
        combined: combine(identifier),
        purpose: "identify",
      });
    }

    const displayName = draft.displayNamePath.trim();
    if (displayName) {
      boxes.push({
        draftId: draft.id,
        field: "displayNamePath",
        combined: combine(displayName),
        purpose: "name",
      });
    }

    for (const attribute of draft.attributes) {
      const name = attribute.name.trim();
      const path = attribute.path.trim();
      if (!name && !path) continue;
      if (!path) continue;
      boxes.push({
        draftId: draft.id,
        field: "attribute",
        attributeId: attribute.id,
        combined: combine(path),
        purpose: "describe",
      });
    }
  }

  return boxes;
};

const PURPOSE_PHRASE: Record<BoxPurpose, string> = {
  identify: "cannot identify a party",
  name: "cannot name a party",
  describe: "cannot describe a party",
};

const purposeOf = (message: string): BoxPurpose | null => {
  for (const [purpose, phrase] of Object.entries(PURPOSE_PHRASE)) {
    if (message.includes(phrase)) return purpose as BoxPurpose;
  }
  return null;
};

export interface MappingViolationPlacement {
  rowErrors: Record<string, Partial<Record<MappingField, string>>>;
  attributeErrors: Record<string, string>;
  unplaced: string[];
}

export const placeMappingViolations = (
  drafts: DraftMapping[],
  violations: SchemaViolation[],
  fields: SchemaField[] | null
): MappingViolationPlacement => {
  const boxes = checkedBoxes(drafts);

  const byPath = new Map<string, CheckedBox[]>();
  for (const box of boxes) {
    const existing = byPath.get(box.combined);
    if (existing) existing.push(box);
    else byPath.set(box.combined, [box]);
  }

  const marks = new Map<CheckedBox, string[]>();
  const unplaced: string[] = [];

  for (const violation of violations) {
    const candidates = byPath.get(violation.field) ?? [];

    if (candidates.length === 0) {
      unplaced.push(`${violation.field} ${violation.message}`);
      continue;
    }

    const purpose = purposeOf(violation.message);
    const targets =
      candidates.length > 1 && purpose
        ? candidates.filter((box) => box.purpose === purpose)
        : candidates;

    for (const box of targets.length > 0 ? targets : candidates) {
      const existing = marks.get(box);
      const text = `${violation.field} ${violation.message}`;
      if (existing) existing.push(text);
      else marks.set(box, [text]);
    }
  }

  const collapsed = new Set<string>();

  if (fields) {
    for (const draft of drafts) {
      const base = draft.path.trim();
      if (!base) continue;

      const own = boxes.filter((box) => box.draftId === draft.id);
      if (own.length === 0) continue;
      if (!own.every((box) => marks.has(box))) continue;
      if (isUndeclaredPath(fields, base) !== true) continue;

      collapsed.add(draft.id);
      for (const box of own) marks.delete(box);
    }
  }

  const rowErrors: MappingViolationPlacement["rowErrors"] = {};
  const attributeErrors: Record<string, string> = {};

  for (const draftId of collapsed) {
    rowErrors[draftId] = {
      path: "The transaction schema declares nothing here, so every path read inside it was refused. Fix this one and the rest resolve.",
    };
  }

  for (const [box, messages] of marks) {
    const text = messages.join(" ");
    if (box.field === "attribute" && box.attributeId) {
      attributeErrors[box.attributeId] = text;
      continue;
    }
    rowErrors[box.draftId] ??= {};
    rowErrors[box.draftId][box.field as MappingField] = text;
  }

  return { rowErrors, attributeErrors, unplaced };
};
