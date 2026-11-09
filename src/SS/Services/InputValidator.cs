using System.Globalization;
using System.Text.RegularExpressions;

namespace SS.Services;

/// <summary>
/// Validaciones estrictas y compartidas para los datos numéricos del sistema.
/// Regla de la aplicación: separador decimal SIEMPRE punto (.), nunca coma.
/// </summary>
public static class InputValidator
{
    private const string PricePattern = @"^\d+(\.\d{1,2})?$";
    private const string IntegerPattern = @"^\d+$";

    public const string PriceDecimalHint = "Usá punto (.) para los decimales, no coma. Ej: 10.98";

    public static bool TryParsePrice(string? text, out decimal price, out string error)
    {
        price = 0m;
        var raw = (text ?? string.Empty).Trim();

        if (raw.Length == 0)
        {
            error = "El precio es obligatorio.";
            return false;
        }

        if (raw.Contains(','))
        {
            error = PriceDecimalHint;
            return false;
        }

        if (!Regex.IsMatch(raw, PricePattern, RegexOptions.CultureInvariant))
        {
            error = "El precio debe ser un número válido con hasta 2 decimales. Ej: 10.98";
            return false;
        }

        if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out price))
        {
            error = "El precio debe ser un número válido. Ej: 10.98";
            return false;
        }

        if (price <= 0m)
        {
            error = "El precio debe ser mayor a 0.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryParseStock(string? text, out int stock, out string error)
    {
        stock = 0;
        var raw = (text ?? string.Empty).Trim();

        if (raw.Length == 0)
        {
            error = "El stock es obligatorio.";
            return false;
        }

        if (!Regex.IsMatch(raw, IntegerPattern, RegexOptions.CultureInvariant))
        {
            error = "El stock debe ser un número entero, sin decimales ni puntos.";
            return false;
        }

        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out stock))
        {
            error = "El stock es demasiado grande.";
            return false;
        }

        if (stock < 1)
        {
            error = "El stock debe ser mayor o igual a 1.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool TryParseMinStock(string? text, out int minStock, out string error)
    {
        minStock = 0;
        var raw = (text ?? string.Empty).Trim();

        if (raw.Length == 0)
        {
            error = "El stock mínimo es obligatorio.";
            return false;
        }

        if (!Regex.IsMatch(raw, IntegerPattern, RegexOptions.CultureInvariant))
        {
            error = "El stock mínimo debe ser un número entero, sin decimales ni puntos.";
            return false;
        }

        if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out minStock))
        {
            error = "El stock mínimo es demasiado grande.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>Monto de dinero: punto decimal, hasta 2 decimales, mayor o igual a 0.</summary>
    public static bool TryParseMoney(string? text, out decimal amount, out string error)
    {
        amount = 0m;
        var raw = (text ?? string.Empty).Trim();

        if (raw.Length == 0)
        {
            error = "El monto es obligatorio.";
            return false;
        }

        if (raw.Contains(','))
        {
            error = PriceDecimalHint;
            return false;
        }

        if (!Regex.IsMatch(raw, PricePattern, RegexOptions.CultureInvariant))
        {
            error = "El monto debe ser un número válido con hasta 2 decimales. Ej: 100.00";
            return false;
        }

        if (!decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount))
        {
            error = "El monto debe ser un número válido. Ej: 100.00";
            return false;
        }

        if (amount < 0m)
        {
            error = "El monto no puede ser negativo.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
