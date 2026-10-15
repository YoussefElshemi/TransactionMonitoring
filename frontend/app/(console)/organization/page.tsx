"use client";

import { useCallback, useEffect, useState } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

import { useAuth } from "@/context/AuthContext";
import { usePermissions } from "@/context/PermissionsContext";
import { NotFound } from "@/components/NotFound";
import { PageHeader } from "@/components/PageHeader";
import { StatusBadge } from "@/components/StatusBadge";
import { SectionNav, SectionGroup } from "@/components/SectionNav";
import { FieldList, Field } from "@/components/FieldList";
import { CurrentPlanFields } from "@/components/billing/CurrentPlanFields";
import { ApiClientsPanel } from "@/components/apiClients/ApiClientsPanel";
import { AuditChainVerificationPanel } from "@/components/audit";
import { HoldPolicyPanel } from "@/components/organization/HoldPolicyPanel";
import { MyAlertPolicyPanel } from "@/components/organization/AlertPolicyPanel";
import { SlaTargetsPanel } from "@/components/organization/SlaTargetsPanel";
import { ApprovalPolicyPanel } from "@/components/organization/ApprovalPolicyPanel";
import { SuspiciousActivityReportPolicyPanel } from "@/components/organization/SuspiciousActivityReportPolicyPanel";
import { EscalationRulesPanel } from "@/components/escalations/EscalationRulesPanel";
import { PartyMappingPanel } from "@/components/organization/PartyMappingPanel";
import { TransactionSchemaPanel } from "@/components/organization/TransactionSchemaPanel";
import { VelocityProfilePanel } from "@/components/organization/VelocityProfilePanel";
import { InvitationsPanel } from "@/components/invitations/InvitationsPanel";
import { TeamMembersPanel } from "@/components/organization/TeamMembersPanel";
import { ReasonCodesPanel } from "@/components/reasonCodes/ReasonCodesPanel";
import { RolesPanel } from "@/components/roles/RolesPanel";
import { RoleAssignmentsPanel } from "@/components/roles/RoleAssignmentsPanel";
import { SubOrganizationsPanel } from "@/components/subOrganizations/SubOrganizationsPanel";
import { NotificationSubscriptionsPanel } from "@/components/notifications/NotificationSubscriptionsPanel";
import { WebhookConfigurationsPanel } from "@/components/webhooks/WebhookConfigurationsPanel";
import { WebhookDeliveriesPanel } from "@/components/webhooks/WebhookDeliveriesPanel";
import { SandboxPanel } from "@/components/organization/SandboxPanel";
import { UsagePanel } from "@/components/billing/UsagePanel";
import { CurrentPlanPanel } from "@/components/billing/CurrentPlanPanel";
import { InvoicesPanel } from "@/components/billing/InvoicesPanel";
import { CreditNotesPanel } from "@/components/billing/CreditNotesPanel";
import { BillingProfilePanel } from "@/components/billing/BillingProfilePanel";
import { PaymentMethodsPanel } from "@/components/payments/PaymentMethodsPanel";
import { PaymentsPanel } from "@/components/payments/PaymentsPanel";
import { AccountPanel } from "@/components/payments/AccountPanel";
import { AvailabilityPanel } from "@/components/availability/AvailabilityPanel";
import { AvailabilityCommitmentsPanel } from "@/components/availability/AvailabilityCommitmentsPanel";
import { ServiceCreditsPanel } from "@/components/availability/ServiceCreditsPanel";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { loginHref } from "@/lib/authRedirect";
import { Permission } from "@/lib/permissions";
import { cn } from "@/lib/utils";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

interface Section {
  name: string;
  permission?: Permission;
  anyPermission?: Permission[];
}

interface SectionGroupSpec {
  labelKey: string;
  sections: Section[];
}

