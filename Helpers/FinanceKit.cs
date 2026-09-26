using System.Globalization;

namespace FinanceAI.Helpers;

public static class Palette
{
    public static readonly Color Ink = Color.FromArgb("#10231C");
    public static readonly Color Muted = Color.FromArgb("#5E726A");
    public static readonly Color Background = Color.FromArgb("#F3F6F4");
    public static readonly Color Accent = Color.FromArgb("#0E7C66");
    public static readonly Color AccentSoft = Color.FromArgb("#E5F4EF");
    public static readonly Color Income = Color.FromArgb("#0E7C66");
    public static readonly Color Expense = Color.FromArgb("#D14343");
    public static readonly Color Warning = Color.FromArgb("#B45309");
    public static readonly Color Line = Color.FromArgb("#E3EBE7");
}

public static class FinanceFormat
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("tr-TR");

    public static string Money(decimal value) => $"₺{value.ToString("N2", Culture)}";

    public static string SignedMoney(decimal value, bool isIncome)
    {
        var sign = isIncome ? "+" : "−";
        return $"{sign}{Money(value)}";
    }

    public static string Day(DateTime date) => date.ToString("dd MMM yyyy", Culture);
}

public static class MoneyParser
{
    public static bool TryParse(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var source = text.Trim()
            .Replace("₺", "", StringComparison.Ordinal)
            .Replace("TL", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "", StringComparison.Ordinal)
            .Replace("\u00A0", "", StringComparison.Ordinal);

        if (source.Length == 0)
            return false;

        int lastComma = source.LastIndexOf(',');
        int lastDot = source.LastIndexOf('.');

        if (lastComma >= 0 && lastDot >= 0)
        {
            source = lastComma > lastDot
                ? source.Replace(".", "", StringComparison.Ordinal).Replace(',', '.')
                : source.Replace(",", "", StringComparison.Ordinal);
        }
        else if (lastComma >= 0)
        {
            int digitsAfter = source.Length - lastComma - 1;
            source = digitsAfter is 1 or 2
                ? source.Replace(',', '.')
                : source.Replace(",", "", StringComparison.Ordinal);
        }
        else if (lastDot >= 0)
        {
            int digitsAfter = source.Length - lastDot - 1;
            if (digitsAfter is not (1 or 2))
                source = source.Replace(".", "", StringComparison.Ordinal);
        }

        return decimal.TryParse(source, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }
}

public static class Categories
{
    public static readonly string[] Income = ["Maaş", "Freelance", "Yatırım", "Hediye", "Diğer"];

    public static readonly string[] Expense =
    [
        "Market", "Yemek", "Ulaşım", "Kira", "Fatura",
        "Abonelik", "Eğitim", "Sağlık", "Eğlence", "Alışveriş", "Diğer"
    ];

    public static IReadOnlyList<string> For(bool isIncome) => isIncome ? Income : Expense;
}
