using FinanceAI.Models;
using Microsoft.Maui.Graphics;

namespace FinanceAI.Pages;

public partial class FinancialAssistantPage : ContentPage
{
    public FinancialAssistantPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadMetricsAsync();
    }

    private async Task LoadMetricsAsync()
    {
        List<Transaction> transactions = await App.Database.GetTransactionsAsync() ?? new();

        // prepare shared insights list
        List<string> insights = new();

        // Load budgets and compute usage per budget for this month
        var budgets = await App.Database.GetBudgetsAsync();
        var now = DateTime.Now;
        int year = now.Year;
        int month = now.Month;

        BudgetsListLayout.Children.Clear();
        int exceededCount = 0;
        double highestUsage = 0;
        string highestCategory = "-";

        foreach (var b in budgets)
        {
            int by = b.Year == 0 ? year : b.Year;
            int bm = b.Month == 0 ? month : b.Month;
            var spent = transactions.Where(t => !t.IsIncome && t.Category == b.Category && t.Date.Year == by && t.Date.Month == bm).Sum(t => t.Amount);
            var remaining = b.MonthlyLimit - spent;
            var usagePct = b.MonthlyLimit > 0 ? (double)(spent / b.MonthlyLimit) * 100.0 : 0.0;

            if (usagePct > highestUsage)
            {
                highestUsage = usagePct;
                highestCategory = b.Category;
            }
            if (usagePct >= 100) exceededCount++;

            // small card
            var card = new Border { BackgroundColor = Colors.White, StrokeThickness = 0, Padding = 10 };
            var vs = new VerticalStackLayout { Spacing = 4 };
            vs.Add(new Label { Text = b.Category, FontAttributes = FontAttributes.Bold });
            vs.Add(new Label { Text = $"₺{b.MonthlyLimit:N2} bütçe · ₺{spent:N2} harcandı · ₺{remaining:N2} kaldı" });
            vs.Add(new Label { Text = $"%{usagePct:F0} kullanım" });
            var progress = new ProgressBar { Progress = (float)Math.Min(usagePct / 100.0, 2.0), HeightRequest = 8, BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#EEEEEE") };
            vs.Add(progress);

            card.Content = vs;
            BudgetsListLayout.Children.Add(card);

            // add insight messages for over/borderline budgets
            if (usagePct >= 100)
            {
                insights.Add($"{b.Category} bütçeni aştın.");
            }
            else if (usagePct >= 80)
            {
                insights.Add($"{b.Category} bütçenin %{usagePct:F0} kadarını kullandın (yaklaşılıyor).");
            }
        }

        BudgetsSummaryLabel.Text = $"Toplam bütçe: {budgets.Count} | Aşılan: {exceededCount} | En yüksek kullanım: {highestCategory} ({highestUsage:F0}%)";

        var totalIncome = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
        var balance = totalIncome - totalExpense;
        var savingsRate = totalIncome > 0 ? (totalIncome - totalExpense) / totalIncome : 0m;

        TotalIncomeLabel.Text = $"₺{totalIncome:N2}";
        TotalExpenseLabel.Text = $"₺{totalExpense:N2}";
        BalanceLabel.Text = $"₺{balance:N2}";
        SavingsRateLabel.Text = $"{savingsRate:P0}";

        // Top spending category
        var topCategory = transactions.Where(t => !t.IsIncome)
            .GroupBy(t => t.Category)
            .Select(g => new { Cat = g.Key, Sum = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Sum)
            .FirstOrDefault();

        HighCategoryLabel.Text = topCategory != null ? $"En çok harcama: {topCategory.Cat} (₺{topCategory.Sum:N2})" : "Harcama kaydı yok.";

        // This month vs last month
        now = DateTime.Now;
        var thisMonthExpenses = transactions.Where(t => !t.IsIncome && t.Date.Year == now.Year && t.Date.Month == now.Month).Sum(t => t.Amount);
        var prev = now.AddMonths(-1);
        var prevMonthExpenses = transactions.Where(t => !t.IsIncome && t.Date.Year == prev.Year && t.Date.Month == prev.Month).Sum(t => t.Amount);

        string monthlyChangeText;
        if (prevMonthExpenses == 0 && thisMonthExpenses == 0)
            monthlyChangeText = "Geçen aya göre değişim yok.";
        else if (prevMonthExpenses == 0)
            monthlyChangeText = "Geçen ay veri yok; bu ay harcama mevcut.";
        else
        {
            var change = ((thisMonthExpenses - prevMonthExpenses) / prevMonthExpenses) * 100m;
            monthlyChangeText = $"Giderler geçen aya göre {(change >= 0 ? "arttı" : "azaldı")} %{Math.Abs(change):F1} (₺{(thisMonthExpenses - prevMonthExpenses):N2})";
        }

        SummaryLabel.Text = $"Bu ay: ₺{thisMonthExpenses:N2}. {monthlyChangeText}";

        // Insights: simple rules (continue adding to existing insights list)
        if (topCategory != null)
            insights.Add($"Bu ay harcamalarının en büyük kısmı {topCategory.Cat} kategorisinde.");
        if (savingsRate > 0.25m)
            insights.Add($"Gelirlerinin %{(savingsRate * 100):F0} kadarını tasarruf etmiş durumdasın.");
        else if (savingsRate <= 0)
            insights.Add("Gelirlerine göre tasarrufun yok; harcamalarını gözden geçir.");

        if (thisMonthExpenses > prevMonthExpenses)
            insights.Add($"Bu ay giderlerin geçen aya göre %{(prevMonthExpenses == 0 ? 100 : Math.Abs(((thisMonthExpenses - prevMonthExpenses) / (prevMonthExpenses == 0 ? 1 : prevMonthExpenses) * 100))):F0} arttı.");

        InsightsLabel.Text = string.Join("\n", insights);
    }

    private async void WhereDidMyMoneyGo_Clicked(object? sender, EventArgs e)
    {
        var transactions = await App.Database.GetTransactionsAsync();
        var thisMonth = DateTime.Now;
        var groups = transactions.Where(t => !t.IsIncome && t.Date.Year == thisMonth.Year && t.Date.Month == thisMonth.Month)
            .GroupBy(t => t.Category)
            .Select(g => new { Cat = g.Key, Sum = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Sum)
            .ToList();

        if (groups.Count == 0)
        {
            QuickAnswerLabel.Text = "Bu ay için gider verisi yok.";
            return;
        }

        QuickAnswerLabel.Text = string.Join("\n", groups.Select(g => $"{g.Cat}: ₺{g.Sum:N2}"));
    }

    private async void WhatDoISpendMostOn_Clicked(object? sender, EventArgs e)
    {
        var transactions = await App.Database.GetTransactionsAsync();
        var top = transactions.Where(t => !t.IsIncome)
            .GroupBy(t => t.Category)
            .Select(g => new { Cat = g.Key, Sum = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Sum)
            .FirstOrDefault();

        QuickAnswerLabel.Text = top != null ? $"En çok harcadığın kategori: {top.Cat} (₺{top.Sum:N2})" : "Harcama kaydı yok.";
    }

    private async void HowMuchSavedThisMonth_Clicked(object? sender, EventArgs e)
    {
        var transactions = await App.Database.GetTransactionsAsync();
        var now = DateTime.Now;
        var thisMonth = transactions.Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month);
        var income = thisMonth.Where(t => t.IsIncome).Sum(t => t.Amount);
        var expense = thisMonth.Where(t => !t.IsIncome).Sum(t => t.Amount);
        var saved = income - expense;

        QuickAnswerLabel.Text = $"Bu ay tasarruf: ₺{saved:N2} ({(income > 0 ? (saved / income * 100) : 0):F0}%)";
    }

    private async void CompareToLastMonth_Clicked(object? sender, EventArgs e)
    {
        var transactions = await App.Database.GetTransactionsAsync();
        var now = DateTime.Now;
        var thisMonthExpense = transactions.Where(t => !t.IsIncome && t.Date.Year == now.Year && t.Date.Month == now.Month).Sum(t => t.Amount);
        var prev = now.AddMonths(-1);
        var prevMonthExpense = transactions.Where(t => !t.IsIncome && t.Date.Year == prev.Year && t.Date.Month == prev.Month).Sum(t => t.Amount);

        if (prevMonthExpense == 0 && thisMonthExpense == 0)
            QuickAnswerLabel.Text = "Geçen aya göre değişim yok.";
        else if (prevMonthExpense == 0)
            QuickAnswerLabel.Text = "Geçen ay veri yok; bu ay harcama mevcut.";
        else
        {
            var change = ((thisMonthExpense - prevMonthExpense) / prevMonthExpense) * 100m;
            QuickAnswerLabel.Text = $"Giderler {(change >= 0 ? "arttı" : "azaldı")} %{Math.Abs(change):F1} (₺{(thisMonthExpense - prevMonthExpense):N2})";
        }
    }
}