const SECTION_GROUPS: SectionGroupSpec[] = [
  { labelKey: "organization.groupOrganization", sections: [{ name: "Details" }, { name: "Sandbox", permission: "sandbox:read" }] },
  {
    labelKey: "organization.groupCasePolicy",
    sections: [
      { name: "Hold Policy", permission: "organizations:write" },
      { name: "Alert Policy", permission: "organizations:write" },
      { name: "SLA Targets", permission: "sla-targets:read" },
      { name: "Escalation Rules", permission: "escalation-rules:read" },
      { name: "Approval Policy", permission: "approval-policies:read" },
      {
        name: "Filing Policy",
        permission: "suspicious-activity-report-policies:read",
      },
      { name: "Reason Codes", permission: "case-reason-codes:read" },
    ],
  },
  {
    labelKey: "organization.groupTransactionData",
    sections: [
      { name: "Transaction Schema", permission: "transaction-schemas:read" },
      { name: "Party Mapping", permission: "party-mappings:read" },
      { name: "Velocity Profile", permission: "velocity-profiles:read" },
    ],
  },
  {
    labelKey: "organization.groupDelivery",
    sections: [
      { name: "API Clients", permission: "api-clients:read" },
      {
        name: "Notifications",
        permission: "notification-subscriptions:read",
      },
      {
        name: "Webhook Configurations",
        permission: "webhook-configurations:read",
      },
      {
        name: "Webhook Deliveries",
        permission: "webhook-configurations:read",
      },
    ],
  },
  {
    labelKey: "organization.groupPeople",
    sections: [
      { name: "Members", permission: "users:read" },
      { name: "Invitations", permission: "invitations:read" },
    ],
  },
  {
    labelKey: "organization.groupAccess",
    sections: [
      { name: "Roles", permission: "roles:read" },
      { name: "Access", permission: "roles:read" },
      { name: "Business Units", permission: "sub-organizations:read" },
    ],
  },
  {
    labelKey: "organization.groupBilling",
    sections: [
      { name: "Current Plan", permission: "subscriptions:read" },
      { name: "Usage", permission: "usages:read" },
      { name: "Invoices", permission: "invoices:read" },
      { name: "Credit Notes", permission: "credit-notes:read" },
      { name: "Billing Details", permission: "billing-profiles:read" },
      { name: "Payment Methods", permission: "payment-methods:read" },
      { name: "Payment History", permission: "payments:read" },
      { name: "Account", permission: "payments:read" },
    ],
  },
  {
    labelKey: "organization.groupAvailability",
    sections: [
      { name: "Availability", permission: "availability:read" },
      {
        name: "Agreed Targets",
        permission: "availability-commitments:read",
      },
      { name: "Service Credits", permission: "service-credits:read" },
    ],
  },
  {
    labelKey: "organization.groupRecords",
    sections: [
      {
        name: "Audit Trail",
        anyPermission: ["audit-logs:verify", "audit-logs:export"],
      },
    ],
  },
];

