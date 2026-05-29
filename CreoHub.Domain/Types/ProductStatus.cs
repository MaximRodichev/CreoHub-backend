namespace CreoHub.Domain.Types;

public enum ProductStatus
{
    Active           = 0,
    Hidden           = 1,
    OnModerating     = 2,
    ModerationFailed = 3,
    Archived         = 4,
    Banned           = 5,
}