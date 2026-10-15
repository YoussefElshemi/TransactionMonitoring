import { ApplicationUser } from "@/types";

export type OrganizationAccess =
  | "granted"
  | "pending"
  | "overdue"
  | "disabled";

export const isReachableWithoutOrganization = (
  pathname: string,
  canAdministerPlatform: boolean
) =>
  pathname === "/profile" ||
  (canAdministerPlatform && pathname.startsWith("/admin"));

export const getOrganizationAccess = (
  user: ApplicationUser | null
): OrganizationAccess => {
  if (!user) return "granted";
  if (user.isEnabled === false) return "pending";

  switch (user.organization?.status) {
    case "OverdueInvoice":
      return "overdue";
    case "Disabled":
      return "disabled";
    default:
      return "granted";
  }
};
