using Microsoft.Maui.Controls.Shapes;

namespace FinanceAI.Helpers;

public enum NoticeKind
{
    Info,
    Warning,
    Danger
}

public static class Notices
{
    public const decimal UnusuallyLargeAmount = 500_000m;

    public static void SetInline(Label? label, string? message)
    {
        if (label is null)
            return;

        bool show = !string.IsNullOrWhiteSpace(message);
        label.Text = show ? message : "";
        label.IsVisible = show;
    }

    public static View Banner(string text, NoticeKind kind)
    {
        var (background, foreground, stroke) = kind switch
        {
            NoticeKind.Danger => (Color.FromArgb("#FDECEC"), Palette.Expense, Color.FromArgb("#F0D4D4")),
            NoticeKind.Warning => (Color.FromArgb("#FEF3E8"), Palette.Warning, Color.FromArgb("#F3D5B5")),
            _ => (Palette.AccentSoft, Palette.Accent, Color.FromArgb("#CDE6DE"))
        };

        return new Border
        {
            BackgroundColor = background,
            Stroke = stroke,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 14 },
            Padding = new Thickness(14, 12),
            Content = new Label
            {
                Text = text,
                TextColor = foreground,
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            }
        };
    }

    public static string AmountError(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "Tutarı yazmadın. Örnek: 1.250,50";

        if (!MoneyParser.TryParse(raw, out decimal amount))
            return "Bu tutar okunamadı. Nokta ve virgülü 1.250,50 gibi yaz.";

        if (amount <= 0)
            return "Tutar sıfır veya eksi olamaz. En az 0,01 gir.";

        return "";
    }

    public static string? LargeAmountQuestion(decimal amount, bool isIncome)
    {
        if (amount < UnusuallyLargeAmount)
            return null;

        string kind = isIncome ? "gelir" : "gider";
        return $"{FinanceFormat.Money(amount)} oldukça yüksek bir {kind}. Yanlışlıkla fazla sıfır basmadıysan devam et.";
    }

    public static string? FutureDateQuestion(DateTime date)
    {
        if (date.Date <= DateTime.Today)
            return null;

        return $"{FinanceFormat.Day(date)} henüz gelmedi. İleri tarihli kayıt tutmak istiyorsan devam et.";
    }

    public static string DeleteTransaction(Models.Transaction tx)
    {
        string kind = tx.IsIncome ? "gelir" : "gider";
        string category = string.IsNullOrWhiteSpace(tx.Category) ? "kategorisiz" : tx.Category;
        string note = string.IsNullOrWhiteSpace(tx.Description) ? "" : $" “{tx.Description.Trim()}”";
        return $"{FinanceFormat.Day(tx.Date)} tarihli {FinanceFormat.Money(tx.Amount)} {category} {kind}{note} kalıcı silinecek. Geri alınamaz.";
    }

    public static string DeleteBudget(string category) =>
        $"{category} bütçesi kalkacak. Bu aya ait işlemler durur; yalnızca limit silinir.";

    public static string DeleteGoal(string name) =>
        $"“{name}” hedefi silinecek. Biriken tutar da listeden kalkar.";

    public static string? BudgetOverspend(
        IEnumerable<Models.Budget> budgets,
        IEnumerable<Models.Transaction> transactions,
        string category,
        decimal amount,
        DateTime date,
        int? excludeId = null)
    {
        var budget = budgets.FirstOrDefault(b =>
            string.Equals(b.Category, category, StringComparison.CurrentCultureIgnoreCase)
            && (b.Year == 0 || b.Year == date.Year)
            && (b.Month == 0 || b.Month == date.Month));

        if (budget is null || budget.MonthlyLimit <= 0)
            return null;

        int year = budget.Year == 0 ? date.Year : budget.Year;
        int month = budget.Month == 0 ? date.Month : budget.Month;

        decimal spent = transactions
            .Where(t => !t.IsIncome
                && (excludeId is null || t.Id != excludeId)
                && string.Equals(t.Category, category, StringComparison.CurrentCultureIgnoreCase)
                && t.Date.Year == year
                && t.Date.Month == month)
            .Sum(t => t.Amount);

        decimal after = spent + amount;
        if (after <= budget.MonthlyLimit)
            return null;

        decimal over = after - budget.MonthlyLimit;
        return $"{category} limiti {FinanceFormat.Money(budget.MonthlyLimit)}. Bu kayıtla harcama {FinanceFormat.Money(after)} olur ({FinanceFormat.Money(over)} aşım). Yine de kaydetmek istiyor musun?";
    }

    public static string? GoalAheadOfTarget(decimal current, decimal target)
    {
        if (current <= target)
            return null;

        return $"Biriken {FinanceFormat.Money(current)}, hedefin {FinanceFormat.Money(target)} üzerinde. Hedefi tamamlandı saymak istiyorsan kaydet.";
    }

    public static string SaveFailed => "Kayıt şu an yazılamadı. Biraz sonra yeniden dene.";
    public static string LoadFailed => "Veriler okunamadı. Sayfayı yenilemek için sekmeyi tekrar aç.";
    public static string MissingRecord => "Bu kayıt artık yok. Liste yenilendiğinde kaybolmuş olabilir.";
    public static string NeedCategory => "Kategori seçmeden kayıt olmaz.";
    public static string NeedBudgetCategory => "Listeden bir kategori seç veya yeni bir ad yaz.";
    public static string NeedGoalName => "Hedefe kısa bir ad ver. Örnek: Acil durum fonu.";
    public static string NeedGoalTarget => "Hedef tutarı sıfırdan büyük olmalı. Örnek: 25.000";
    public static string NeedBudgetLimit => "Aylık limit sıfırdan büyük olmalı. Örnek: 8.000";
    public static string NeedNonNegativeSaved => "Biriken tutar boş kalabilir veya 0 ve üzeri olmalı.";
}

public static class PageNotices
{
    public static Task AlertAsync(this Page page, string title, string message) =>
        page.DisplayAlertAsync(title, message, "Tamam");

    public static Task<bool> ConfirmAsync(this Page page, string title, string message, string accept, string cancel = "Vazgeç") =>
        page.DisplayAlertAsync(title, message, accept, cancel);
}
