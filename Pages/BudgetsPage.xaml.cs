using FinanceAI.Helpers;
using FinanceAI.Models;

namespace FinanceAI.Pages;

public partial class BudgetsPage : ContentPage
{
    private Budget? _editing;

    public BudgetsPage()
    {
        InitializeComponent();
        foreach (string category in Categories.Expense)
            CategoryPicker.Items.Add(category);
        App.TransactionsChanged += OnTransactionsChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private void OnTransactionsChanged()
    {
        if (Handler is null)
            return;

        _ = RefreshAsync();
    }

    private async void RefreshButton_Clicked(object? sender, EventArgs e) => await RefreshAsync();

    private int _loadTicket;

    private async Task RefreshAsync()
    {
        int ticket = Interlocked.Increment(ref _loadTicket);
        try
        {
            var budgets = await App.Database.GetBudgetsAsync();
            var transactions = await App.Database.GetTransactionsAsync();
            if (ticket != _loadTicket)
                return;

            int exceeded = 0;
            double highestUsage = 0;
            string highestCategory = "—";
            var rows = new List<IReadOnlyList<View>>();

            foreach (var budget in budgets)
            {
                var (spent, remaining, usage) = Measure(budget, transactions);
                if (usage > 100)
                    exceeded++;
                if (usage > highestUsage)
                {
                    highestUsage = usage;
                    highestCategory = budget.Category;
                }

                var usageColor = usage >= 100 ? Palette.Expense : usage >= 80 ? Palette.Warning : Palette.Income;
                var captured = budget;
                rows.Add(
                [
                    DataTable.Cell(captured.Category, bold: true),
                    DataTable.Cell(FinanceFormat.Money(captured.MonthlyLimit)),
                    DataTable.Cell(FinanceFormat.Money(spent)),
                    DataTable.Cell(remaining < 0 ? FinanceFormat.Money(Math.Abs(remaining)) + " aşıldı" : FinanceFormat.Money(remaining), remaining < 0 ? Palette.Expense : Palette.Ink),
                    DataTable.Cell($"%{usage:0}", usageColor, bold: true, TextAlignment.End),
                    DataTable.Actions(() => BeginEdit(captured), () => DeleteBudgetAsync(captured))
                ]);
            }

            BudgetsSummaryLabel.Text = budgets.Count == 0
                ? "Aylık limitleri tabloda tut. Düzenle ile satırı güncelle."
                : $"{budgets.Count} bütçe · {exceeded} aşıldı · en dolu {highestCategory} (%{highestUsage:0})";

            BudgetsListLayout.Children.Clear();
            BudgetsListLayout.Children.Add(DataTable.Create(
            [
                new TableColumn("Kategori", GridLength.Star),
                new TableColumn("Limit", new GridLength(120)),
                new TableColumn("Harcanan", new GridLength(120)),
                new TableColumn("Kalan", new GridLength(150)),
                new TableColumn("Kullanım", new GridLength(90), TextAlignment.End),
                new TableColumn("İşlem", new GridLength(130), TextAlignment.End)
            ], rows, "Henüz bütçe yok."));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Veri hatası", "Bütçeler okunamadı. " + ex.Message, "Tamam");
        }
    }

    private static (decimal spent, decimal remaining, double usage) Measure(Budget budget, List<Transaction> transactions)
    {
        var now = DateTime.Now;
        int year = budget.Year == 0 ? now.Year : budget.Year;
        int month = budget.Month == 0 ? now.Month : budget.Month;

        decimal spent = transactions
            .Where(t => !t.IsIncome
                && string.Equals(t.Category, budget.Category, StringComparison.CurrentCultureIgnoreCase)
                && t.Date.Year == year
                && t.Date.Month == month)
            .Sum(t => t.Amount);

        decimal remaining = budget.MonthlyLimit - spent;
        double usage = budget.MonthlyLimit > 0 ? (double)(spent / budget.MonthlyLimit) * 100.0 : 0;
        return (spent, remaining, usage);
    }

    private Task BeginEdit(Budget budget)
    {
        _editing = budget;
        FormTitleLabel.Text = $"{budget.Category} bütçesini güncelle";
        AddBudgetButton.Text = "Kaydet";
        AppIcons.Apply(AddBudgetButton, Glyph.Save, Colors.White);
        CancelBudgetButton.IsVisible = true;
        CustomCategoryEntry.Text = budget.Category;
        LimitEntry.Text = budget.MonthlyLimit.ToString("0.##", FinanceFormat.Culture);
        CategoryPicker.SelectedIndex = -1;
        return Task.CompletedTask;
    }

    private void CancelBudgetButton_Clicked(object? sender, EventArgs e) => ClearForm();

    private void ClearForm()
    {
        _editing = null;
        FormTitleLabel.Text = "Yeni bütçe";
        AddBudgetButton.Text = "Ekle";
        AppIcons.Apply(AddBudgetButton, Glyph.Add, Colors.White);
        CancelBudgetButton.IsVisible = false;
        CustomCategoryEntry.Text = "";
        LimitEntry.Text = "";
        CategoryPicker.SelectedIndex = -1;
    }

    private async Task DeleteBudgetAsync(Budget budget)
    {
        bool ok = await DisplayAlertAsync(
            "Bütçeyi sil",
            $"{budget.Category} bütçesi silinecek. İşlemlerin silinmez.",
            "Sil",
            "Vazgeç");

        if (!ok)
            return;

        int deleted = await App.Database.DeleteBudgetAsync(budget);
        if (deleted <= 0)
        {
            await DisplayAlertAsync("Silinemedi", "Bütçe bulunamadı.", "Tamam");
            return;
        }

        if (_editing?.Id == budget.Id)
            ClearForm();

        await RefreshAsync();
        App.RaiseTransactionsChanged();
    }

    private async void AddBudgetButton_Clicked(object? sender, EventArgs e)
    {
        string? category = string.IsNullOrWhiteSpace(CustomCategoryEntry.Text)
            ? CategoryPicker.SelectedItem?.ToString()
            : CustomCategoryEntry.Text.Trim();

        if (string.IsNullOrWhiteSpace(category))
        {
            await DisplayAlertAsync("Kategori", "Listeden seç veya yeni bir kategori yaz.", "Tamam");
            return;
        }

        if (!MoneyParser.TryParse(LimitEntry.Text, out decimal limit) || limit <= 0)
        {
            await DisplayAlertAsync("Geçersiz tutar", "Sıfırdan büyük bir aylık limit gir.", "Tamam");
            return;
        }

        if (_editing != null)
        {
            _editing.Category = category;
            _editing.MonthlyLimit = limit;
            int updated = await App.Database.UpdateBudgetAsync(_editing);
            if (updated <= 0)
            {
                await DisplayAlertAsync("Kaydedilemedi", "Bütçe bulunamadı.", "Tamam");
                return;
            }

            ClearForm();
            await RefreshAsync();
            App.RaiseTransactionsChanged();
            return;
        }

        var budgets = await App.Database.GetBudgetsAsync();
        var existing = budgets.FirstOrDefault(b =>
            string.Equals(b.Category, category, StringComparison.CurrentCultureIgnoreCase)
            && b.Year == 0
            && b.Month == 0);

        if (existing != null)
        {
            bool replace = await DisplayAlertAsync(
                "Bütçe var",
                $"{existing.Category} için limit {FinanceFormat.Money(existing.MonthlyLimit)}. Bunu {FinanceFormat.Money(limit)} yapmak ister misin?",
                "Güncelle",
                "Vazgeç");

            if (!replace)
                return;

            existing.MonthlyLimit = limit;
            existing.Category = category;
            await App.Database.UpdateBudgetAsync(existing);
        }
        else
        {
            await App.Database.AddBudgetAsync(new Budget
            {
                Category = category,
                MonthlyLimit = limit,
                Year = 0,
                Month = 0,
                CreatedDate = DateTime.Now
            });
        }

        ClearForm();
        await RefreshAsync();
        App.RaiseTransactionsChanged();
    }
}