export default function MyOrganization() {
  const t = useTranslation();
  const { user } = useAuth();
  const { holds, holdsAny } = usePermissions();
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;
  const goToLogin = useCallback(() => {
    router.push(loginHref(redirectUri));
  }, [router, redirectUri]);

  const organization = user?.organization;
  const handOffToAdminView = Boolean(
    organization && holds("platform:administer")
  );

  const sectionGroups: SectionGroup[] = SECTION_GROUPS.map((group) => ({
    labelKey: group.labelKey,
    sections: group.sections
      .filter((section) =>
        section.anyPermission
          ? holdsAny(...section.anyPermission)
          : !section.permission || holds(section.permission)
      )
      .map((section) => section.name),
  })).filter((group) => group.sections.length > 0);

  const tabs = sectionGroups.flatMap((group) => group.sections);
  const hasSections = tabs.length > 1;
  const [activeTab, setActiveTab] = useState("Details");

  useEffect(() => {
    const fromHash = decodeURIComponent(window.location.hash.replace("#", ""));
    if (fromHash && tabs.includes(fromHash)) setActiveTab(fromHash);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tabs.join("|")]);

  const handleTabChange = (tab: string) => {
    setActiveTab(tab);
    window.history.replaceState(null, "", `#${encodeURIComponent(tab)}`);
  };

  useEffect(() => {
    if (handOffToAdminView && organization) {
      router.replace(
        `/admin/organizations/${organization.organizationId}${window.location.hash}`
      );
    }
  }, [handOffToAdminView, organization, router]);

  if (!organization) {
    return (
      <NotFound
        title={t("organization.noOrganizationYet")}
        description={t("organization.yourAccountIsnTIn")}
        backHref="/profile"
        backLabel={t("organization.goToProfile")}
      />
    );
  }

  if (handOffToAdminView) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-56" />
        <Skeleton className="h-64 w-full max-w-2xl" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow={t("organization.organization")}
        title={organization.friendlyName}
        description={t("organization.theOrganizationYourRulesCases")}
      />

      <div
        className={cn(
          "grid gap-6",
          hasSections && "lg:grid-cols-[14rem_minmax(0,1fr)] lg:items-start"
        )}
      >
        {hasSections && (
          <SectionNav
            groups={sectionGroups}
            active={activeTab}
            onChange={handleTabChange}
          />
        )}
        {/* `min-w-0`, or a wide table inside a panel stretches the grid column
            instead of scrolling within it. */}
        <div className="min-w-0 space-y-6">
          {activeTab === "Details" && (
            <Card>
              <CardContent className="p-0">
                <FieldList>
                  <Field label={t("common.status")}>
                    <StatusBadge status={organization.status} />
                  </Field>
                  <CurrentPlanFields
                    organizationId={organization.organizationId}
                  />
                  <Field label={t("common.created")} mono>
                    <LocalTime value={organization.createdAt} />
                  </Field>
                  <Field label={t("common.updated")} mono>
                    <LocalTime value={organization.updatedAt} />
                  </Field>
                  <Field label={t("organization.organizationId")} mono>
                    <span className="break-all text-xs text-slate-500">
                      {organization.organizationId}
                    </span>
                  </Field>
                </FieldList>
              </CardContent>
            </Card>
          )}

          {activeTab === "Sandbox" && <SandboxPanel />}

          {activeTab === "Current Plan" && (
            <CurrentPlanPanel organizationId={user!.organizationId} />
          )}

          {activeTab === "Usage" && (
            <UsagePanel onUnauthorized={goToLogin} />
          )}

          {activeTab === "Invoices" && (
            <InvoicesPanel
              organizationId={user!.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Credit Notes" && (
            <CreditNotesPanel
              organizationId={user!.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Billing Details" && (
            <BillingProfilePanel
              organizationId={user!.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Payment Methods" && (
            <PaymentMethodsPanel
              organizationId={user!.organizationId}
              returnPath="/organization#Payment%20Methods"
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Payment History" && (
            <PaymentsPanel
              organizationId={user!.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Account" && (
            <AccountPanel
              organizationId={user!.organizationId}
              invoiceHref={() =>
                holds("invoices:read") ? "/organization#Invoices" : null
              }
              creditNoteHref={() =>
                holds("credit-notes:read") ? "/organization#Credit%20Notes" : null
              }
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Availability" && (
            <AvailabilityPanel
              organizationId={user!.organizationId}
              ownOrganization
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Agreed Targets" && (
            <AvailabilityCommitmentsPanel
              organizationId={user!.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Service Credits" && (
            <ServiceCreditsPanel
              organizationId={user!.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Hold Policy" && <HoldPolicyPanel />}

          {activeTab === "Alert Policy" && <MyAlertPolicyPanel />}

          {activeTab === "SLA Targets" && (
            <SlaTargetsPanel
              organizationId={user!.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Escalation Rules" && (
            <EscalationRulesPanel
              organizationId={user!.organizationId}
              detailHref={(escalationRuleId) =>
                `/organization/escalation-rules/${escalationRuleId}`
              }
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Approval Policy" && (
            <ApprovalPolicyPanel
              organizationId={user!.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Filing Policy" && (
            <SuspiciousActivityReportPolicyPanel
              organizationId={user!.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Transaction Schema" && (
            <TransactionSchemaPanel
              organizationSegment="my"
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Party Mapping" && (
            <PartyMappingPanel organizationSegment="my" onUnauthorized={goToLogin} />
          )}

          {activeTab === "Velocity Profile" && (
            <VelocityProfilePanel
              organizationSegment="my"
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Reason Codes" && (
            <ReasonCodesPanel
              organizationId={user!.organizationId}
              detailHref={(caseReasonCodeId) =>
                `/organization/reason-codes/${caseReasonCodeId}`
              }
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Members" && (
            <TeamMembersPanel
              organizationId={user!.organizationId}
              detailHref={(userId) =>
                holds("roles:read")
                  ? `/organization/users/${userId}/access`
                  : null
              }
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Invitations" && (
            <InvitationsPanel
              organizationId={user!.organizationId}
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Roles" && (
            <RolesPanel
              organizationId={user!.organizationId}
              detailHref={(roleId) => `/organization/roles/${roleId}`}
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Access" && (
            <RoleAssignmentsPanel
              organizationId={user!.organizationId}
              roleHref={(roleId) => `/organization/roles/${roleId}`}
              userHref={(userId) => `/organization/users/${userId}/access`}
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Business Units" && (
            <SubOrganizationsPanel
              organizationId={user!.organizationId}
              detailHref={(subOrganizationId) =>
                `/organization/business-units/${subOrganizationId}`
              }
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "API Clients" && <ApiClientsPanel />}

          {activeTab === "Notifications" && (
            <NotificationSubscriptionsPanel
              webhookHref="/organization#Webhook%20Configurations"
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Webhook Configurations" && (
            <WebhookConfigurationsPanel
              organizationSegment="my"
              detailHref={(webhookConfigurationId) =>
                `/organization/webhooks/${webhookConfigurationId}`
              }
              embedded
            />
          )}

          {activeTab === "Webhook Deliveries" && (
            <WebhookDeliveriesPanel
              organizationSegment="my"
              canWrite={holds("webhook-configurations:write")}
              configurationHref={(webhookConfigurationId) =>
                `/organization/webhooks/${webhookConfigurationId}`
              }
              embedded
              onUnauthorized={goToLogin}
            />
          )}

          {activeTab === "Audit Trail" && (
            <AuditChainVerificationPanel onUnauthorized={goToLogin} />
          )}
        </div>
      </div>
    </div>
  );
}
