"use client";

import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";
import { RotateCw, UserPlus, XCircle } from "lucide-react";

import {
  Invitation,
  InvitationResource,
  IssuedInvitationResource,
  Paged,
} from "@/types";
import { reportApiError, unwrap } from "@/lib/api";
import {
  invitationsPath,
  isActionable,
  needsResend,
  readInvitationAlreadyMember,
  readInvitationNotPending,
  resendInvitationPath,
  revokeInvitationPath,
  roleName,
  wasReissued,
} from "@/lib/invitations";
import { apiFetch } from "@/lib/http";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { relationText, userName } from "@/lib/resources";
import { useFormat } from "@/lib/useFormat";
import { usePermissions } from "@/context/PermissionsContext";
import { useTranslation } from "@/context/LocalizationContext";
import Pagination from "@/components/Pagination";
import { PageSizeSelect } from "@/components/PageSizeSelect";
import { StatusBadge } from "@/components/StatusBadge";
import { TabBar } from "@/components/TabBar";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { HomeUnitCell } from "@/components/subOrganizations/SubOrganizationParts";

import { InviteLinkDialog } from "./InviteLinkDialog";
import { InviteMemberDialog, InviteRequest } from "./InviteMemberDialog";

const PENDING = "Pending";
const ALL = "All";
const TABS = [PENDING, ALL];

const COLUMNS = 7;

