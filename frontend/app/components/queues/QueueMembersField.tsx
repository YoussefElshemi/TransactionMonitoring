"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Check, Info, X } from "lucide-react";

import { ApplicationUser, Paged, UserResource } from "@/types";
import { API_BASE } from "@/lib/api";
import { shortId } from "@/lib/resources";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

export function QueueMembersField({
  value,
  onChange,
  disabled = false,
}: {
  value: string[];
  onChange: (userIds: string[]) => void;
  disabled?: boolean;
}) {
  const t = useTranslation();
  const [users, setUsers] = useState<ApplicationUser[] | null>(null);
  const [directoryReadable, setDirectoryReadable] = useState(true);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");

  const fetchUsers = useCallback(async (term: string) => {
    setLoading(true);
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/my/users?PageNumber=1&PageSize=100&UserName=${encodeURIComponent(term)}`,
        { credentials: "include" }
      );

      if (response.ok) {
        const page = (await response.json()) as Paged<UserResource>;
        setUsers(page.data.map((resource) => resource.user));
        setDirectoryReadable(true);
      } else if (response.status === 403 || response.status === 404) {
        setDirectoryReadable(false);
      } else {
        setUsers([]);
      }
    } catch {
      setUsers([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    const timer = setTimeout(() => fetchUsers(search), search ? 300 : 0);
    return () => clearTimeout(timer);
  }, [search, fetchUsers]);

  const namesById = useMemo(
    () =>
      new Map((users ?? []).map((user) => [user.userId, user.userName || user.email])),
    [users]
  );

  const toggle = (userId: string) => {
    onChange(
      value.includes(userId)
        ? value.filter((id) => id !== userId)
        : [...value, userId]
    );
  };

  return (
    <div className="space-y-2">
      <div className="flex flex-wrap items-center gap-1.5">
        {value.map((userId) => (
          <span
            key={userId}
            className="flex items-center gap-1 rounded-full border border-slate-200 bg-slate-50 py-0.5 pl-2.5 pr-1 text-xs text-slate-700"
          >
            {namesById.get(userId) ?? shortId(userId)}
            {!disabled && (
              <button
                type="button"
                onClick={() => toggle(userId)}
                className="rounded-full p-0.5 text-slate-400 hover:bg-slate-200 hover:text-slate-700"
                aria-label={t("queues.removeFromThisQueue")}
              >
                <X className="size-3" />
              </button>
            )}
          </span>
        ))}
        {value.length === 0 && (
          <span className="text-sm text-slate-500">
            Nobody. Cases will still arrive here; under Round robin or Least
            loaded they will sit unassigned until somebody is added.
          </span>
        )}
      </div>

      {directoryReadable ? (
        <>
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder={t("queues.searchPeopleInYourOrganization")}
            disabled={disabled}
            className="max-w-sm"
          />
          <ul className="max-h-56 max-w-sm space-y-0.5 overflow-y-auto rounded-md border border-slate-200 p-1">
            {loading && users === null ? (
              Array.from({ length: 4 }).map((_, index) => (
                <li key={index} className="px-1 py-1.5">
                  <Skeleton className="h-4 w-full" />
                </li>
              ))
            ) : (users?.length ?? 0) === 0 ? (
              <li className="px-1 py-6 text-center text-sm text-slate-500">
                {search
                  ? `Nobody matching "${search}"`
                  : "Nobody else in this organization yet"}
              </li>
            ) : (
              users!.map((user) => {
                const picked = value.includes(user.userId);
                return (
                  <li key={user.userId}>
                    <button
                      type="button"
                      disabled={disabled}
                      onClick={() => toggle(user.userId)}
                      className="flex w-full items-center gap-2 rounded px-1.5 py-1.5 text-left text-sm hover:bg-slate-100 disabled:pointer-events-none disabled:opacity-50"
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
                      <span className="min-w-0 flex-1">
                        <span className="block truncate text-slate-900">
                          {user.userName}
                        </span>
                        <span className="block truncate text-xs text-slate-500">
                          {user.email}
                        </span>
                      </span>
                    </button>
                  </li>
                );
              })
            )}
          </ul>
        </>
      ) : (
        <div className="flex max-w-sm items-start gap-2.5 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2.5">
          <Info className="mt-0.5 size-4 flex-shrink-0 text-slate-500" />
          <p className="text-sm text-slate-600">
            Your organization&apos;s user list isn&apos;t readable from this
            account, so membership can be seen but not changed here.
          </p>
        </div>
      )}
    </div>
  );
}
