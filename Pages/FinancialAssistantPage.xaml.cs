using FinanceAI.Helpers;
using FinanceAI.Models;

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
        try
        {
            var transactions = await App.Database.GetTransactionsAsync();
            var budgets = await App.Database.GetBudgetsAsync();
            var now = DateTime.Now;

            var thisMonth = transactions
                .Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month)
                .ToList();
            decimal income = thisMonth.Where(t => t.IsIncome).Sum(t => t.Amount);
            decimal expense = thisMonth.Where(t => !t.IsIncome).Sum(t => t.Amount);
            decimal balance = income - expense;
            decimal savingsRate = income > 0 ? balance / income : 0;

            TotalIncomeLabel.Text = FinanceFormat.Money(income);
            TotalExpenseLabel.Text = FinanceFormat.Money(expense);
            BalanceLabel.Text = FinanceFormat.Money(balance);
            BalanceLabel.TextColor = balance >= 0 ? Palette.Income : Palette.Expense;
            SavingsRateLabel.Text = $"%{Math.Round(savingsRate * 100):0}";

            var previous = now.AddMonths(-1);
            decimal previousExpense = transactions
                .Where(t => !t.IsIncome && t.Date.Year == previous.Year && t.Date.Month == previous.Month)
                .Sum(t => t.Amount);

            string changeText = DescribeExpenseChange(expense, previousExpense);
            SummaryLabel.Text = $"Bu ay {FinanceFormat.Money(income)} gelir, {FinanceFormat.Money(expense)} gider. {changeText}";

            var monthGroups = thisMonth
                .Where(t => !t.IsIncome)
                .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "Diğer" : t.Category)
                .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount)
                .ToList();

            HighCategoryLabel.Text = monthGroups.Count == 0
                ? "Bu ay henüz gider yok."
                : $"Bu ay en çok: {monthGroups[0].Category} ({FinanceFormat.Money(monthGroups[0].Amount)})";

            var insights = new List<string>();
            if (monthGroups.Count > 0 && expense > 0)
            {
                decimal share = monthGroups[0].Amount / expense * 100m;
                insights.Add($"{monthGroups[0].Category}, bu ayki giderlerin %{share:0} kadarı.");
            }

            if (income == 0 && expense == 0)
                insights.Add("Bu ay henüz işlem yok.");
            else if (savingsRate >= 0.25m)
                insights.Add("Gelirin dörtte birinden fazlası duruyor.");
            else if (balance < 0)
                insights.Add("Bu ay giderler gelirleri geçti.");
            else if (income > 0)
                insights.Add("Tasarruf payı düşük. Büyük kalemleri bütçeyle sınırlamak işe yarar.");

            BudgetsListLayout.Children.Clear();
            int exceeded = 0;
            double highestUsage = 0;
            string highestCategory = "-";

            if (budgets.Count == 0)
            {
                BudgetsSummaryLabel.Text = "Tanımlı bütçe yok. Bütçe sekmesinden ekleyebilirsin.";
            }
            else
            {
                foreach (var budget in budgets)
                {
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

                    if (usage > highestUsage)
                    {
                        highestUsage = usage;
                        highestCategory = budget.Category;
                    }

                    if (usage >= 100)
                    {
                        exceeded++;
                        insights.Add($"{budget.Category} bütçesi aşıldı.");
                    }
                    else if (usage >= 80)
                    {
                        insights.Add($"{budget.Category} bütçesinin %{usage:0} kadarı kullanıldı.");
                    }

                    BudgetsListLayout.Children.Add(new Label
                    {
                        Text = $"{budget.Category}: {FinanceFormat.Money(spent)} / {FinanceFormat.Money(budget.MonthlyLimit)} · {(remaining < 0 ? FinanceFormat.Money(Math.Abs(remaining)) + " aşıldı" : FinanceFormat.Money(remaining) + " kaldı")}",
                        TextColor = usage >= 100 ? Palette.Expense : Palette.Ink
                    });
                }

                BudgetsSummaryLabel.Text = $"{budgets.Count} bütçe · {exceeded} aşıldı · en dolu {highestCategory} (%{highestUsage:0})";
            }

            InsightsLabel.Text = string.Join(Environment.NewLine, insights);
        }
        catch (Exception ex)
        {
            SummaryLabel.Text = "Veriler okunamadı.";
            InsightsLabel.Text = ex.Message;
        }
    }

    private static string DescribeExpenseChange(decimal current, decimal previous)
    {
        if (previous == 0 && current == 0)
            return "Geçen aya göre harcama değişmedi.";
        if (previous == 0)
            return "Geçen ay gider kaydı yok.";

        decimal change = (current - previous) / previous * 100m;
        string direction = change >= 0 ? "arttı" : "azaldı";
        return $"Giderler geçen aya göre %{Math.Abs(change):0} {direction}.";
    }

    private async void WhereDidMyMoneyGo_Clicked(object? sender, EventArgs e)
    {
        var transactions = await App.Database.GetTransactionsAsync();
        var now = DateTime.Now;
        var groups = transactions
            .Where(t => !t.IsIncome && t.Date.Year == now.Year && t.Date.Month == now.Month)
            .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "Diğer" : t.Category)
            .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Amount)
            .ToList();

        QuickAnswerLabel.Text = groups.Count == 0
            ? "Bu ay için gider yok."
            : string.Join(Environment.NewLine, groups.Select(g => $"{g.Category}: {FinanceFormat.Money(g.Amount)}"));
    }

    private async void WhatDoISpendMostOn_Clicked(object? sender, EventArgs e)
    {
        var transactions = await App.Database.GetTransactionsAsync();
        var now = DateTime.Now;

        string Describe(IEnumerable<Transaction> source, string title)
        {
            var top = source
                .Where(t => !t.IsIncome)
                .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "Diğer" : t.Category)
                .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount)
                .FirstOrDefault();

            return top == null
                ? $"{title}: gider yok."
                : $"{title}: {top.Category} ({FinanceFormat.Money(top.Amount)})";
        }

        var month = transactions.Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month);
        QuickAnswerLabel.Text = Describe(month, "Bu ay") + Environment.NewLine + Describe(transactions, "Tüm zamanlar");
    }

    private async void HowMuchSavedThisMonth_Clicked(object? sender, EventArgs e)
    {
        var transactions = await App.Database.GetTransactionsAsync();
        var now = DateTime.Now;
        var month = transactions.Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month).ToList();
        decimal income = month.Where(t => t.IsIncome).Sum(t => t.Amount);
        decimal expense = month.Where(t => !t.IsIncome).Sum(t => t.Amount);
        decimal saved = income - expense;
        decimal rate = income > 0 ? saved / income * 100m : 0;

        QuickAnswerLabel.Text = income == 0 && expense == 0
            ? "Bu ay işlem yok."
            : $"Bu ay {FinanceFormat.Money(saved)} kaldı. Tasarruf oranı %{rate:0}.";
    }

    private async void CompareToLastMonth_Clicked(object? sender, EventArgs e)
    {
        var transactions = await App.Database.GetTransactionsAsync();
        var now = DateTime.Now;
        var previous = now.AddMonths(-1);

        decimal thisExpense = transactions.Where(t => !t.IsIncome && t.Date.Year == now.Year && t.Date.Month == now.Month).Sum(t => t.Amount);
        decimal previousExpense = transactions.Where(t => !t.IsIncome && t.Date.Year == previous.Year && t.Date.Month == previous.Month).Sum(t => t.Amount);
        decimal thisIncome = transactions.Where(t => t.IsIncome && t.Date.Year == now.Year && t.Date.Month == now.Month).Sum(t => t.Amount);
        decimal previousIncome = transactions.Where(t => t.IsIncome && t.Date.Year == previous.Year && t.Date.Month == previous.Month).Sum(t => t.Amount);

        QuickAnswerLabel.Text =
            DescribeExpenseChange(thisExpense, previousExpense)
            + Environment.NewLine
            + $"Gelir bu ay {FinanceFormat.Money(thisIncome)}, geçen ay {FinanceFormat.Money(previousIncome)}.";
    }
}
