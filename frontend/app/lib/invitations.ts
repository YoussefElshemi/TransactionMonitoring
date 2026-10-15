import { API_BASE, organizationScopedPath } from "@/lib/api";
import {
  Invitation,
  InvitableRole,
  InvitationAlreadyMemberProblemDetails,
  InvitationNotPendingProblemDetails,
  InvitationStatus,
  ProblemDetails,
} from "@/types";

export const INVITATIONS_PATH = `${API_BASE}/invitations`;

export const invitationsPath = (organizationId?: string): string =>
  organizationScopedPath("invitations", organizationId);

export const invitationPath = (
  invitationId: string,
  organizationId?: string
): string => `${invitationsPath(organizationId)}/${invitationId}`;

export const resendInvitationPath = (
  invitationId: string,
  organizationId?: string
): string => `${invitationPath(invitationId, organizationId)}/resend`;

export const revokeInvitationPath = (
  invitationId: string,
  organizationId?: string
): string => `${invitationPath(invitationId, organizationId)}/revoke`;

export const previewInvitationPath = (token: string): string =>
  `${INVITATIONS_PATH}/preview?token=${encodeURIComponent(token)}`;

export const acceptInvitationPath = (): string =>
  `${INVITATIONS_PATH}/accept`;

export const organizationInvitationsPath = (organizationId: string): string =>
  invitationsPath(organizationId);

export const offboardUserPath = (
  organizationId: string,
  userId: string
): string => `${API_BASE}/organizations/${organizationId}/users/${userId}/offboard`;

export const ACCEPT_INVITATION_HREF = "/invitations/accept";

export const TOKEN_PARAM = "token";

export const INVITATIONS_HREF = "/organization#Invitations";
export const MEMBERS_HREF = "/organization#Members";

export const OFFERABLE_ROLES: readonly InvitableRole[] = [
  "User",
  "Manager",
  "Auditor",
];

export interface RoleOffer {
  role: InvitableRole;
  nameKey: string;
  descriptionKey: string;
}

export const ROLE_OFFERS: Record<InvitableRole, RoleOffer> = {
  User: {
    role: "User",
    nameKey: "invitations.role.user",
    descriptionKey: "invitations.role.user.description",
  },
  Manager: {
    role: "Manager",
    nameKey: "invitations.role.manager",
    descriptionKey: "invitations.role.manager.description",
  },
  Auditor: {
    role: "Auditor",
    nameKey: "invitations.role.auditor",
    descriptionKey: "invitations.role.auditor.description",
  },
};

export const roleOffer = (role: InvitableRole | string): RoleOffer | null =>
  (ROLE_OFFERS as Record<string, RoleOffer | undefined>)[role] ?? null;

export const roleName = (
  t: (key: string) => string,
  role: InvitableRole | string
): string => {
  const offer = roleOffer(role);
  return offer ? t(offer.nameKey) : role;
};

const NOT_PENDING_TYPE = "InvitationNotPendingException";
const ALREADY_MEMBER_TYPE = "InvitationAlreadyMemberException";
const INVALID_TOKEN_TYPE = "InvalidInvitationTokenException";

export const readInvitationNotPending = (
  body: unknown
): { invitationStatus: InvitationStatus | null } | null => {
  const problem = body as InvitationNotPendingProblemDetails | null;
  if (!problem || problem.type !== NOT_PENDING_TYPE) return null;

  const named =
    typeof problem.invitationStatus === "string"
      ? (problem.invitationStatus as InvitationStatus)
      : null;

  return { invitationStatus: named };
};

export const readInvitationAlreadyMember = (
  body: unknown
): { sameOrganization: boolean | null } | null => {
  const problem = body as InvitationAlreadyMemberProblemDetails | null;
  if (!problem || problem.type !== ALREADY_MEMBER_TYPE) return null;

  return {
    sameOrganization:
      typeof problem.sameOrganization === "boolean"
        ? problem.sameOrganization
        : null,
  };
};

export const isInvalidInvitationToken = (body: unknown): boolean =>
  (body as (ProblemDetails & { type?: string }) | null)?.type ===
  INVALID_TOKEN_TYPE;

export const isActionable = (invitation: Invitation): boolean =>
  invitation.status !== "Accepted" && invitation.status !== "Revoked";

export const needsResend = (invitation: Invitation): boolean =>
  invitation.status === "Expired";

export const wasReissued = (invitation: Invitation): boolean =>
  invitation.updatedAt !== invitation.createdAt;
