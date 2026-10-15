"use client";

import { useCallback, useState, useEffect } from "react";
import {
  useRouter,
  useParams,
  useSearchParams,
  usePathname
} from "next/navigation";
import {
  AccountBalance,
  Organization,
  ValidationProblemDetails,
  ProblemDetails,
  ApplicationUser,
  Paged,
  UserResource
} from "@/types";
import { toast } from "sonner";
import { UserCombobox } from "@/components/UserCombobox";
import { InvitationsPanel } from "@/components/invitations/InvitationsPanel";
import { InviteToOrganizationButton } from "@/components/invitations/InviteToOrganizationButton";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { SectionNav, SectionGroup } from "@/components/SectionNav";
import { AvailabilityPanel } from "@/components/availability/AvailabilityPanel";
import { AvailabilityCommitmentsPanel } from "@/components/availability/AvailabilityCommitmentsPanel";
import { ServiceCreditsPanel } from "@/components/availability/ServiceCreditsPanel";
import { FieldList, Field } from "@/components/FieldList";
import { HistoryPanel, historyPath } from "@/components/history";
import { AuditChainVerificationPanel } from "@/components/audit";
import { StatusBadge } from "@/components/StatusBadge";
import {
  HoldPolicy,
  HoldPolicyFields,
  holdPolicyPatch,
} from "@/components/HoldPolicyFields";
import {
  AlertPolicyPanel,
  alertPolicyOf,
} from "@/components/organization/AlertPolicyPanel";
import { PartyMappingPanel } from "@/components/organization/PartyMappingPanel";
import { TeamMembersPanel } from "@/components/organization/TeamMembersPanel";
import { TransactionSchemaPanel } from "@/components/organization/TransactionSchemaPanel";
import { VelocityProfilePanel } from "@/components/organization/VelocityProfilePanel";
import { ReasonCodesPanel } from "@/components/reasonCodes/ReasonCodesPanel";
import { SlaTargetsPanel } from "@/components/organization/SlaTargetsPanel";
import { ApprovalPolicyPanel } from "@/components/organization/ApprovalPolicyPanel";
import { SuspiciousActivityReportPolicyPanel } from "@/components/organization/SuspiciousActivityReportPolicyPanel";
import { EscalationRulesPanel } from "@/components/escalations/EscalationRulesPanel";
import { NotificationSubscriptionsPanel } from "@/components/notifications/NotificationSubscriptionsPanel";
import { WebhookConfigurationsPanel } from "@/components/webhooks/WebhookConfigurationsPanel";
import { WebhookDeliveriesPanel } from "@/components/webhooks/WebhookDeliveriesPanel";
import { ApiClientsPanel } from "@/components/apiClients/ApiClientsPanel";
import { RolesPanel } from "@/components/roles/RolesPanel";
import { RoleAssignmentsPanel } from "@/components/roles/RoleAssignmentsPanel";
import { SubOrganizationsPanel } from "@/components/subOrganizations/SubOrganizationsPanel";
import { SubscriptionsPanel } from "@/components/billing/SubscriptionsPanel";
import { UsagePanel } from "@/components/billing/UsagePanel";
import { ContractTermsPanel } from "@/components/billing/ContractTermsPanel";
import { InvoicesPanel } from "@/components/billing/InvoicesPanel";
import { PaymentMethodsPanel } from "@/components/payments/PaymentMethodsPanel";
import { PaymentsPanel } from "@/components/payments/PaymentsPanel";
import { AccountPanel } from "@/components/payments/AccountPanel";
import { ArrearsBoard } from "@/components/reconciliation/ArrearsBoard";
import { RecordPaymentDialog } from "@/components/reconciliation/RecordPaymentDialog";
import { ReconcileDialog } from "@/components/reconciliation/ReconcileDialog";
import { BillingProfilePanel } from "@/components/billing/BillingProfilePanel";
import { CreditNotesPanel } from "@/components/billing/CreditNotesPanel";
import { RateLimitOverridesPanel } from "@/components/rateLimits/RateLimitOverridesPanel";
import { SandboxPanel } from "@/components/organization/SandboxPanel";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/context/AuthContext";
import { canWriteBilling } from "@/lib/permissions";
import { usePermissions } from "@/context/PermissionsContext";
import { useRequirePermission } from "@/lib/useRequirePermission";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Card, CardContent } from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import { Trash2, UserPlus, UserX } from "lucide-react";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, unwrap } from "@/lib/api";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { OrganizationDefaultsPanel } from "@/components/preferences/OrganizationDefaultsPanel";
import { useTranslation } from "@/context/LocalizationContext";