export function InvitationsPanel({
  organizationId,
  organizationScoped = false,
  onUnauthorized,
}: {
  organizationId: string;
  organizationScoped?: boolean;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const format = useFormat();
  const { holds } = usePermissions();
  const canWrite = holds("invitations:write");

  const scope = organizationScoped ? organizationId : undefined;

  const [invitations, setInvitations] = useState<InvitationResource[]>([]);
  const [tab, setTab] = useState<string>(PENDING);
  const [email, setEmail] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);

  const [inviteOpen, setInviteOpen] = useState(false);
  const [inviting, setInviting] = useState(false);
  const [revokeTarget, setRevokeTarget] = useState<Invitation | null>(null);
  const [resending, setResending] = useState<string | null>(null);

  const [issued, setIssued] = useState<{
    acceptUrl: string;
    email: string;
    reissued: boolean;
  } | null>(null);

  const intent = useIdempotencyKey();

  const fetchInvitations = useCallback(
    async (page: number, size: number, term: string, includeResolved: boolean) => {
      setLoading(true);

      try {
        const params = new URLSearchParams({
          pageNumber: String(page),
          pageSize: String(size),
        });
        if (term.length) params.set("email", term);
        if (includeResolved) params.set("includeResolved", "true");

        const response = await apiFetch(`${invitationsPath(scope)}?${params}`, {
          method: "GET",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        });

        if (response.ok) {
          const listing = (await response.json()) as Paged<InvitationResource>;
          setInvitations(listing.data);
          setTotalPages(listing.metadata.pageCount);
        } else {
          await reportApiError(response, {
            fallback: t("invitations.errorFetching"),
            onUnauthorized,
            toastId: "error-fetching-invitations",
          });
        }
      } catch {
        toast.error(t("invitations.errorFetching"), {
          id: "error-fetching-invitations",
        });
      } finally {
        setLoading(false);
      }
    },
    [scope, onUnauthorized, t]
  );

  const reload = useCallback(
    () => fetchInvitations(pageNumber, pageSize, email, tab === ALL),
    [fetchInvitations, pageNumber, pageSize, email, tab]
  );

  useEffect(() => {
    void fetchInvitations(pageNumber, pageSize, email, tab === ALL);
  }, [fetchInvitations, pageNumber, pageSize, email, tab]);

  const reportConflict = async (response: Response): Promise<boolean> => {
    let body: unknown = null;
    try {
      body = await response.clone().json();
    } catch {
      return false;
    }

    const conflict = readInvitationNotPending(body);
    if (!conflict) return false;

    const collidedWith = conflict.invitationStatus;

    toast.error(
      collidedWith === "Accepted"
        ? t("invitations.conflict.accepted")
        : collidedWith === "Revoked"
          ? t("invitations.conflict.revoked")
          : t("invitations.noLongerPending"),
      {
        id: "invitation-not-pending",
        description:
          collidedWith === "Accepted"
            ? t("invitations.conflict.acceptedHelp")
            : collidedWith === "Revoked"
              ? t("invitations.conflict.revokedHelp")
              : t("invitations.noLongerPendingHelp"),
      }
    );

    void reload();
    return true;
  };

  const handleInvite = async (request: InviteRequest) => {
    setInviting(true);

    try {
      const response = await apiFetch(
        invitationsPath(scope),
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(request),
        },
        { idempotencyKey: intent.current("invite") }
      );

      if (response.ok) {
        intent.completed("invite");

        const resource = await unwrap<IssuedInvitationResource>(response);
        const reissued = wasReissued(resource.invitation);

        toast.success(
          reissued
            ? t("invitations.resent", resource.invitation.email)
            : t("invitations.sent", resource.invitation.email)
        );

        setInviteOpen(false);
        // Held back until the link has been acknowledged: closing this dialog
        // is the last moment the URL exists anywhere.
        setIssued({
          acceptUrl: resource.invitation.acceptUrl,
          email: resource.invitation.email,
          reissued,
        });
        void reload();
        return;
      }

      if (response.status === 409) {
        let body: unknown = null;
        try {
          body = await response.clone().json();
        } catch {
        }

        const member = readInvitationAlreadyMember(body);
        if (member?.sameOrganization === true) {
          toast.error(t("invitations.alreadyHere"), {
            description: t("invitations.alreadyHereHelp"),
          });
          return;
        }

        if (member?.sameOrganization === false) {
          toast.error(t("invitations.belongsElsewhere"), {
            description: t("invitations.belongsElsewhereHelp"),
          });
          return;
        }
      }

      await reportApiError(response, {
        fallback: t("invitations.errorInviting"),
        onUnauthorized,
        fieldLabels: { Role: t("invitations.invite.role") },
      });
    } catch {
      toast.error(t("invitations.errorInviting"));
    } finally {
      setInviting(false);
    }
  };

  const handleResend = async (invitation: Invitation) => {
    setResending(invitation.invitationId);
    const key = `resend:${invitation.invitationId}`;

    try {
      const response = await apiFetch(
        resendInvitationPath(invitation.invitationId, scope),
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: intent.current(key) }
      );

      if (response.ok) {
        intent.completed(key);

        const resource = await unwrap<IssuedInvitationResource>(response);
        toast.success(t("invitations.resent", resource.invitation.email));
        setIssued({
          acceptUrl: resource.invitation.acceptUrl,
          email: resource.invitation.email,
          reissued: true,
        });
        void reload();
        return;
      }

      if (response.status === 409 && (await reportConflict(response))) return;

      await reportApiError(response, {
        fallback: t("invitations.errorResending"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("invitations.errorResending"));
    } finally {
      setResending(null);
    }
  };

  const handleRevoke = async (invitation: Invitation) => {
    setRevokeTarget(null);
    const key = `revoke:${invitation.invitationId}`;

    try {
      const response = await apiFetch(
        revokeInvitationPath(invitation.invitationId, scope),
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        },
        { idempotencyKey: intent.current(key) }
      );

      if (response.ok) {
        intent.completed(key);
        toast.success(t("invitations.revoked", invitation.email), {
          description: t("invitations.revokedHelp"),
        });
        void reload();
        return;
      }

      if (response.status === 409 && (await reportConflict(response))) return;

      await reportApiError(response, {
        fallback: t("invitations.errorRevoking"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("invitations.errorRevoking"));
    }
  };

  return (
    <div className="space-y-4">
      <TabBar
        tabs={TABS}
        activeTab={tab}
        aria-label={t("invitations.tabs.label")}
        label={(value) =>
          value === PENDING
            ? t("invitations.tabs.pending")
            : t("invitations.tabs.all")
        }
        onChange={(next) => {
          setTab(next);
          setPageNumber(1);
        }}
      />

      <div className="flex items-center justify-between gap-3">
        <Input
          value={email}
          onChange={(event) => {
            setEmail(event.target.value);
            setPageNumber(1);
          }}
          placeholder={t("invitations.searchByEmail")}
          className="w-64"
        />
        <div className="flex items-center gap-3">
          <PageSizeSelect
            value={pageSize}
            onChange={(next) => {
              setPageSize(next);
              setPageNumber(1);
            }}
          />
          {canWrite && (
            <Button
              size="sm"
              className="gap-1.5"
              onClick={() => setInviteOpen(true)}
            >
              <UserPlus className="size-4" />
              {t("invitations.inviteAction")}
            </Button>
          )}
        </div>
      </div>

      <div className="overflow-hidden rounded-xl border border-slate-200 bg-card">
        <Table>
          <TableHeader>
            <TableRow className="bg-slate-50 hover:bg-slate-50">
              <TableHead>{t("common.email")}</TableHead>
              <TableHead>{t("invitations.invite.role")}</TableHead>
              <TableHead className="w-44">
                {t("invitations.invite.businessUnit")}
              </TableHead>
              <TableHead>{t("invitations.invitedBy")}</TableHead>
              <TableHead>{t("common.status")}</TableHead>
              <TableHead>{t("invitations.expires")}</TableHead>
              <TableHead className="w-40">{t("common.actions")}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              Array.from({ length: 5 }).map((_, row) => (
                <TableRow key={row}>
                  {Array.from({ length: COLUMNS }).map((_, cell) => (
                    <TableCell key={cell}>
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : invitations.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={COLUMNS}
                  className="py-14 text-center font-mono text-xs uppercase tracking-widest text-slate-400"
                >
                  {tab === PENDING
                    ? t("invitations.emptyPending")
                    : t("common.noResults")}
                </TableCell>
              </TableRow>
            ) : (
              invitations.map(({ invitation, invitedByUser }) => {
                const lapsed = needsResend(invitation);
                const open = isActionable(invitation);

                return (
                  <TableRow
                    key={invitation.invitationId}
                    className="hover:bg-slate-50/50"
                  >
                    <TableCell className="font-medium text-slate-900">
                      {invitation.email}
                    </TableCell>
                    <TableCell className="font-mono text-xs text-slate-600">
                      {roleName(t, invitation.role)}
                    </TableCell>
                    <TableCell>
                      <HomeUnitCell
                        subOrganizationId={invitation.subOrganizationId}
                      />
                    </TableCell>
                    <TableCell className="text-sm text-slate-600">
                      {invitation.invitedByPlatformOperator
                        ? t("invitations.invitedByOperator")
                        : relationText(
                            userName(invitedByUser),
                            invitation.invitedByUserId
                          )}
                    </TableCell>
                    <TableCell>
                      <StatusBadge status={invitation.status} />
                    </TableCell>
                    <TableCell
                      className={
                        lapsed
                          ? "font-mono text-xs text-slate-400"
                          : "font-mono text-xs text-slate-600"
                      }
                    >
                      <span title={format.instant(invitation.expiresAt)}>
                        {format.relative(invitation.expiresAt)}
                      </span>
                    </TableCell>
                    <TableCell>
                      {canWrite && open && (
                        <div className="flex items-center gap-1">
                          <Button
                            variant={lapsed ? "default" : "outline"}
                            size="sm"
                            className="gap-1.5"
                            disabled={resending === invitation.invitationId}
                            onClick={() => void handleResend(invitation)}
                          >
                            <RotateCw className="size-4" />
                            {t("invitations.resendAction")}
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-critical-500 hover:text-critical-700"
                            title={t("invitations.revokeAction")}
                            onClick={() => setRevokeTarget(invitation)}
                          >
                            <XCircle className="size-4" />
                          </Button>
                        </div>
                      )}
                    </TableCell>
                  </TableRow>
                );
              })
            )}
          </TableBody>
        </Table>
      </div>

      {invitations.length > 0 && (
        <Pagination
          handlePrevPage={() => setPageNumber((page) => Math.max(1, page - 1))}
          pageNumber={pageNumber}
          totalPages={totalPages}
          handleNextPage={() =>
            setPageNumber((page) => Math.min(totalPages, page + 1))
          }
        />
      )}

      <InviteMemberDialog
        open={inviteOpen}
        onOpenChange={setInviteOpen}
        organizationId={organizationId}
        submitting={inviting}
        onSubmit={(request) => void handleInvite(request)}
      />

      <InviteLinkDialog
        acceptUrl={issued?.acceptUrl ?? null}
        email={issued?.email ?? ""}
        reissued={issued?.reissued ?? false}
        onClose={() => setIssued(null)}
      />

      <Dialog
        open={revokeTarget !== null}
        onOpenChange={(open) => !open && setRevokeTarget(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t("invitations.revoke.title")}</DialogTitle>
            <DialogDescription>
              {t("invitations.revoke.description", revokeTarget?.email ?? "")}
            </DialogDescription>
          </DialogHeader>

          <p className="text-sm text-slate-600">
            {t("invitations.revoke.consequence")}
          </p>

          <DialogFooter>
            <Button variant="outline" onClick={() => setRevokeTarget(null)}>
              {t("common.cancel")}
            </Button>
            <Button
              variant="destructive"
              onClick={() => revokeTarget && void handleRevoke(revokeTarget)}
            >
              {t("invitations.revokeAction")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
