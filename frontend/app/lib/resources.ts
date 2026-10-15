import { ApplicationUser, Rule } from "@/types";

export const byId = <T>(
  records: T[] | undefined,
  key: (record: T) => string
): ReadonlyMap<string, T> =>
  new Map((records ?? []).map((record) => [key(record), record]));

export const rulesById = (rules: Rule[] | undefined) =>
  byId(rules, (rule) => rule.ruleId);

export const usersById = (users: ApplicationUser[] | undefined) =>
  byId(users, (user) => user.userId);

export type RelationState = "none" | "withheld" | "resolved";

export const relationState = (
  id: string | null | undefined,
  record: unknown
): RelationState => {
  if (!id) return "none";
  return record ? "resolved" : "withheld";
};

export const shortId = (id: string): string => id.slice(0, 8);

export const relationText = (
  name: string | null | undefined,
  id: string | null | undefined,
  noneText = "—"
): string => {
  if (!id) return noneText;
  return name || shortId(id);
};

export const userName = (user: ApplicationUser | undefined): string | null =>
  user ? user.email || user.userName || null : null;
