namespace GhostRdp.Core.Validation;

public sealed record ValidationResult(bool IsValid, string? Error)
{
    public static ValidationResult Success() => new(true, null);

    public static ValidationResult Failure(string error) => new(false, error);
}
