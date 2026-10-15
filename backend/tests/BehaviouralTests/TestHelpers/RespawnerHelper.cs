using System.Data.Common;
using Infrastructure.Constants;
using Respawn;

namespace BehaviouralTests.TestHelpers;

public static class RespawnerHelper
{
    public static async Task<Respawner> CreateRespawnerAsync(DbConnection dbConnection)
    {
        return await Respawner.CreateAsync(dbConnection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = [Schemas.Identity, Schemas.Service],
                TablesToIgnore =
                [
                    TableNames.Roles,
                    TableNames.CaseStatuses,
                    TableNames.CaseDispositions,
                    TableNames.CaseGroupingScopes,
                    TableNames.CaseGroupingSeverityPolicies,
                    TableNames.DecisionStatuses,
                    TableNames.DecisionReasonCodes,
                    TableNames.HoldExpiryVerdicts,
                    TableNames.OrganizationStatuses,
                    TableNames.BackTestStatuses,
                    TableNames.DeploymentStrategies,
                    TableNames.RuleDeploymentStatuses,
                    TableNames.RuleActions,
                    TableNames.RuleMatchPolarities,
                    TableNames.RuleSeverities,
                    TableNames.PartyRoles,
                    TableNames.VelocityBackfillStatuses,
                    TableNames.AuditableEntityTypes,
                    TableNames.AuditOperations,
                    TableNames.ApiScopes,
                    TableNames.AlertStatuses,
                    TableNames.AlertSuppressionReasons,
                    TableNames.QueueRoutingPolicies,
                    TableNames.ApprovalStatuses,
                    TableNames.ApprovalSubjects,
                    TableNames.AttachmentScanStatuses,
                    TableNames.NotificationChannels,
                    TableNames.SuspiciousActivityReportStatuses,
                    TableNames.Permissions,
                    TableNames.PermissionScopeTypes,
                    TableNames.RateLimitPools,
                    TableNames.Modes,
                    TableNames.SimulationStatuses,
                    TableNames.SimulationScenarios,
                    TableNames.PricingModels,
                    TableNames.BillableResources,
                    TableNames.InvoiceLineTypes,
                    TableNames.DiscountTypes,
                    TableNames.Currencies,
                    TableNames.InvoiceStatuses,
                    TableNames.TaxTreatments,
                    TableNames.ServiceComponents,
                    TableNames.ProbeSources,
                    TableNames.IncidentStatuses,
                    TableNames.IncidentImpacts,
                    TableNames.MaintenanceStatuses,
                    TableNames.PaymentStatuses,
                    TableNames.PaymentMethodKinds,
                    TableNames.AccountEntryReasons,
                    TableNames.DunningStages,

                    // Seeded built-in role compositions: truncating would leave every built-in role granting nothing.
                    TableNames.RolePermissions
                ]
            });
    }
}