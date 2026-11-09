using System.Text.RegularExpressions;

namespace SS.Services;

public static class UserValidator
{
    public const int MinUsernameLength = 3;
    public const int MaxUsernameLength = 32;
    public const int MinPasswordLength = 6;
    public const int MaxPasswordLength = 64;

    private static readonly Regex UsernamePattern =
        new(@"^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant);

    public static bool TryValidateUsername(string? text, out string username, out string error)
    {
        username = (text ?? string.Empty).Trim();

        if (username.Length == 0)
        {
            error = "El usuario es obligatorio.";
            return false;
        }

        if (username.Length < MinUsernameLength)
        {
            error = $"El usuario debe tener al menos {MinUsernameLength} caracteres.";
            return false;
        }

        if (username.Length > MaxUsernameLength)
        {
            error = $"El usuario no puede superar {MaxUsernameLength} caracteres.";
            return false;
        }

        if (!UsernamePattern.IsMatch(username))
        {
            error = "El usuario solo puede tener letras, números, punto, guion y guion bajo (sin espacios ni acentos).";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidatePassword(string? password, string? confirm, out string error)
    {
        var raw = password ?? string.Empty;

        if (raw.Length == 0)
        {
            error = "La contraseña es obligatoria.";
            return false;
        }

        if (raw.Trim().Length != raw.Length)
        {
            error = "La contraseña no puede empezar ni terminar con espacios.";
            return false;
        }

        if (raw.Length < MinPasswordLength)
        {
            error = $"La contraseña debe tener al menos {MinPasswordLength} caracteres.";
            return false;
        }

        if (raw.Length > MaxPasswordLength)
        {
            error = $"La contraseña no puede superar {MaxPasswordLength} caracteres.";
            return false;
        }

        if (!string.Equals(raw, confirm ?? string.Empty, System.StringComparison.Ordinal))
        {
            error = "Las contraseñas no coinciden.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
