namespace Core.Enums;

public enum IdempotencyOutcome
{
    Reserved = 1,

    Replay,

    InProgress,

    ResourceMismatch
}
