using System.Net;
using System.Reflection;
using Core.Configs;
using Core.Enums;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Jobs;
using Core.Models.Common;
using Core.Services;
using FluentValidation;
using Infrastructure;
using Infrastructure.BackgroundServices;
using Infrastructure.Constants;
using Infrastructure.Entities;
using Infrastructure.Interceptors;
using Infrastructure.Probes;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Presentation.Availability;
using Presentation.Constants;
using Presentation.ExceptionHandlers;
using Presentation.Factories;
using Presentation.OperationFilters;
using Presentation.RateLimiting;
using Presentation.Telemetry;
using Quartz;
using Quartz.AspNetCore;
using Quartz.Impl.AdoJobStore;
using QuestPDF;
using QuestPDF.Infrastructure;
using Serilog;
using StackExchange.Redis;
using HeaderNames = Presentation.Constants.HeaderNames;

namespace Presentation.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterConfigurations(this IServiceCollection services, ConfigurationManager configuration)
    {
        var config = configuration.AddEnvironmentVariables().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddOptions<AppConfig>().BindConfiguration(nameof(AppConfig));

        return services;
    }

    public static IServiceCollection RegisterServices(this IServiceCollection services)
    {
        Settings.License = LicenseType.Community;
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRandomProvider, RandomProvider>();

        services.AddScoped<IApiClientService, ApiClientService>();
        services.AddSingleton<IApiVersionService, ApiVersionService>();
        services.AddSingleton<ApiVersionUsageMeter>();
        services.AddScoped<IRateLimitOverrideService, RateLimitOverrideService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IRuleService, RuleService>();
        services.AddScoped<IRuleVersionService, RuleVersionService>();
        services.AddScoped<IRuleDeploymentService, RuleDeploymentService>();
        services.AddScoped<IRuleSetService, RuleSetService>();
        services.AddScoped<IRuleEvaluationService, RuleEvaluationService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IUsageService, UsageService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IContractDiscountService, ContractDiscountService>();
        services.AddScoped<IInvoiceAdjustmentService, InvoiceAdjustmentService>();
        services.AddScoped<ICommitmentService, CommitmentService>();
        services.AddScoped<IBillingEntityService, BillingEntityService>();
        services.AddScoped<IBillingProfileService, BillingProfileService>();
        services.AddScoped<IExchangeRateService, ExchangeRateService>();
        services.AddScoped<IServiceCostRateService, ServiceCostRateService>();
        services.AddScoped<IRevenueService, RevenueService>();
        services.AddScoped<IExchangeRateFeed, EuropeanCentralBankExchangeRateFeed>();
        services.AddScoped<ICreditNoteService, CreditNoteService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IDunningService, DunningService>();

        services.AddScoped<IPaymentProvider>(serviceProvider =>
        {
            var appConfig = serviceProvider.GetRequiredService<IOptions<AppConfig>>();

            return string.IsNullOrWhiteSpace(appConfig.Value.PaymentConfig.ApiKey)
                ? new DisabledPaymentProvider()
                : new StripePaymentProvider(appConfig);
        });

        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IApplicationUserService, ApplicationUserService>();
        services.AddScoped<IOrganizationUserService, OrganizationUserService>();
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IDecisionService, DecisionService>();
        services.AddScoped<IOutboxMessageService, OutboxMessageService>();
        services.AddScoped<IWebhookConfigurationService, WebhookConfigurationService>();
        services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();
        services.AddScoped<IWebhookHealthService, WebhookHealthService>();
        services.AddScoped<IWebhookCatalogueService, WebhookCatalogueService>();
        services.AddScoped<IDisplayPreferenceService, DisplayPreferenceService>();
        services.AddScoped<ITransactionSchemaService, TransactionSchemaService>();
        services.AddScoped<IPayloadProtectionService, PayloadProtectionService>();
        services.AddScoped<IPartyMappingService, PartyMappingService>();
        services.AddScoped<IPartyService, PartyService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IVelocityProfileService, VelocityProfileService>();
        services.AddScoped<ISimulationService, SimulationService>();
        services.AddScoped<ISandboxService, SandboxService>();
        services.AddScoped<ISandboxCreditService, SandboxCreditService>();
        services.AddScoped<IVelocityService, VelocityService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<ICaseNoteService, CaseNoteService>();
        services.AddScoped<ICaseReasonCodeService, CaseReasonCodeService>();
        services.AddScoped<IQueueService, QueueService>();
        services.AddScoped<IEscalationRuleService, EscalationRuleService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IAttachmentService, AttachmentService>();
        services.AddScoped<ICaseLinkService, CaseLinkService>();
        services.AddScoped<ISuspiciousActivityReportService, SuspiciousActivityReportService>();
        services.AddScoped<IAttachmentStore, FileSystemAttachmentStore>();
        services.AddScoped<IVirusScanner, SignatureVirusScanner>();
        services.AddScoped<INotificationSubscriptionService, NotificationSubscriptionService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IRoleAssignmentService, RoleAssignmentService>();
        services.AddScoped<ISubOrganizationService, SubOrganizationService>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();

        // WebhookService is also bound to IWebhookService for the configuration test-send.
        services.AddScoped<INotificationSender, WebhookService>();
        services.AddScoped<INotificationSender, EmailNotificationSender>();
        services.AddScoped<INotificationSender, ChatNotificationSender>();
        services.AddScoped<IQueueRoutingService, QueueRoutingService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IBackTestService, BackTestService>();
        services.AddScoped<IBackTestResultService, BackTestResultService>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<IStatusService, StatusService>();
        services.AddScoped<IIncidentService, IncidentService>();
        services.AddScoped<IMaintenanceWindowService, MaintenanceWindowService>();
        services.AddScoped<IStatusSubscriptionService, StatusSubscriptionService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<IOffboardingService, OffboardingService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IAvailabilityCommitmentService, AvailabilityCommitmentService>();
        services.AddScoped<IServiceCreditService, ServiceCreditService>();
        services.AddScoped<IStatusNotifier, EmailStatusNotifier>();
        services.AddScoped<IInvitationNotifier, EmailInvitationNotifier>();

        services.AddScoped<IServiceComponentProbe, ApiProbe>();
        services.AddScoped<IServiceComponentProbe, RuleEvaluationProbe>();
        services.AddScoped<IServiceComponentProbe, WebhookProbe>();
        services.AddScoped<IServiceComponentProbe, BackgroundProcessingProbe>();
        services.AddScoped<IServiceComponentProbe, ReportingProbe>();

        // Singleton: it is this instance's in-memory tally.
        services.AddSingleton<AvailabilityRecorder>();
        services.AddHostedService<AvailabilityFlushService>();

        services.AddHostedService<OutboxBackgroundService>();

        return services;
    }

    public static IServiceCollection RegisterResponseFactories(this IServiceCollection services)
    {
        services.AddScoped<RelatedRecordLoader>();

        services.AddScoped<ApiClientResponseFactory>();
        services.AddScoped<InvitationResponseFactory>();
        services.AddScoped<AuditLogResponseFactory>();
        services.AddScoped<BackTestResponseFactory>();
        services.AddScoped<CaseNoteResponseFactory>();
        services.AddScoped<CaseResponseFactory>();
        services.AddScoped<CaseReasonCodeResponseFactory>();
        services.AddScoped<QueueResponseFactory>();
        services.AddScoped<EscalationRuleResponseFactory>();
        services.AddScoped<ApprovalResponseFactory>();
        services.AddScoped<AttachmentResponseFactory>();
        services.AddScoped<CaseLinkResponseFactory>();
        services.AddScoped<SuspiciousActivityReportResponseFactory>();
        services.AddScoped<NotificationSubscriptionResponseFactory>();
        services.AddScoped<AlertResponseFactory>();
        services.AddScoped<DecisionResponseFactory>();
        services.AddScoped<EvaluateResponseFactory>();
        services.AddScoped<InvoiceResponseFactory>();
        services.AddScoped<RuleDeploymentResponseFactory>();
        services.AddScoped<RuleEvaluationResponseFactory>();
        services.AddScoped<RuleResponseFactory>();
        services.AddScoped<RuleSetResponseFactory>();
        services.AddScoped<TransactionResponseFactory>();
        services.AddScoped<UserResponseFactory>();
        services.AddScoped<WebhookConfigurationResponseFactory>();
        services.AddScoped<TransactionSchemaResponseFactory>();
        services.AddScoped<PartyMappingResponseFactory>();
        services.AddScoped<VelocityProfileResponseFactory>();
        services.AddScoped<SimulationResponseFactory>();
        services.AddScoped<WebhookDeliveryResponseFactory>();
        services.AddScoped<PartyResponseFactory>();

        return services;
    }

    public static IServiceCollection RegisterQuartz(this IServiceCollection services, ConfigurationManager configuration)
    {
        var appConfig = new AppConfig();
        configuration.GetSection(nameof(AppConfig)).Bind(appConfig);
        var backgroundServiceConfig = appConfig.BackgroundServiceConfig;

        services.Configure<QuartzOptions>(options =>
        {
            options.Scheduling.OverWriteExistingData = true;
            options.Scheduling.ScheduleTriggerRelativeToReplacedTrigger = true;
        });

        services.AddQuartz(configurator =>
        {
            configurator.UseDefaultThreadPool(options => options.MaxConcurrency = backgroundServiceConfig.MaxConcurrency);
            configurator.MaxBatchSize = backgroundServiceConfig.MaxBatchSize;

            // Unique per process: clustered instances sharing an id are read as one node.
            configurator.SchedulerId = "AUTO";

            configurator.UsePersistentStore(options =>
            {
                options.UseProperties = true;
                options.UseSystemTextJsonSerializer();

                options.UseClustering(clustering =>
                {
                    clustering.CheckinInterval = TimeSpan.FromSeconds(backgroundServiceConfig.ClusterCheckinIntervalSeconds);
                });

                options.UsePostgres(TransactionMonitoringDbContext.DatabaseConnectionName, postgresOptions =>
                {
                    postgresOptions.UseDriverDelegate<PostgreSQLDelegate>();
                    postgresOptions.ConnectionStringName = TransactionMonitoringDbContext.DatabaseConnectionName;
                    postgresOptions.TablePrefix = $"\"{Schemas.Quartz}\".";
                });
            });

            configurator.AddHttpApi(options =>
            {
                options.ApiPath = "/quartz-api";
                options.IncludeStackTraceInProblemDetails = true;
            });

            configurator.ScheduleJob<GenerateInvoiceJob>(options =>
            {
                options
                    .ForJob(nameof(GenerateInvoiceJob))
                    .WithIdentity($"{nameof(GenerateInvoiceJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.MonthlyOnDayAndHourAndMinute(1, 0, 0)
                        .WithMisfireHandlingInstructionFireAndProceed());
            });

            configurator.ScheduleJob<CollectPaymentJob>(options =>
            {
                options
                    .ForJob(nameof(CollectPaymentJob))
                    .WithIdentity($"{nameof(CollectPaymentJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(6, 0));
            });

            configurator.ScheduleJob<AdvanceDunningJob>(options =>
            {
                options
                    .ForJob(nameof(AdvanceDunningJob))
                    .WithIdentity($"{nameof(AdvanceDunningJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(7, 0));
            });

            // Evening: the ECB publishes around 16:00 CET.
            configurator.ScheduleJob<RecordExchangeRateJob>(options =>
            {
                options
                    .ForJob(nameof(RecordExchangeRateJob))
                    .WithIdentity($"{nameof(RecordExchangeRateJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(17, 0)
                        .WithMisfireHandlingInstructionFireAndProceed());
            });

            configurator.ScheduleJob<PurgeIdempotencyRecordJob>(options =>
            {
                options
                    .ForJob(nameof(PurgeIdempotencyRecordJob))
                    .WithIdentity($"{nameof(PurgeIdempotencyRecordJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(1, 0));
            });

            configurator.ScheduleJob<ExpireHeldDecisionJob>(options =>
            {
                options
                    .ForJob(nameof(ExpireHeldDecisionJob))
                    .WithIdentity($"{nameof(ExpireHeldDecisionJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.CronSchedule("0 * * * * ?")
                        .WithMisfireHandlingInstructionFireAndProceed());
            });

            configurator.ScheduleJob<EscalateCaseJob>(options =>
            {
                options
                    .ForJob(nameof(EscalateCaseJob))
                    .WithIdentity($"{nameof(EscalateCaseJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.CronSchedule("0 * * * * ?")
                        .WithMisfireHandlingInstructionFireAndProceed());
            });

            configurator.ScheduleJob<NotifyBreachedCaseJob>(options =>
            {
                options
                    .ForJob(nameof(NotifyBreachedCaseJob))
                    .WithIdentity($"{nameof(NotifyBreachedCaseJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.CronSchedule("0 * * * * ?")
                        .WithMisfireHandlingInstructionFireAndProceed());
            });

            configurator.ScheduleJob<NotifyOverdueFilingJob>(options =>
            {
                options
                    .ForJob(nameof(NotifyOverdueFilingJob))
                    .WithIdentity($"{nameof(NotifyOverdueFilingJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.CronSchedule("0 0 * * * ?")
                        .WithMisfireHandlingInstructionFireAndProceed());
            });

            configurator.ScheduleJob<AnchorAuditChainJob>(options =>
            {
                options
                    .ForJob(nameof(AnchorAuditChainJob))
                    .WithIdentity($"{nameof(AnchorAuditChainJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(2, 0));
            });

            // Must match StatusConstants.ProbeIntervalSeconds, which the heartbeat measures staleness against.
            configurator.ScheduleJob<ProbeServiceComponentJob>(options =>
            {
                options
                    .ForJob(nameof(ProbeServiceComponentJob))
                    .WithIdentity($"{nameof(ProbeServiceComponentJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.CronSchedule("0 * * * * ?")
                        .WithMisfireHandlingInstructionFireAndProceed());
            });

            configurator.ScheduleJob<TransitionMaintenanceWindowJob>(options =>
            {
                options
                    .ForJob(nameof(TransitionMaintenanceWindowJob))
                    .WithIdentity($"{nameof(TransitionMaintenanceWindowJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.CronSchedule("0 * * * * ?")
                        .WithMisfireHandlingInstructionFireAndProceed());
            });

            configurator.ScheduleJob<RollUpComponentUptimeJob>(options =>
            {
                options
                    .ForJob(nameof(RollUpComponentUptimeJob))
                    .WithIdentity($"{nameof(RollUpComponentUptimeJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(0, 15)
                        .WithMisfireHandlingInstructionFireAndProceed());
            });

            configurator.ScheduleJob<IssueServiceCreditJob>(options =>
            {
                options
                    .ForJob(nameof(IssueServiceCreditJob))
                    .WithIdentity($"{nameof(IssueServiceCreditJob)}Trigger")
                    .WithSchedule(CronScheduleBuilder.MonthlyOnDayAndHourAndMinute(2, 3, 0)
                        .WithMisfireHandlingInstructionFireAndProceed());
            });
        });

        services.AddQuartzHostedService();

        return services;
    }

    public static IServiceCollection RegisterAuth(this IServiceCollection services)
    {
        services
            .AddAuthorization(options =>
            {
                foreach (var apiScope in Enum.GetValues<ApiScope>())
                {
                    var scope = ApiScopeHelper.ToScopeString(apiScope);

                    options.AddPolicy(ApiScopeHelper.ToPolicyName(apiScope), policy =>
                    {
                        policy
                            .AddAuthenticationSchemes(
                                AuthConstants.ApiClientScheme,
                                IdentityConstants.BearerScheme,
                                IdentityConstants.ApplicationScheme)
                            .RequireAuthenticatedUser()
                            .RequireAssertion(context => !context.User.IsApiClient() || context.User.HasApiScope(scope));
                    });
                }

                options.AddPolicy(AuthConstants.DefaultPolicy, policy =>
                {
                    policy
                        .AddAuthenticationSchemes(IdentityConstants.BearerScheme).RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme).RequireAuthenticatedUser();
                });
            })
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthConstants.DefaultScheme;
                options.DefaultChallengeScheme = AuthConstants.DefaultScheme;
            })
            .AddPolicyScheme(AuthConstants.DefaultScheme, string.Empty, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    if (context.Request.Cookies.ContainsKey(AuthConstants.CookieName))
                    {
                        return IdentityConstants.ApplicationScheme;
                    }

                    return IdentityConstants.BearerScheme;
                };
            })
            .AddBearerToken(IdentityConstants.BearerScheme)
            .AddBearerToken(AuthConstants.ApiClientScheme, options =>
            {
                options.BearerTokenExpiration = TimeSpan.FromHours(1);
            });

        services
            .AddIdentity<ApplicationUserEntity, ApplicationRoleEntity>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;

                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<TransactionMonitoringDbContext>()
            .AddDefaultTokenProviders()
            .AddApiEndpoints();

        services.ConfigureApplicationCookie(config =>
        {
            config.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = ctx =>
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return Task.FromResult(0);
                },
                OnRedirectToAccessDenied = ctx =>
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    return Task.FromResult(0);
                }
            };

            config.Cookie.Name = AuthConstants.CookieName;
            config.ExpireTimeSpan = TimeSpan.FromHours(8);
            config.Cookie.MaxAge = config.ExpireTimeSpan;
            config.SlidingExpiration = true;
        });

        return services;
    }

    public static IServiceCollection RegisterDataAccess(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<IModeContext, HttpModeContext>();
        services.AddScoped<ModeStampingSaveChangesInterceptor>();

        services.AddDbContext<TransactionMonitoringDbContext>((serviceProvider, opt) =>
        {
            opt.UseNpgsql(configuration.GetConnectionString(TransactionMonitoringDbContext.DatabaseConnectionName),
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

            opt.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);

            opt.AddInterceptors(serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>());
            opt.AddInterceptors(serviceProvider.GetRequiredService<ModeStampingSaveChangesInterceptor>());

            // Picks up test-registered interceptors; production registers none.
            opt.AddInterceptors(serviceProvider.GetServices<IInterceptor>());
        });

        services.AddScoped<IApiClientRepository, ApiClientRepository>();
        services.AddScoped<IRateLimitOverrideRepository, RateLimitOverrideRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditAnchorRepository, AuditAnchorRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IAccountEntryRepository, AccountEntryRepository>();
        services.AddScoped<IDunningRepository, DunningRepository>();
        services.AddScoped<IProviderEventRepository, ProviderEventRepository>();
        services.AddScoped<IRuleRepository, RuleRepository>();
        services.AddScoped<IRuleVersionRepository, RuleVersionRepository>();
        services.AddScoped<IRuleDeploymentRepository, RuleDeploymentRepository>();
        services.AddScoped<IRuleSetRepository, RuleSetRepository>();
        services.AddScoped<IRuleEvaluationRepository, RuleEvaluationRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUsageRepository, UsageRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IPlanVersionRepository, PlanVersionRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IContractDiscountRepository, ContractDiscountRepository>();
        services.AddScoped<IInvoiceAdjustmentRepository, InvoiceAdjustmentRepository>();
        services.AddScoped<ICommitmentRepository, CommitmentRepository>();
        services.AddScoped<IBillingEntityRepository, BillingEntityRepository>();
        services.AddScoped<IBillingProfileRepository, BillingProfileRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<IServiceCostRateRepository, ServiceCostRateRepository>();
        services.AddScoped<ICreditNoteRepository, CreditNoteRepository>();
        services.AddScoped<IDocumentNumberRepository, DocumentNumberRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IApplicationUserRepository, ApplicationUserRepository>();
        services.AddScoped<IOrganizationUserRepository, OrganizationUserRepository>();
        services.AddScoped<ICaseRepository, CaseRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<ICaseRuleLinkRepository, CaseRuleLinkRepository>();
        services.AddScoped<IDecisionRepository, DecisionRepository>();
        services.AddScoped<IDecisionRuleLinkRepository, DecisionRuleLinkRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IWebhookConfigurationRepository, WebhookConfigurationRepository>();
        services.AddScoped<ITransactionSchemaRepository, TransactionSchemaRepository>();
        services.AddScoped<ITransactionSchemaVersionRepository, TransactionSchemaVersionRepository>();
        services.AddScoped<IPartyRepository, PartyRepository>();
        services.AddScoped<ITransactionPartyRepository, TransactionPartyRepository>();
        services.AddScoped<IPartyMappingRepository, PartyMappingRepository>();
        services.AddScoped<IPartyMappingVersionRepository, PartyMappingVersionRepository>();
        services.AddScoped<IVelocityProfileRepository, VelocityProfileRepository>();
        services.AddScoped<IVelocityObservationRepository, VelocityObservationRepository>();
        services.AddScoped<IVelocityBackfillRepository, VelocityBackfillRepository>();
        services.AddScoped<ISimulationRepository, SimulationRepository>();
        services.AddScoped<ISandboxRepository, SandboxRepository>();
        services.AddScoped<ISandboxCreditRepository, SandboxCreditRepository>();
        services.AddScoped<ICaseNoteRepository, CaseNoteRepository>();
        services.AddScoped<ICaseReasonCodeRepository, CaseReasonCodeRepository>();
        services.AddScoped<IQueueRepository, QueueRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
        services.AddScoped<ISubOrganizationRepository, SubOrganizationRepository>();
        services.AddScoped<IPermissionScopeRepository, PermissionScopeRepository>();
        services.AddScoped<ICaseSlaTargetRepository, CaseSlaTargetRepository>();
        services.AddScoped<IEscalationRuleRepository, EscalationRuleRepository>();
        services.AddScoped<IApprovalRepository, ApprovalRepository>();
        services.AddScoped<IApprovalPolicyRepository, ApprovalPolicyRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<ICaseLinkRepository, CaseLinkRepository>();
        services.AddScoped<ISuspiciousActivityReportRepository, SuspiciousActivityReportRepository>();
        services.AddScoped<ISuspiciousActivityReportPolicyRepository, SuspiciousActivityReportPolicyRepository>();
        services.AddScoped<IOrganizationDataKeyRepository, OrganizationDataKeyRepository>();
        services.AddScoped<INotificationSubscriptionRepository, NotificationSubscriptionRepository>();
        services.AddScoped<IQueueMemberRepository, QueueMemberRepository>();
        services.AddScoped<IBackTestRepository, BackTestRepository>();
        services.AddScoped<IBackTestResultRepository, BackTestResultRepository>();
        services.AddScoped<IIdempotencyRecordRepository, IdempotencyRecordRepository>();
        services.AddScoped<IComponentProbeRepository, ComponentProbeRepository>();
        services.AddScoped<IComponentUptimeDayRepository, ComponentUptimeDayRepository>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IMaintenanceWindowRepository, MaintenanceWindowRepository>();
        services.AddScoped<IStatusSubscriptionRepository, StatusSubscriptionRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IAvailabilityWindowRepository, AvailabilityWindowRepository>();
        services.AddScoped<IAvailabilityCommitmentRepository, AvailabilityCommitmentRepository>();
        services.AddScoped<IServiceCreditRepository, ServiceCreditRepository>();

        services.AddScoped<IUnitOfWork>(x => x.GetRequiredService<TransactionMonitoringDbContext>());

        return services;
    }

    public static IServiceCollection RegisterValidators(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }

    public static IServiceCollection RegisterExceptionHandlers(this IServiceCollection services)
    {
        services.AddExceptionHandler<BadHttpRequestExceptionHandler>();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<DomainExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static IServiceCollection RegisterHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddDbContextCheck<TransactionMonitoringDbContext>();

        services.AddQuartzHealthChecks();

        return services;
    }

    public static IServiceCollection RegisterRateLimiting(this IServiceCollection services, ConfigurationManager configuration)
    {
        var appConfig = new AppConfig();
        configuration.GetSection(nameof(AppConfig)).Bind(appConfig);

        var connectionString = configuration.GetConnectionString("Redis");
        var distributed = appConfig.RateLimitConfig.Distributed;

        if (distributed.Enabled && !string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var options = ConfigurationOptions.Parse(connectionString);

                options.AbortOnConnectFail = false;

                return ConnectionMultiplexer.Connect(options);
            });

            services.AddSingleton<IDistributedRateLimiter, RedisDistributedRateLimiter>();
        }
        else
        {
            if (distributed.Enabled)
            {
                Log.Warning(
                    "Distributed rate limiting is enabled but no Redis connection string is configured. "
                    + "Limits will be enforced per instance only.");
            }

            services.AddSingleton<IDistributedRateLimiter, DisabledDistributedRateLimiter>();
        }

        services.AddSingleton<RateLimitOverrideSnapshot>();
        services.AddHostedService<RateLimitOverrideRefresher>();

        // Singleton because the buckets are the state.
        services.AddSingleton<RequestRateLimiter>();

        return services;
    }

    public static IServiceCollection AddSwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.EnableAnnotations();

            options.OperationFilter<IdempotentOperationFilter>();
            options.OperationFilter<ProblemDetailsOperationFilter>();
            options.OperationFilter<ModeOperationFilter>();
            options.OperationFilter<ApiVersionOperationFilter>();

            // Document Patch<T> as the bare nullable it wraps.
            options.MapType<Patch<int?>>(() => new OpenApiSchema { Type = "integer", Nullable = true });
            options.MapType<Patch<string?>>(() => new OpenApiSchema { Type = "string", Nullable = true });

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Transaction Monitoring",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = HeaderNames.Authorization,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer"
            });

            options.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
            {
                Description = "Machine-to-machine access. Exchange a client id and secret at /oauth/token.",
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri("/oauth/token", UriKind.Relative),
                        Scopes = Enum.GetValues<ApiScope>()
                            .ToDictionary(ApiScopeHelper.ToScopeString, x => x.ToString())
                    }
                }
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    []
                },
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "OAuth2"
                        }
                    },
                    Enum.GetValues<ApiScope>().Select(ApiScopeHelper.ToScopeString).ToList()
                }
            });
        });

        return services;
    }
}
