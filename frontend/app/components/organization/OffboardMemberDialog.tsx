"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { TriangleAlert } from "lucide-react";

import { ApplicationUser, OffboardedUser, Paged, UserResource } from "@/types";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import { offboardUserPath } from "@/lib/invitations";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { useFormat } from "@/lib/useFormat";
import { useTranslation } from "@/context/LocalizationContext";
import { Button } from "@/components/ui/button";
import { UserCombobox } from "@/components/UserCombobox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

function useSuccessorCandidates(
  enabled: boolean,
  organizationSegment: string,
  excludeUserId: string,
  searchTerm: string
) {
  const [users, setUsers] = useState<ApplicationUser[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!enabled) return;

    let cancelled = false;
    setLoading(true);

    void (async () => {
      try {
        const response = await apiFetch(
          `${API_BASE}/organizations/${organizationSegment}/users?PageNumber=1&PageSize=100&UserName=${encodeURIComponent(searchTerm)}`,
          {
            method: "GET",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
          }
        );

        if (cancelled) return;

        if (response.ok) {
          const page = (await response.json()) as Paged<UserResource>;
          setUsers(
            page.data
              .map((resource) => resource.user)
              .filter((candidate) => candidate.userId !== excludeUserId)
          );
        } else {
          setUsers([]);
        }
      } catch {
        if (!cancelled) setUsers([]);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [enabled, organizationSegment, excludeUserId, searchTerm]);

  return { users, loading };
}

export function OffboardMemberDialog({
  organizationId,
  organizationSegment = "my",
  member,
  onClose,
  onOffboarded,
  onUnauthorized,
}: {
  organizationId: string;
  organizationSegment?: string;
  member: ApplicationUser | null;
  onClose: () => void;
  onOffboarded: () => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const format = useFormat();
  const intent = useIdempotencyKey();

  const [successor, setSuccessor] = useState<ApplicationUser | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<OffboardedUser | null>(null);
  const [handedTo, setHandedTo] = useState<string | null>(null);

  const open = member !== null;

  const candidates = useSuccessorCandidates(
    open && result === null,
    organizationSegment,
    member?.userId ?? "",
    searchTerm
  );

  useEffect(() => {
    if (!open) return;
    setSuccessor(null);
    setSearchTerm("");
    setResult(null);
    setHandedTo(null);
  }, [open, member?.userId]);

  const handleOffboard = async () => {
    if (!member) return;

    setSubmitting(true);
    const key = `offboard:${member.userId}`;

    try {
      const response = await apiFetch(
        offboardUserPath(organizationId, member.userId),
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify({ successorUserId: successor?.userId ?? null }),
        },
        { idempotencyKey: intent.current(key) }
      );

      if (response.ok) {
        intent.completed(key);

        const offboarded = await unwrap<OffboardedUser>(response);
        setHandedTo(successor?.userName ?? null);
        setResult(offboarded);
        toast.success(
          t("offboard.done", member.userName || member.email)
        );
        onOffboarded();
        return;
      }

      await reportApiError(response, {
        fallback: t("offboard.error"),
        onUnauthorized,
        fieldLabels: {
          "Body.SuccessorUserId": t("offboard.successor"),
          SuccessorUserId: t("offboard.successor"),
        },
      });
    } catch {
      toast.error(t("offboard.error"));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="sm:max-w-lg">
        {result === null ? (
          <>
            <DialogHeader>
              <DialogTitle>{t("offboard.title")}</DialogTitle>
              <DialogDescription>
                {t(
                  "offboard.description",
                  member?.userName || member?.email || ""
                )}
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-1.5">
              <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("offboard.successor")}
              </label>
              <UserCombobox
                users={candidates.users}
                loading={candidates.loading}
                value={successor}
                onChange={setSuccessor}
                onSearchTermChange={setSearchTerm}
                disabled={submitting}
                placeholder={t("offboard.successorPlaceholder")}
                emptyLabel={t("offboard.noCandidates")}
              />
              <p className="text-xs text-slate-500">
                {successor
                  ? t("offboard.successorNamed", successor.userName)
                  : t("offboard.successorNone")}
              </p>
            </div>

            <div className="flex items-start gap-2.5 rounded-lg border border-attention-200 bg-attention-50 px-3 py-2.5">
              <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
              <div className="space-y-1.5 text-sm text-attention-700">
                <p>{t("offboard.consequences")}</p>
                <p>{t("offboard.accountSurvives")}</p>
              </div>
            </div>

            <DialogFooter>
              <Button variant="outline" onClick={onClose} disabled={submitting}>
                {t("common.cancel")}
              </Button>
              <Button
                variant="destructive"
                onClick={() => void handleOffboard()}
                disabled={submitting}
              >
                {submitting
                  ? t("offboard.working")
                  : t("offboard.confirm")}
              </Button>
            </DialogFooter>
          </>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>{t("offboard.result.title")}</DialogTitle>
              <DialogDescription>
                {t(
                  "offboard.result.description",
                  member?.userName || member?.email || ""
                )}
              </DialogDescription>
            </DialogHeader>

            <dl className="divide-y divide-slate-200 overflow-hidden rounded-xl border border-slate-200">
              <MovedRow
                label={
                  handedTo
                    ? t("offboard.result.casesTo", handedTo)
                    : t("offboard.result.casesToPool")
                }
                value={format.count(result.reassignedCaseCount)}
              />
              <MovedRow
                label={t("offboard.result.queues")}
                value={format.count(result.queueMembershipCount)}
              />
              <MovedRow
                label={t("offboard.result.roleGrants")}
                value={format.count(result.revokedRoleAssignmentCount)}
              />
              <MovedRow
                label={t("offboard.result.invitations")}
                value={format.count(result.revokedInvitationCount)}
              />
            </dl>

            <p className="text-sm text-slate-600">
              {t("offboard.result.closedCasesStay")}
            </p>

            <DialogFooter>
              <Button onClick={onClose}>{t("common.done")}</Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}

function MovedRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-4 px-4 py-2.5">
      <dt className="text-sm text-slate-600">{label}</dt>
      <dd className="font-mono text-sm font-medium text-slate-900">{value}</dd>
    </div>
  );
}