interface Section {
  name: string;
}

interface SectionGroupSpec {
  labelKey: string;
  sections: Section[];
}

const SECTION_GROUPS: SectionGroupSpec[] = [
  {
    labelKey: "organization.groupOrganization",
    sections: [
      { name: "Details" },
      { name: "Language & Region" },
      { name: "Sandbox" },
    ],
  },
  {
    labelKey: "organization.groupCasePolicy",
    sections: [
      { name: "Hold Policy" },
      { name: "Alert Policy" },
      { name: "SLA Targets" },
      { name: "Escalation Rules" },
      { name: "Approval Policy" },
      { name: "Filing Policy" },
      { name: "Reason Codes" },
    ],
  },
  {
    labelKey: "organization.groupTransactionData",
    sections: [
      { name: "Transaction Schema" },
      { name: "Party Mapping" },
      { name: "Velocity Profile" },
    ],
  },
  {
    labelKey: "organization.groupDelivery",
    sections: [
      { name: "API Clients" },
      { name: "Notifications" },
      { name: "Webhook Configurations" },
      { name: "Webhook Deliveries" },
    ],
  },
  {
    labelKey: "organization.groupPeople",
    sections: [
      { name: "Members" },
      { name: "Invitations" },
    ],
  },
  {
    labelKey: "organization.groupAccess",
    sections: [
      { name: "Roles" },
      { name: "Access" },
      { name: "Business Units" },
    ],
  },
  {
    labelKey: "organization.groupRecords",
    sections: [
      { name: "Audit Trail" },
      { name: "History" },
    ],
  },
  {
    labelKey: "organization.groupBilling",
    sections: [
      { name: "Billing" },
      { name: "Commercial Terms" },
      { name: "Usage" },
      { name: "Invoices" },
      { name: "Payment Methods" },
      { name: "Payments" },
      { name: "Account" },
      { name: "Arrears" },
    ],
  },
  {
    labelKey: "organization.groupAvailability",
    sections: [
      { name: "Availability" },
      { name: "Agreed Targets" },
      { name: "Service Credits" },
    ],
  },
  {
    labelKey: "organization.groupPlatform",
    sections: [{ name: "Rate Limits" }],
  },
];

const ALL_SECTIONS = new Set(
  SECTION_GROUPS.flatMap((group) => group.sections.map((section) => section.name))
);

