using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Entities;

public class ApplicationUserEntity : DeletableIdentityUser, IAuditable
{
    public Guid OrganizationId { get; set; }
    public OrganizationEntity Organization { get; init; } = null!;
    public Guid? SubOrganizationId { get; set; }
    public SubOrganizationEntity? SubOrganization { get; init; }
    public ICollection<ApiClientEntity> ApiClients { get; init; } = [];
    public ICollection<ApiClientSecretEntity> ApiClientSecrets { get; init; } = [];
    public ICollection<InvoiceEntity> Invoices { get; init; } = [];
    public ICollection<RuleEntity> Rules { get; init; } = [];
    public ICollection<RuleSetEntity> RuleSets { get; init; } = [];
    public ICollection<CaseEntity> Cases { get; init; } = [];
    public ICollection<IdentityUserRole<Guid>> Roles { get; init; } = [];
    public ICollection<CaseNoteEntity> CaseNotes { get; init; } = [];
    public ICollection<AttachmentEntity> Attachments { get; init; } = [];
    public ICollection<CaseLinkEntity> CaseLinks { get; init; } = [];
    public ICollection<TransactionSchemaEntity> TransactionSchemas { get; init; } = [];
    public ICollection<PartyMappingEntity> PartyMappings { get; init; } = [];
    public ICollection<PartyEntity> AssessedParties { get; init; } = [];
    public ICollection<VelocityProfileEntity> VelocityProfiles { get; init; } = [];
    public ICollection<QueueMemberEntity> QueueMemberships { get; init; } = [];
    public ICollection<RoleAssignmentEntity> RoleAssignments { get; init; } = [];
    public ICollection<PlanVersionEntity> PlanVersions { get; init; } = [];
    public ICollection<SubscriptionEntity> Subscriptions { get; init; } = [];
    public ICollection<ContractDiscountEntity> ContractDiscounts { get; init; } = [];
    public ICollection<InvoiceAdjustmentEntity> InvoiceAdjustments { get; init; } = [];
    public ICollection<CommitmentEntity> Commitments { get; init; } = [];
    public string? Locale { get; set; }
    public string? TimeZone { get; set; }

    [NotMapped]
    public Guid AuditEntityId => Id;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.ApplicationUser;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.ApplicationUser;
}
