"use client";

import { useEffect, useMemo, useState } from "react";
import { Info, UserRound } from "lucide-react";

import { ApplicationUser, Paged, UserResource } from "@/types";
import { useAuth } from "@/context/AuthContext";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { UserCombobox } from "@/components/UserCombobox";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";
import { API_BASE } from "@/lib/api";

export function AssignCaseDialog({
  open,
  onOpenChange,
  onAssign,
  assigning
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onAssign: (userId?: string) => void | Promise<void>;
  assigning: boolean;
}) {
  const t = useTranslation();
  const { user } = useAuth();
  const [users, setUsers] = useState<ApplicationUser[] | null>(null);
  const [directoryReadable, setDirectoryReadable] = useState(true);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [selected, setSelected] = useState<ApplicationUser | null>(null);
  const [manualUserId, setManualUserId] = useState("");

  useEffect(() => {
    if (!open) return;

    let cancelled = false;
    setLoading(true);

    const load = async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/organizations/my/users?PageNumber=1&PageSize=100&UserName=${encodeURIComponent(searchTerm)}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include"
          }
        );

        if (cancelled) return;

        if (response.ok) {
          const data = (await response.json()) as Paged<UserResource>;
          setUsers(data.data.map((resource) => resource.user));
          setDirectoryReadable(true);
        } else if (response.status === 403 || response.status === 404) {
          setDirectoryReadable(false);
        } else {
          setUsers([]);
        }
      } catch {
        if (!cancelled) setUsers([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    load();
    return () => {
      cancelled = true;
    };
  }, [open, searchTerm]);

  useEffect(() => {
    if (!open) {
      setManualUserId("");
      setSelected(null);
    }
  }, [open]);

  const candidates = useMemo(
    () => users?.filter((candidate) => candidate.userId !== user?.userId) ?? [],
    [users, user?.userId]
  );

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t("assignCaseDialog.assignCase")}</DialogTitle>
        </DialogHeader>

        <Button
          variant="outline"
          className="w-full justify-start gap-2"
          onClick={() => onAssign()}
          disabled={assigning}
        >
          <UserRound className="size-4" />
          {t("assignCaseDialog.assignToMe")}
        </Button>

        <div className="flex items-center gap-3">
          <span className="h-px flex-1 bg-slate-200" />
          <span className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
            or someone else
          </span>
          <span className="h-px flex-1 bg-slate-200" />
        </div>

        {directoryReadable ? (
          <UserCombobox
            users={candidates}
            loading={loading}
            value={selected}
            onChange={setSelected}
            onSearchTermChange={setSearchTerm}
            onSubmit={() => selected && onAssign(selected.userId)}
            disabled={assigning}
            emptyLabel={t("assignCaseDialog.noOneElseInThis")}
          />
        ) : (
          <div className="space-y-3">
            <div className="flex items-start gap-2.5 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2.5">
              <Info className="mt-0.5 size-4 flex-shrink-0 text-slate-500" />
              <p className="text-sm text-slate-600">
                The organization&apos;s user list isn&apos;t available to this
                account, so paste the id of the person you want to assign this
                case to.
              </p>
            </div>
            <Input
              value={manualUserId}
              onChange={(e) => setManualUserId(e.target.value)}
              placeholder={t("assignCaseDialog.userId")}
              className="font-mono text-sm"
            />
          </div>
        )}

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={assigning}
          >
            {t("common.cancel")}
          </Button>
          {directoryReadable ? (
            <Button
              onClick={() => selected && onAssign(selected.userId)}
              disabled={assigning || !selected}
              className="max-w-[15rem]"
            >
              <span className="min-w-0 truncate">
                {assigning
                  ? "Assigning..."
                  : selected
                    ? `Assign to ${selected.userName}`
                    : "Assign"}
              </span>
            </Button>
          ) : (
            <Button
              onClick={() => onAssign(manualUserId.trim())}
              disabled={assigning || !manualUserId.trim()}
            >
              {assigning ? "Assigning..." : "Assign"}
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
