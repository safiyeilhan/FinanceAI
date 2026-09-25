using FinanceAI.Models;

namespace FinanceAI.Pages;

public partial class BudgetsPage : ContentPage
{
    public BudgetsPage()
    {
        InitializeComponent();

        AddBudgetButton.Clicked += AddBudgetButton_Clicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        BudgetsListLayout.Children.Clear();
        var budgets = await App.Database.GetBudgetsAsync();
        var transactions = await App.Database.GetTransactionsAsync();

        // Summary
        int activeCount = budgets.Count;
        int exceeded = 0;
        double highestUsage = 0;
        string highestCategory = "-";

        foreach (var b in budgets)
        {
            var now = DateTime.Now;
            int year = b.Year == 0 ? now.Year : b.Year;
            int month = b.Month == 0 ? now.Month : b.Month;

            var spent = transactions.Where(t => !t.IsIncome && t.Category == b.Category && t.Date.Year == year && t.Date.Month == month).Sum(t => t.Amount);
            var remaining = b.MonthlyLimit - spent;
            var usagePct = b.MonthlyLimit > 0 ? (double)(spent / b.MonthlyLimit) * 100.0 : 0;

            if (usagePct > 100) exceeded++;
            if (usagePct > highestUsage)
            {
                highestUsage = usagePct;
                highestCategory = b.Category;
            }

            BudgetsListLayout.Children.Add(CreateBudgetCard(b, spent, remaining, usagePct));
        }

        BudgetsSummaryLabel.Text = $"Aktif bütçeler: {activeCount} | Aşılan: {exceeded} | En yüksek kullanım: {highestCategory} ({highestUsage:F0}%)";
    }

    private View CreateBudgetCard(Budget b, decimal spent, decimal remaining, double usagePct)
    {
        Border card = new() { BackgroundColor = Microsoft.Maui.Graphics.Colors.White, StrokeThickness = 0, Padding = 12 };
        VerticalStackLayout vs = new() { Spacing = 6 };

        vs.Add(new Label { Text = b.Category, FontSize = 16, FontAttributes = FontAttributes.Bold });
        vs.Add(new Label { Text = $"₺{b.MonthlyLimit:N2} bütçe" });
        vs.Add(new Label { Text = $"₺{spent:N2} harcandı" });
        vs.Add(new Label { Text = $"₺{remaining:N2} kaldı" });
        vs.Add(new Label { Text = $"%{usagePct:F0} kullanım" });

        ProgressBar progress = new() { Progress = (float)(Math.Min(usagePct, 200) / 100.0f), HeightRequest = 8, BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#EEEEEE") };
        vs.Add(progress);

        // Buttons
        HorizontalStackLayout actions = new() { Spacing = 8 };
        Button edit = new() { Text = "Düzenle", BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#1976D2"), TextColor = Microsoft.Maui.Graphics.Colors.White, CornerRadius = 8 };
        Button del = new() { Text = "Sil", BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E53935"), TextColor = Microsoft.Maui.Graphics.Colors.White, CornerRadius = 8 };
        actions.Add(edit);
        actions.Add(del);
        vs.Add(actions);

        edit.Clicked += async (s, e) => await EditBudget(b);
        del.Clicked += async (s, e) => await DeleteBudget(b);

        card.Content = vs;
        return card;
    }

    private async Task EditBudget(Budget b)
    {
        var newLimitStr = await DisplayPromptAsync("Bütçe Düzenle", $"{b.Category} için aylık limit (₺)", initialValue: b.MonthlyLimit.ToString());
        if (string.IsNullOrWhiteSpace(newLimitStr)) return;
        if (decimal.TryParse(newLimitStr, out var val))
        {
            b.MonthlyLimit = val;
            await App.Database.UpdateBudgetAsync(b);
            await RefreshAsync();
        }
    }

    private async Task DeleteBudget(Budget b)
    {
        bool ok = await DisplayAlertAsync("Bütçeyi Sil", $"{b.Category} bütçesini silmek istediğinize emin misiniz?", "Evet", "Hayır");
        if (!ok) return;
        await App.Database.DeleteBudgetAsync(b);
        await RefreshAsync();
    }

    private async void AddBudgetButton_Clicked(object? sender, EventArgs e)
    {
        var category = await DisplayPromptAsync("Yeni Bütçe", "Kategori adı:");
        if (string.IsNullOrWhiteSpace(category)) return;
        var limitStr = await DisplayPromptAsync("Yeni Bütçe", "Aylık limit (₺):");
        if (!decimal.TryParse(limitStr, out var limit)) return;

        var b = new Budget { Category = category.Trim(), MonthlyLimit = limit };
        await App.Database.AddBudgetAsync(b);
        await RefreshAsync();
    }
}
