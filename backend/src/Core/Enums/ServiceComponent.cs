namespace Core.Enums;

// Persisted as a foreign key from probes, incidents and maintenance windows: never renumber.
public enum ServiceComponent
{
    Api = 1,

    RuleEvaluation = 2,

    Webhooks = 3,

    BackgroundProcessing = 4,

    Reporting = 5
}
