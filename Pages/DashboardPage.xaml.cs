using FinanceAI.Charts;
using FinanceAI.Helpers;
using FinanceAI.Models;

namespace FinanceAI.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly GroupedBarChartDrawable _chart = new();

    public DashboardPage()
    {
        InitializeComponent();
        ChartView.Drawable = _chart;
        App.TransactionsChanged += OnTransactionsChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshDataAsync();
    }

    private void OnTransactionsChanged()
    {
        if (Handler is null)
            return;

        _ = RefreshDataAsync();
    }

    private async void RefreshButton_Clicked(object? sender, EventArgs e) => await RefreshDataAsync();

    private async void ReportsButton_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new FinancialReportsPage());
    }

    private async void AssistantButton_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new FinancialAssistantPage());
    }

    private async void GoalsButton_Clicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//goals");
    }

    private int _loadTicket;

    private async Task RefreshDataAsync()
    {
        int ticket = Interlocked.Increment(ref _loadTicket);
        try
        {
            var transactions = await App.Database.GetTransactionsAsync();
            var goals = await App.Database.GetGoalsAsync();
            if (ticket != _loadTicket)
                return;
            var now = DateTime.Now;

            decimal totalIncome = transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
            decimal totalExpense = transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);
            TotalBalanceLabel.Text = FinanceFormat.Money(totalIncome - totalExpense);

            var thisMonth = transactions.Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month).ToList();
            decimal thisMonthIncome = thisMonth.Where(t => t.IsIncome).Sum(t => t.Amount);
            decimal thisMonthExpense = thisMonth.Where(t => !t.IsIncome).Sum(t => t.Amount);
            decimal savings = thisMonthIncome - thisMonthExpense;

            ThisMonthIncomeLabel.Text = FinanceFormat.Money(thisMonthIncome);
            ThisMonthExpenseLabel.Text = FinanceFormat.Money(thisMonthExpense);
            SavingsLabel.Text = thisMonthIncome > 0
                ? $"{FinanceFormat.Money(savings)} · %{Math.Round(savings / thisMonthIncome * 100):0}"
                : FinanceFormat.Money(savings);
            SavingsLabel.TextColor = savings >= 0 ? Palette.Income : Palette.Expense;

            var previous = now.AddMonths(-1);
            decimal previousExpense = transactions
                .Where(t => !t.IsIncome && t.Date.Year == previous.Year && t.Date.Month == previous.Month)
                .Sum(t => t.Amount);

            if (previousExpense == 0 && thisMonthExpense == 0)
                MonthlyChangeLabel.Text = "Bu ay ve geçen ay harcama yok.";
            else if (previousExpense == 0)
                MonthlyChangeLabel.Text = "Geçen ay harcama yok. Bu ay " + FinanceFormat.Money(thisMonthExpense) + ".";
            else
            {
                decimal change = (thisMonthExpense - previousExpense) / previousExpense * 100m;
                string direction = change >= 0 ? "arttı" : "azaldı";
                MonthlyChangeLabel.Text =
                    $"Harcamalar %{Math.Abs(change):0} {direction}. Fark {FinanceFormat.Money(thisMonthExpense - previousExpense)}.";
            }

            FillCategories(thisMonth.Where(t => !t.IsIncome).ToList(), thisMonthExpense);
            FillTopCategories(transactions);

            var months = Enumerable.Range(0, 6)
                .Select(i => new DateTime(now.Year, now.Month, 1).AddMonths(-5 + i))
                .ToList();

            _chart.SetData(
                months.Select(m => (float)transactions.Where(t => t.IsIncome && t.Date.Year == m.Year && t.Date.Month == m.Month).Sum(t => t.Amount)),
                months.Select(m => (float)transactions.Where(t => !t.IsIncome && t.Date.Year == m.Year && t.Date.Month == m.Month).Sum(t => t.Amount)),
                months.Select(m => m.ToString("MM.yy", FinanceFormat.Culture)));
            ChartView.Invalidate();

            var active = goals.Where(g => g.TargetAmount > 0 && g.CurrentAmount < g.TargetAmount).ToList();
            var nearest = active.OrderBy(g => g.TargetDate).FirstOrDefault();
            decimal progress = goals.Count == 0
                ? 0
                : goals.Average(g => g.TargetAmount > 0 ? Math.Min(g.CurrentAmount / g.TargetAmount, 1) : 0);

            GoalsSummaryLabel.Text = goals.Count == 0
                ? "Henüz hedef yok."
                : $"Devam eden {active.Count} hedef. En yakın: {(nearest == null ? "yok" : nearest.Name)}. Genel ilerleme %{progress * 100:0}.";
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Veri hatası", "Panel güncellenemedi. " + ex.Message, "Tamam");
        }
    }

    private void FillCategories(List<Transaction> expenses, decimal total)
    {
        CategoryDistributionLayout.Children.Clear();
        var groups = expenses
            .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "Diğer" : t.Category)
            .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Amount)
            .ToList();

        if (groups.Count == 0)
        {
            CategoryDistributionLayout.Children.Add(new Label
            {
                Text = "Bu ay gider yok.",
                TextColor = Palette.Muted
            });
            return;
        }

        foreach (var group in groups)
        {
            double percent = total > 0 ? (double)(group.Amount / total) : 0;
            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                }
            };
            row.Add(new Label { Text = group.Category, TextColor = Palette.Ink }, 0, 0);
            row.Add(new Label
            {
                Text = $"{FinanceFormat.Money(group.Amount)} · %{percent * 100:0}",
                TextColor = Palette.Muted
            }, 1, 0);

            var bar = new ProgressBar
            {
                Progress = Math.Clamp(percent, 0, 1),
                ProgressColor = Palette.Expense,
                BackgroundColor = Palette.Line
            };

            CategoryDistributionLayout.Children.Add(new VerticalStackLayout
            {
                Spacing = 4,
                Children = { row, bar }
            });
        }
    }

    private void FillTopCategories(List<Transaction> transactions)
    {
        TopCategoriesLayout.Children.Clear();
        var top = transactions
            .Where(t => !t.IsIncome)
            .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "Diğer" : t.Category)
            .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Amount)
            .Take(3)
            .ToList();

        if (top.Count == 0)
        {
            TopCategoriesLayout.Children.Add(new Label { Text = "Henüz gider yok.", TextColor = Palette.Muted });
            return;
        }

        int rank = 1;
        foreach (var item in top)
        {
            TopCategoriesLayout.Children.Add(new Label
            {
                Text = $"{rank}. {item.Category} · {FinanceFormat.Money(item.Amount)}",
                TextColor = Palette.Ink,
                FontSize = 15
            });
            rank++;
        }
    }
}