const OrganizationDetails = () => {
  const t = useTranslation();
  const { allowed } = useRequirePermission("platform:administer", "/");
  const { holds } = usePermissions();

  const { id } = useParams();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const [activeSection, setActiveSection] = useState<string>("Details");
  const [organization, setOrganization] = useState<Organization | null>(null);
  const { user } = useAuth();
  const ownOrganization =
    organization !== null && user?.organizationId === organization.organizationId;
  const [friendlyName, setFriendlyName] = useState<string>("");
  const [status, setStatus] = useState<string>("");
  const [localeSaveLoading, setLocaleSaveLoading] = useState(false);
  const [holdPolicy, setHoldPolicy] = useState<HoldPolicy>({
    holdWindow: null,
    holdExpiryVerdict: null,
  });
  const [selectedUser, setSelectedUser] = useState<ApplicationUser | null>(null);
  const [userSearchTerm, setUserSearchTerm] = useState<string>("");
  const [candidates, setCandidates] = useState<ApplicationUser[]>([]);
  const [candidatesLoading, setCandidatesLoading] = useState<boolean>(false);

  const [membersVersion, setMembersVersion] = useState<number>(0);
  const reloadMembers = () => setMembersVersion((version) => version + 1);

  const [loading, setLoading] = useState<boolean>(true);
  const [saveLoading, setSaveLoading] = useState<boolean>(false);
  const [holdSaveLoading, setHoldSaveLoading] = useState<boolean>(false);

  const [addUserModalOpen, setAddUserModalOpen] = useState<boolean>(false);
  const [modalLoading, setModalLoading] = useState<boolean>(false);

  const router = useRouter();

  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const sectionGroups: SectionGroup[] = SECTION_GROUPS.map((group) => ({
    labelKey: group.labelKey,
    sections: group.sections.map((section) => section.name),
  }));

  const sections = sectionGroups.flatMap((group) => group.sections);

  const handleCloseModal = () => {
    setAddUserModalOpen(false);
    setSelectedUser(null);
    setUserSearchTerm("");
  };

  const fetchOrganizationById = async (id: string, showSkeleton = true) => {
    if (showSkeleton) setLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${id}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = await unwrap<Organization>(response);
        setOrganization(data);
        setFriendlyName(data.friendlyName);
        setStatus(data.status);
        setHoldPolicy({
          holdWindow: data.holdWindow,
          holdExpiryVerdict: data.holdExpiryVerdict,
        });
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginAccessResource"), {
          id: "unauthorized"
        });
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission"),
          {
            id: "forbidden"
          }
        );
        return router.push("/");
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("organizations.errorOccurredFetchingOrganization"),
          { id: "error-fetching-organizations" }
        );
      }
    } catch {
      toast.error(t("organizations.errorOccurredFetchingOrganization"), {
        id: "error-fetching-organizations"
      });
    } finally {
      if (showSkeleton) setLoading(false);
    }
  };

  const handleAddUser = async (organizationId: string, userId: string) => {
    setModalLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/users/${userId}`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("organizations.successfullyAddedUser"));
        reloadMembers();
        handleCloseModal();
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("organizations.errorOccurredAddingUser"), {
          id: "error-adding-user"
        });
      }
    } catch {
      toast.error(t("organizations.errorOccurredAddingUser"), {
        id: "error-adding-user"
      });
    } finally {
      setModalLoading(false);
    }
  };

  const fetchUsers = async (searchTerm: string) => {
    setCandidatesLoading(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/users?UserName=${searchTerm}`,
        {
          method: "GET",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include"
        }
      );

      if (response.ok) {
        const data = (await response.json()) as Paged<UserResource>;
        setCandidates(data.data.map((resource) => resource.user));
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginAccessResource"), {
          id: "unauthorized"
        });
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission"),
          {
            id: "forbidden"
          }
        );
        router.push("/");
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("common.errorOccurredFetchingUsers"),
          { id: "error-fetching-users" }
        );
      }
    } catch {
      toast.error(t("common.errorOccurredFetchingUsers"), {
        id: "error-fetching-users"
      });
    } finally {
      setCandidatesLoading(false);
    }
  };

  const patchOrganization = async (
    organizationId: string,
    body: Record<string, unknown>
  ): Promise<boolean> => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}`,
        {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json"
          },
          credentials: "include",
          body: JSON.stringify(body)
        }
      );

      if (response.ok) {
        await fetchOrganizationById(organizationId, false);
        return true;
      }

      if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail || t("common.errorOccurredSavingChanges")
        );
      }
    } catch {
      toast.error(t("common.errorOccurredSavingChanges"));
    }

    return false;
  };

  const handleSaveDetails = async () => {
    if (!organization) return;

    if (
      organization.friendlyName === friendlyName &&
      organization.status === status
    )
      return;

    setSaveLoading(true);

    const body: Record<string, unknown> = {
      friendlyName,
      status
    };
    const saved = await patchOrganization(organization.organizationId, body);

    setSaveLoading(false);

    if (saved) toast.success(t("organizations.successfullyUpdatedOrganization"));
  };

  const handleSaveHoldPolicy = async () => {
    if (!organization) return;

    // Assembled key by key rather than spread: on these two fields the API
    // reads an omitted key as "leave it alone" and an explicit null as "clear
    // it back to the server default", so sending the whole form would wipe the
    // policy on every save.
    const patch = holdPolicyPatch(holdPolicy, {
      holdWindow: organization.holdWindow,
      holdExpiryVerdict: organization.holdExpiryVerdict,
    });

    if (Object.keys(patch).length === 0) return;

    if (
      patch.holdExpiryVerdict === "Never" &&
      !window.confirm(
        "Set this organisation to never expire holds?\n\n" +
          "Every rule that inherits this will hold transactions indefinitely — " +
          "nothing will release or block on its own, and each one waits for an " +
          "analyst. An individual rule can still set its own verdict to " +
          "reinstate a deadline."
      )
    ) {
      return;
    }

    setHoldSaveLoading(true);

    const saved = await patchOrganization(organization.organizationId, patch);

    setHoldSaveLoading(false);

    if (saved) toast.success(t("common.holdPolicyUpdated"));
  };

  const resetHoldPolicy = () => {
    if (!organization) return;

    setHoldPolicy({
      holdWindow: organization.holdWindow,
      holdExpiryVerdict: organization.holdExpiryVerdict,
    });
  };

  const deleteOrganization = async (organizationId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}`,
        {
          method: "DELETE",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("organizations.successfullyDeletedOrganization2"), {
          action: { label: t("common.undo"), onClick: () => recoverOrganization(organizationId) },
        });
        router.push("/admin/organizations");
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("organizations.failedDeleteOrganization2"));
      }
    } catch {
      toast.error(t("organizations.failedDeleteOrganization2"));
    }
  };

  const recoverOrganization = async (organizationId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/recover`,
        {
          method: "POST",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("organizations.successfullyRecoveredOrganization2"));
        router.push(`/admin/organizations/${organizationId}`);
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("organizations.failedRecoverOrganization2"));
      }
    } catch {
      toast.error(t("organizations.failedRecoverOrganization2"));
    }
  };

  const removeUser = async (organizationId: string, userId: string) => {
    try {
      const response = await apiFetch(
        `${API_BASE}/organizations/${organizationId}/users/${userId}`,
        {
          method: "DELETE",
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("organizations.successfullyRemovedUser"));
        reloadMembers();
      } else if (response.status === 400) {
        const errorData = (await response.json()) as ValidationProblemDetails;
        toast.error(
          Object.keys(errorData.errors)
            .map(
              (key) =>
                `${key}${key.length ? ": " : ""}${errorData.errors[key].join(
                  "\n"
                )}`
            )
            .join("\n")
        );
      } else if (response.status === 401) {
        toast.error(t("common.pleaseLoginPerformAction"));
        return router.push(loginHref(redirectUri));
      } else if (response.status === 403) {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(
          errorData.detail ||
            t("common.youDoNotHavePermission2")
        );
      } else {
        const errorData = (await response.json()) as ProblemDetails;
        toast.error(errorData.detail || t("organizations.failedDeleteUser"));
      }
    } catch {
      toast.error(t("organizations.failedDeleteUser"));
    }
  };

  useEffect(() => {
    if (!allowed) return;
    fetchOrganizationById(id as string);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [allowed, id]);

  useEffect(() => {
    if (addUserModalOpen) fetchUsers(userSearchTerm);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [addUserModalOpen, userSearchTerm]);

  useEffect(() => {
    const fromHash = decodeURIComponent(window.location.hash.replace("#", ""));
    if (fromHash && sections.includes(fromHash)) setActiveSection(fromHash);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sections.join("|")]);

  useEffect(() => {
    const handleHashChange = () => {
      const section = decodeURIComponent(
        window.location.hash.replace("#", "")
      );
      if (ALL_SECTIONS.has(section)) setActiveSection(section);
    };

    window.addEventListener("hashchange", handleHashChange);
    return () => window.removeEventListener("hashchange", handleHashChange);
  }, []);

  const handleSectionChange = (section: string) => {
    if (section === activeSection) return;

    setActiveSection(section);
    window.history.replaceState(null, "", `#${encodeURIComponent(section)}`);
  };

  if (!allowed) return null;

  if (loading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-64" />
        <Card>
          <CardContent className="pt-6 space-y-3">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-4 w-full" />
            ))}
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!organization) {
    return <NotFound
        title={t("organizations.organizationNotFound")}
        description={t("common.notFound.description")}
        backHref="/admin/organizations"
        backLabel={t("organizations.allOrganizations")}
      />;
  }

  const holdPolicyDirty =
    Object.keys(
      holdPolicyPatch(holdPolicy, {
        holdWindow: organization.holdWindow,
        holdExpiryVerdict: organization.holdExpiryVerdict,
      })
    ).length > 0;

  const detailsDirty =
    organization.friendlyName !== friendlyName ||
    organization.status !== status;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("organizations.organization")}
        title={organization.friendlyName}
        description={t("organizations.everythingThisOrganizationSRules")}
      >
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8 text-critical-500 hover:text-critical-700"
          onClick={() => deleteOrganization(organization.organizationId)}
        >
          <Trash2 className="size-4" />
        </Button>
      </PageHeader>

      <div className="grid gap-6 lg:grid-cols-[14rem_minmax(0,1fr)] lg:items-start">
        <SectionNav
          groups={sectionGroups}
          active={activeSection}
          onChange={handleSectionChange}
        />

        {/* `min-w-0`, or a wide table inside a panel stretches the grid column
            instead of scrolling within it. */}
        <div className="min-w-0 space-y-6">
          {activeSection === "Details" && (
            <>
              <Card>
                <CardContent className="space-y-4 pt-6">
                  <div className="grid gap-x-8 gap-y-4 sm:grid-cols-2">
                    <div className="space-y-1.5">
                      <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                        {t("organizations.friendlyName")}
                      </label>
                      <Input
                        value={friendlyName}
                        onChange={(e) => setFriendlyName(e.target.value)}
                        disabled={saveLoading}
                      />
                    </div>
                    <div className="space-y-1.5">
                      <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                        {t("common.status")}
                      </label>
                      <Select
                        value={status}
                        onValueChange={setStatus}
                        disabled={saveLoading}
                      >
                        <SelectTrigger>
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {[...new Set(["Enabled", status, "Disabled"])].map(
                            (s) => (
                              <SelectItem key={s} value={s}>
                                {s}
                              </SelectItem>
                            )
                          )}
                        </SelectContent>
                      </Select>
                    </div>
                  </div>

                  <div className="flex items-center gap-2 border-t border-slate-100 pt-4">
                    <Button
                      size="sm"
                      onClick={handleSaveDetails}
                      disabled={!detailsDirty || saveLoading}
                    >
                      {saveLoading ? "Saving..." : "Save"}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        setFriendlyName(organization.friendlyName);
                        setStatus(organization.status);
                      }}
                      disabled={!detailsDirty || saveLoading}
                    >
                      {t("common.reset")}
                    </Button>
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardContent className="p-0">
                  <FieldList>
                    <Field label={t("common.status")}>
                      <StatusBadge status={organization.status} />
                    </Field>
                    <Field label={t("common.created")} mono>
                      <LocalTime value={organization.createdAt} />
                    </Field>
                    <Field label={t("common.updated")} mono>
                      <LocalTime value={organization.updatedAt} />
                    </Field>
                    <Field label={t("organizations.organizationId")} mono>
                      <span className="break-all text-xs text-slate-500">
                        {organization.organizationId}
                      </span>
                    </Field>
                  </FieldList>
                </CardContent>
              </Card>
            </>
          )}

          {activeSection === "Language & Region" && (
            <OrganizationDefaultsPanel
              defaultLocale={organization.defaultLocale}
              defaultTimeZone={organization.defaultTimeZone}
              saving={localeSaveLoading}
              onSave={async (patch) => {
                setLocaleSaveLoading(true);
                const saved = await patchOrganization(
                  organization.organizationId,
                  patch
                );
                setLocaleSaveLoading(false);
                if (saved) toast.success(t("organization.defaults.saved"));
              }}
            />
          )}

          {activeSection === "Sandbox" && (
            <SandboxPanel
              organizationId={organization.organizationId}
              organizationName={organization.friendlyName}
              canConfigureAllowance
            />
          )}

          {activeSection === "Hold Policy" && (
            <Card>
              <CardContent className="space-y-4 pt-6">
                <div>
                  <h2 className="text-sm font-semibold text-slate-900">
                    {t("organizations.holdPolicy")}
                  </h2>
                  <p className="mt-0.5 text-sm text-slate-500">
                    The default for every rule in this organization that does
                    not set its own. A rule&apos;s own policy always wins, and
                    where several rules fire together the longest window and the
                    most restrictive verdict win.
                  </p>
                </div>

                <HoldPolicyFields
                  scope="organization"
                  value={holdPolicy}
                  onChange={setHoldPolicy}
                  disabled={holdSaveLoading}
                  inheritance={{ organization: null }}
                />

                <div className="flex items-center gap-2 border-t border-slate-100 pt-4">
                  <Button
                    size="sm"
                    onClick={handleSaveHoldPolicy}
                    disabled={!holdPolicyDirty || holdSaveLoading}
                  >
                    {holdSaveLoading ? "Saving..." : "Save hold policy"}
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={resetHoldPolicy}
                    disabled={!holdPolicyDirty || holdSaveLoading}
                  >
                    {t("common.reset")}
                  </Button>
                </div>

                <p className="text-xs text-slate-400">
                  Changing this never moves a deadline that is already running —
                  a hold keeps the policy it started with, so a transaction
                  someone has already been given a date for is unaffected.
                </p>
              </CardContent>
            </Card>
          )}

          {activeSection === "Alert Policy" && (
            <AlertPolicyPanel
              organizationSegment={organization.organizationId}
              saved={alertPolicyOf(organization)}
              onSaved={() =>
                fetchOrganizationById(organization.organizationId, false)
              }
            />
          )}

          {activeSection === "SLA Targets" && (
            <SlaTargetsPanel
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Escalation Rules" && (
            <EscalationRulesPanel
              organizationId={organization.organizationId}
              detailHref={(escalationRuleId) =>
                `/admin/organizations/${organization.organizationId}/escalation-rules/${escalationRuleId}`
              }
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Approval Policy" && (
            <ApprovalPolicyPanel
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Filing Policy" && (
            <SuspiciousActivityReportPolicyPanel
              organizationId={organization.organizationId}
              suggestSchemaFields={ownOrganization}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Reason Codes" && (
            <ReasonCodesPanel
              organizationId={organization.organizationId}
              detailHref={(caseReasonCodeId) =>
                `/admin/organizations/${organization.organizationId}/reason-codes/${caseReasonCodeId}`
              }
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Transaction Schema" && (
            <TransactionSchemaPanel
              organizationSegment={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Party Mapping" && (
            <PartyMappingPanel
              organizationSegment={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Velocity Profile" && (
            <VelocityProfilePanel
              organizationSegment={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "API Clients" && (
            <ApiClientsPanel
              organizationId={organization.organizationId}
              detailHref={(apiClientId) =>
                `/admin/organizations/${organization.organizationId}/api-clients/${apiClientId}`
              }
            />
          )}

          {activeSection === "Notifications" && (
            <NotificationSubscriptionsPanel
              organizationId={organization.organizationId}
              webhookHref={`/admin/organizations/${organization.organizationId}#Webhook%20Configurations`}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Webhook Configurations" && (
            <WebhookConfigurationsPanel
              organizationSegment={organization.organizationId}
              detailHref={(webhookConfigurationId) =>
                `/admin/organizations/${organization.organizationId}/webhook-configurations/${webhookConfigurationId}`
              }
              embedded
            />
          )}

          {activeSection === "Webhook Deliveries" && (
            <WebhookDeliveriesPanel
              organizationSegment={organization.organizationId}
              canWrite={holds("webhook-configurations:write")}
              configurationHref={(webhookConfigurationId) =>
                `/admin/organizations/${organization.organizationId}/webhook-configurations/${webhookConfigurationId}`
              }
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Members" && (
            <TeamMembersPanel
              organizationId={organization.organizationId}
              organizationSegment={organization.organizationId}
              detailHref={(userId) =>
                `/admin/organizations/${organization.organizationId}/users/${userId}`
              }
              refreshToken={membersVersion}
              actions={
                <>
                  <InviteToOrganizationButton
                    organizationId={organization.organizationId}
                    organizationName={organization.friendlyName}
                    onUnauthorized={goToLogin}
                    onInvited={reloadMembers}
                  />
                  <Button
                    size="sm"
                    className="gap-1.5"
                    onClick={() => setAddUserModalOpen(true)}
                  >
                    <UserPlus className="size-4" />
                    {t("organizations.addUser")}
                  </Button>
                </>
              }
              rowActions={(member) => (
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-8 w-8 text-critical-500 hover:text-critical-700"
                  title={t("organizations.removeUser")}
                  onClick={() =>
                    removeUser(organization.organizationId, member.userId)
                  }
                >
                  <UserX className="size-4" />
                </Button>
              )}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Invitations" && (
            <InvitationsPanel
              organizationId={organization.organizationId}
              organizationScoped
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Roles" && (
            <RolesPanel
              organizationId={organization.organizationId}
              detailHref={(roleId) =>
                `/admin/organizations/${organization.organizationId}/roles/${roleId}`
              }
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Access" && (
            <RoleAssignmentsPanel
              organizationId={organization.organizationId}
              roleHref={(roleId) =>
                `/admin/organizations/${organization.organizationId}/roles/${roleId}`
              }
              userHref={(userId) =>
                `/admin/organizations/${organization.organizationId}/users/${userId}/access`
              }
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Business Units" && (
            <SubOrganizationsPanel
              organizationId={organization.organizationId}
              detailHref={(subOrganizationId) =>
                `/admin/organizations/${organization.organizationId}/business-units/${subOrganizationId}`
              }
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Audit Trail" && (
            <AuditChainVerificationPanel
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "History" && (
            <HistoryPanel
              entityType="Organization"
              path={historyPath.Organization(organization.organizationId)}
              entityId={organization.organizationId}
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Billing" && (
            <SubscriptionsPanel
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Invoices" && (
            <div className="space-y-8">
              <BillingProfilePanel
                organizationId={organization.organizationId}
                onUnauthorized={goToLogin}
              />

              <InvoicesPanel
                organizationId={organization.organizationId}
                onUnauthorized={goToLogin}
              />

              <CreditNotesPanel
                organizationId={organization.organizationId}
                onUnauthorized={goToLogin}
              />
            </div>
          )}

          {activeSection === "Payment Methods" && (
            <PaymentMethodsPanel
              organizationId={organization.organizationId}
              returnPath={`/admin/organizations/${organization.organizationId}#Payment%20Methods`}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Payments" && (
            <div className="space-y-4">
              <RecordTransferAction
                organizationId={organization.organizationId}
                onUnauthorized={goToLogin}
              />

              <PaymentsPanel
                organizationId={organization.organizationId}
                onUnauthorized={goToLogin}
              />
            </div>
          )}

          {activeSection === "Account" && (
            <TenantAccountSection
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Arrears" && (
            <ArrearsBoard
              filterOrganizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Usage" && (
            <UsagePanel
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Commercial Terms" && (
            <ContractTermsPanel
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Rate Limits" && (
            <RateLimitOverridesPanel
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Availability" && (
            <AvailabilityPanel
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Agreed Targets" && (
            <AvailabilityCommitmentsPanel
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeSection === "Service Credits" && (
            <ServiceCreditsPanel
              organizationId={organization.organizationId}
              onUnauthorized={goToLogin}
            />
          )}
        </div>
      </div>

      <Dialog open={addUserModalOpen} onOpenChange={setAddUserModalOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>{t("organizations.addUser")}</DialogTitle>
          </DialogHeader>
          <UserCombobox
            users={candidates}
            loading={candidatesLoading}
            value={selectedUser}
            onChange={setSelectedUser}
            onSearchTermChange={setUserSearchTerm}
            onSubmit={() =>
              selectedUser && handleAddUser(id as string, selectedUser.userId)
            }
            disabled={modalLoading}
            emptyLabel={t("organizations.noUsersToAdd")}
          />
          <DialogFooter>
            <Button variant="outline" onClick={handleCloseModal} disabled={modalLoading}>
              {t("common.cancel")}
            </Button>
            <Button
              onClick={() =>
                selectedUser && handleAddUser(id as string, selectedUser.userId)
              }
              disabled={modalLoading || !selectedUser}
              className="max-w-[15rem]"
            >
              <span className="min-w-0 truncate">
                {modalLoading
                  ? "Adding..."
                  : selectedUser
                    ? `Add ${selectedUser.userName}`
                    : "Add"}
              </span>
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default OrganizationDetails;

function RecordTransferAction({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { grants } = usePermissions();
  const [open, setOpen] = useState(false);
  const [recorded, setRecorded] = useState(0);

  if (!canWriteBilling(grants, "payments:reconcile")) return null;

  return (
    <div className="flex flex-wrap items-start justify-between gap-3 rounded-xl border border-slate-200 bg-slate-50 p-4">
      <div>
        <p className="font-mono text-xs uppercase tracking-widest text-slate-500">
          {t("reconciliation.recordATransfer")}
        </p>
        <p className="mt-1 max-w-2xl text-sm text-slate-500">
          {t("reconciliation.moneyThatArrivedInTheBank")}
        </p>
      </div>

      <Button size="sm" onClick={() => setOpen(true)}>
        {t("reconciliation.recordATransfer")}
      </Button>

      <RecordPaymentDialog
        key={recorded}
        organizationId={organizationId}
        open={open}
        onClose={() => setOpen(false)}
        onRecorded={() => setRecorded((count) => count + 1)}
        onUnauthorized={onUnauthorized}
      />
    </div>
  );
}

function TenantAccountSection({
  organizationId,
  onUnauthorized,
}: {
  organizationId: string;
  onUnauthorized: () => void;
}) {
  const { grants } = usePermissions();
  const [balance, setBalance] = useState<AccountBalance | null>(null);
  const [reconciled, setReconciled] = useState(0);

  const mayReconcile = canWriteBilling(grants, "payments:reconcile");
  const mayRefund = canWriteBilling(grants, "payments:refund");

  return (
    <>
      <AccountPanel
        key={reconciled}
        organizationId={organizationId}
        invoiceHref={() => `/admin/organizations/${organizationId}#Invoices`}
        creditNoteHref={() => `/admin/organizations/${organizationId}#Invoices`}
        onReconcile={mayReconcile ? setBalance : undefined}
        onUnauthorized={onUnauthorized}
      />

      {balance !== null && (
        <ReconcileDialog
          balance={balance}
          mayRefund={mayRefund}
          organizationName={null}
          onClose={() => setBalance(null)}
          onChanged={() => setReconciled((count) => count + 1)}
          onUnauthorized={onUnauthorized}
        />
      )}
    </>
  );
}
