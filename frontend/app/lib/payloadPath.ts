export const MAX_SEGMENTS = 8;

const SEGMENT_SEPARATOR = ".";
const ACCESSOR_OPEN = "[";
const ACCESSOR_CLOSE = "]";
const WILDCARD = "*";

export interface PathAccessor {
  index: number | null;
  isWildcard: boolean;
}

export interface PathSegment {
  name: string;
  accessors: PathAccessor[];
}

const parseSegment = (part: string): PathSegment | null => {
  const open = part.indexOf(ACCESSOR_OPEN);
  const name = open < 0 ? part : part.slice(0, open);

  if (
    !name.trim() ||
    name.includes(ACCESSOR_CLOSE) ||
    name.includes(WILDCARD)
  ) {
    return null;
  }

  const accessors: PathAccessor[] = [];
  let position = open < 0 ? part.length : open;

  while (position < part.length) {
    if (part[position] !== ACCESSOR_OPEN) return null;

    const close = part.indexOf(ACCESSOR_CLOSE, position);
    if (close < 0) return null;

    const inner = part.slice(position + 1, close);

    if (inner === WILDCARD) {
      accessors.push({ index: null, isWildcard: true });
    } else if (/^\d+$/.test(inner)) {
      accessors.push({ index: Number(inner), isWildcard: false });
    } else {
      return null;
    }

    position = close + 1;
  }

  return { name, accessors };
};

export const parsePath = (path: string): PathSegment[] | null => {
  if (!path || !path.trim()) return null;

  const segments: PathSegment[] = [];

  for (const part of path.split(SEGMENT_SEPARATOR)) {
    const segment = parseSegment(part);
    if (!segment) return null;
    segments.push(segment);
  }

  return segments.length <= MAX_SEGMENTS ? segments : null;
};

export const isAddressable = (path: string): boolean =>
  Boolean(path && path.trim()) &&
  (!path.includes(WILDCARD) || parsePath(path) !== null);

export const hasWildcard = (path: string): boolean => {
  const segments = parsePath(path);
  if (!segments) return false;
  return segments.some((segment) =>
    segment.accessors.some((accessor) => accessor.isWildcard)
  );
};

export const isNested = (path: string): boolean => {
  const segments = parsePath(path);
  if (!segments) return false;
  return segments.length > 1 || segments[0].accessors.length > 0;
};

const isRecord = (value: unknown): value is Record<string, unknown> =>
  value !== null && typeof value === "object" && !Array.isArray(value);

const index = (nodes: unknown[], accessor: PathAccessor): unknown[] => {
  const indexed: unknown[] = [];

  for (const node of nodes) {
    if (!Array.isArray(node)) continue;

    if (accessor.isWildcard) {
      indexed.push(...node);
      continue;
    }

    if (accessor.index !== null && accessor.index < node.length) {
      indexed.push(node[accessor.index]);
    }
  }

  return indexed;
};

const descend = (nodes: unknown[], segment: PathSegment): unknown[] => {
  let resolved: unknown[] = [];

  for (const node of nodes) {
    if (!isRecord(node)) continue;
    if (!Object.prototype.hasOwnProperty.call(node, segment.name)) continue;
    resolved.push(node[segment.name]);
  }

  for (const accessor of segment.accessors) {
    resolved = index(resolved, accessor);
  }

  return resolved;
};

export const resolveAll = (
  payload: Record<string, unknown> | null | undefined,
  path: string
): unknown[] => {
  if (!payload) return [];

  // The literal key first, exactly as the engine does it.
  if (Object.prototype.hasOwnProperty.call(payload, path)) {
    return [payload[path]];
  }

  const segments = parsePath(path);
  if (!segments) return [];

  let current: unknown[] = [payload];

  for (const segment of segments) {
    current = descend(current, segment);
    if (current.length === 0) return current;
  }

  return current;
};

export const isPresent = (
  payload: Record<string, unknown> | null | undefined,
  path: string
): boolean => resolveAll(payload, path).length > 0;

export const generalisePath = (path: string): string =>
  path.replace(/\[\d+\]/g, "[*]");
