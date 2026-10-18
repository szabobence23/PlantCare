using System.Text.RegularExpressions;

namespace PlantCare.Api.Validation;

/// <summary>Registration input rules (spec AC1).</summary>
public static partial class AuthInputValidator
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const int EmailMaxLength = 254;

    public static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

    public static Dictionary<string, string[]> ValidateRegistration(string? email, string? password)
    {
        var errors = new Dictionary<string, string[]>();
        var normalized = NormalizeEmail(email);

        if (normalized.Length == 0)
            errors["email"] = ["Az e-mail cím megadása kötelező."];
        else if (normalized.Length > EmailMaxLength || !EmailPattern().IsMatch(normalized))
            errors["email"] = ["Érvénytelen e-mail cím."];

        if (string.IsNullOrEmpty(password))
            errors["password"] = ["A jelszó megadása kötelező."];
        else if (password.Length < PasswordMinLength)
            errors["password"] = [$"A jelszónak legalább {PasswordMinLength} karakterből kell állnia."];
        else if (password.Length > PasswordMaxLength)
            errors["password"] = [$"A jelszó legfeljebb {PasswordMaxLength} karakter lehet."];

        return errors;
    }

    public static Dictionary<string, string[]> ValidateLogin(string? email, string? password)
    {
        var errors = new Dictionary<string, string[]>();
        if (NormalizeEmail(email).Length == 0)
            errors["email"] = ["Az e-mail cím megadása kötelező."];
        if (string.IsNullOrEmpty(password))
            errors["password"] = ["A jelszó megadása kötelező."];
        return errors;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
