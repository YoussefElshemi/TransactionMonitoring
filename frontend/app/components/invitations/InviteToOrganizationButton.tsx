"use client";

import { useState } from "react";
import { toast } from "sonner";
import { UserPlus } from "lucide-react";

import { IssuedInvitationResource } from "@/types";
import { reportApiError, unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import {
  organizationInvitationsPath,
  readInvitationAlreadyMember,
  wasReissued,
} from "@/lib/invitations";
import { useIdempotencyKey } from "@/lib/useIdempotencyKey";
import { useTranslation } from "@/context/LocalizationContext";
import { Button } from "@/components/ui/button";

import { InviteLinkDialog } from "./InviteLinkDialog";
import { InviteMemberDialog, InviteRequest } from "./InviteMemberDialog";

export function InviteToOrganizationButton({
  organizationId,
  organizationName,
  onUnauthorized,
  onInvited,
}: {
  organizationId: string;
  organizationName: string;
  onUnauthorized: () => void;
  onInvited?: () => void;
}) {
  const t = useTranslation();
  const intent = useIdempotencyKey();

  const [open, setOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [issued, setIssued] = useState<{
    acceptUrl: string;
    email: string;
    reissued: boolean;
  } | null>(null);

  const handleInvite = async (request: InviteRequest) => {
    setSubmitting(true);

    try {
      const response = await apiFetch(
        organizationInvitationsPath(organizationId),
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          credentials: "include",
          body: JSON.stringify(request),
        },
        { idempotencyKey: intent.current() }
      );

      if (response.ok) {
        intent.completed();

        const resource = await unwrap<IssuedInvitationResource>(response);
        const reissued = wasReissued(resource.invitation);

        toast.success(
          reissued
            ? t("invitations.resent", resource.invitation.email)
            : t("invitations.sentToOrganization", resource.invitation.email, organizationName)
        );

        setOpen(false);
        setIssued({
          acceptUrl: resource.invitation.acceptUrl,
          email: resource.invitation.email,
          reissued,
        });
        onInvited?.();
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
          toast.error(t("invitations.alreadyInOrganization", organizationName), {
            description: t("invitations.alreadyInOrganizationHelp"),
          });
          return;
        }

        if (member?.sameOrganization === false) {
          toast.error(t("invitations.belongsElsewhere"), {
            description: t("invitations.belongsElsewhereOperatorHelp"),
          });
          return;
        }
      }

      await reportApiError(response, {
        fallback: t("invitations.errorInvitingToOrganization"),
        onUnauthorized,
        fieldLabels: { Role: t("invitations.invite.role") },
      });
    } catch {
      toast.error(t("invitations.errorInvitingToOrganization"));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      <Button
        size="sm"
        variant="outline"
        className="gap-1.5"
        onClick={() => setOpen(true)}
      >
        <UserPlus className="size-4" />
        {t("invitations.inviteAction")}
      </Button>

      <InviteMemberDialog
        open={open}
        onOpenChange={setOpen}
        organizationId={organizationId}
        defaultRole="Manager"
        titleKey="invitations.operator.title"
        descriptionKey="invitations.operator.description"
        submitting={submitting}
        onSubmit={(request) => void handleInvite(request)}
      />

      <InviteLinkDialog
        acceptUrl={issued?.acceptUrl ?? null}
        email={issued?.email ?? ""}
        reissued={issued?.reissued ?? false}
        onClose={() => setIssued(null)}
      />
    </>
  );
}
