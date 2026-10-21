namespace RequestsManagement.Core.Validation;

/// <summary>Shared by the single and bulk validators, so both report the same rule with the same message.</summary>
public static class RowVersionFormat
{
    public const string ErrorMessage = "'RowVersion' must be the base64 value returned by the API.";

    // SQL Server rowversion is exactly 8 bytes.
    public static bool IsValid(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        Span<byte> buffer = stackalloc byte[16];
        return Convert.TryFromBase64String(value, buffer, out var written) && written == 8;
    }
}
