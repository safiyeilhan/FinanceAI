using FinanceAI.Models;
using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;

namespace FinanceAI.Pages;

#pragma warning disable IDE0028, IDE0301, IDE0305
public partial class DashboardPage : ContentPage
{
    public ObservableCollection<Transaction> Transactions { get; } = new();

    public DashboardPage()
    {
        InitializeComponent();

        // expose this page as binding context so XAML can bind to Transactions
        BindingContext = this;

        AddSampleButton.Clicked += AddSampleButton_Clicked;
        ClearSampleButton.Clicked += ClearSampleButton_Clicked;
        ChartView.Drawable = new SimpleSixMonthChartDrawable();
        ReportsButton.Clicked += ReportsButton_Clicked;
        BudgetsNavButton.Clicked += BudgetsNavButton_Clicked;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        App.TransactionsChanged += OnTransactionsChanged;
        await RefreshDataAsync();
    }

    private async void ReportsButton_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new FinancialReportsPage());
    }

    private async void BudgetsNavButton_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new BudgetsPage());
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        App.TransactionsChanged -= OnTransactionsChanged;
    }

    private void OnTransactionsChanged()
    {
        // Ensure refresh runs on UI thread because it updates UI-bound collections/controls
        MainThread.BeginInvokeOnMainThread(() => { _ = RefreshDataAsync(); });
    }

    private async Task RefreshDataAsync()
    {
        var txs = await App.Database.GetTransactionsAsync();

        // update observable collection so UI bound list updates automatically
        Transactions.Clear();
        foreach (var t in txs)
            Transactions.Add(t);

        // load goals summary
        var goals = await App.Database.GetGoalsAsync();
        var activeGoals = goals.Where(g => g.CurrentAmount < g.TargetAmount).ToList();
        int activeCount = activeGoals.Count;
        var nearest = activeGoals.OrderBy(g => g.TargetDate).FirstOrDefault();
        decimal overallProgress = 0;
        if (goals.Count > 0)
        {
            overallProgress = goals.Average(g => g.TargetAmount > 0 ? g.CurrentAmount / g.TargetAmount : 0);
        }
        GoalsSummaryLabel.Text = $"Aktif hedefler: {activeCount} | En yakın: {(nearest != null ? nearest.Name + " (" + nearest.TargetDate.ToString("dd.MM.yyyy") + ")" : "-")} | Genel ilerleme: {(overallProgress * 100):F0}%";

        var now = DateTime.Now;

        decimal totalIncome = Transactions.Where(t => t.IsIncome).Sum(t => t.Amount);
        decimal totalExpense = Transactions.Where(t => !t.IsIncome).Sum(t => t.Amount);

        decimal balance = totalIncome - totalExpense;
        TotalBalanceLabel.Text = $"₺{balance:N2}";

        var thisMonth = Transactions.Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month);
        decimal thisMonthIncome = thisMonth.Where(t => t.IsIncome).Sum(t => t.Amount);
        decimal thisMonthExpense = thisMonth.Where(t => !t.IsIncome).Sum(t => t.Amount);

        ThisMonthIncomeLabel.Text = $"₺{thisMonthIncome:N2}";
        ThisMonthExpenseLabel.Text = $"₺{thisMonthExpense:N2}";

        decimal savings = thisMonthIncome - thisMonthExpense;
        string savingsText = thisMonthIncome > 0 ? $"₺{savings:N2} ({(savings / thisMonthIncome * 100):F0}%)" : $"₺{savings:N2} (0%)";
        SavingsLabel.Text = savingsText;

        // Monthly change vs previous month
        var prevMonthDate = now.AddMonths(-1);
        var prevMonth = Transactions.Where(t => t.Date.Year == prevMonthDate.Year && t.Date.Month == prevMonthDate.Month);
        decimal prevExpense = prevMonth.Where(t => !t.IsIncome).Sum(t => t.Amount);

        if (prevExpense == 0 && thisMonthExpense == 0)
            MonthlyChangeLabel.Text = "Geçen aya göre değişim yok.";
        else if (prevExpense == 0)
            MonthlyChangeLabel.Text = "Harcamalar geçen aya göre arttı (önce veri yok).";
        else
        {
            var change = ((thisMonthExpense - prevExpense) / prevExpense) * 100;
            var arrow = change >= 0 ? "↑" : "↓";
            MonthlyChangeLabel.Text = $"Bu ay harcama {arrow} %{Math.Abs(change):F1} (₺{(thisMonthExpense - prevExpense):N2})";
        }

        // Category distribution (this month expenses)
        CategoryDistributionLayout.Children.Clear();
        var categoryGroups = thisMonth.Where(t => !t.IsIncome)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Amount)
            .ToList();

        decimal totalThisMonthExpense = categoryGroups.Sum(x => x.Amount);

        foreach (var g in categoryGroups)
        {
            double percent = totalThisMonthExpense > 0 ? (double)(g.Amount / totalThisMonthExpense) * 100 : 0;
            var row = new Grid
            {
                ColumnSpacing = 8,
                ColumnDefinitions =
                {
                    new() { Width = GridLength.Star },
                    new() { Width = GridLength.Auto }
                }
            };

            row.Add(new Label { Text = g.Category, FontSize = 13 }, 0, 0);
            row.Add(new Label { Text = $"₺{g.Amount:N2} ({percent:F0}%)", FontSize = 13 }, 1, 0);
            CategoryDistributionLayout.Children.Add(row);
        }

        // Top 3 categories overall (expenses)
        TopCategoriesLayout.Children.Clear();
        var top3 = Transactions.Where(t => !t.IsIncome)
            .GroupBy(t => t.Category)
            .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Amount)
            .Take(3);

        foreach (var t in top3)
        {
            TopCategoriesLayout.Children.Add(new Label { Text = $"{t.Category}: ₺{t.Amount:N2}", FontSize = 14 });
        }

        // Update chart data
        var last6Months = Enumerable.Range(0, 6)
            .Select(i => new
            {
                Date = new DateTime(now.Year, now.Month, 1).AddMonths(-i),
                Income = Transactions.Where(t => t.IsIncome && t.Date.Year == new DateTime(now.Year, now.Month, 1).AddMonths(-i).Year && t.Date.Month == new DateTime(now.Year, now.Month, 1).AddMonths(-i).Month).Sum(t => t.Amount),
                Expense = Transactions.Where(t => !t.IsIncome && t.Date.Year == new DateTime(now.Year, now.Month, 1).AddMonths(-i).Year && t.Date.Month == new DateTime(now.Year, now.Month, 1).AddMonths(-i).Month).Sum(t => t.Amount)
            })
            .Reverse()
            .ToList();

        (ChartView.Drawable as SimpleSixMonthChartDrawable)!.SetData(
            last6Months.Select(x => (float)x.Income),
            last6Months.Select(x => (float)x.Expense),
            last6Months.Select(x => x.Date.ToString("MM.yyyy"))
        );
        ChartView.Invalidate();
    }

    private async void AddSampleButton_Clicked(object? sender, EventArgs e)
    {
        // Add a few sample transactions without deleting existing data
        var rnd = new Random();
        var categories = new[] { "Yiyecek", "Ulaşım", "Kira", "Eğlence", "Maaş", "Yan Gelir" };

        for (int i = 0; i < 8; i++)
        {
            var isIncome = i % 6 == 0; // a couple incomes
            var tx = new Transaction
            {
                Amount = isIncome ? rnd.Next(2000, 8000) : rnd.Next(20, 800),
                IsIncome = isIncome,
                Category = isIncome ? "Maaş" : categories[rnd.Next(categories.Length)],
                Description = isIncome ? "Ek gelir" : "Günlük harcama",
                Date = DateTime.Now.AddDays(-rnd.Next(0, 180))
            };

            // mark these as sample so they can be removed later
            tx.IsSample = true;

            await App.Database.AddTransactionAsync(tx);
        }

        App.RaiseTransactionsChanged();
    }

    private async void ClearSampleButton_Clicked(object? sender, EventArgs e)
    {
        bool ok = await DisplayAlertAsync("Onay", "Sahte verileri silmek istiyor musunuz? Bu işlem geri alınamaz.", "Evet", "Hayır");
        if (!ok) return;

        var all = await App.Database.GetTransactionsAsync();
        var samples = all.Where(t => t.IsSample).ToList();

        var beforeCount = all.Count;
        var beforeTotalIncome = all.Where(t => t.IsIncome).Sum(t => t.Amount);
        var beforeTotalExpense = all.Where(t => !t.IsIncome).Sum(t => t.Amount);
        foreach (var s in samples)
        {
            try
            {
                // Use delete-by-id to avoid any issues with object identity
                var rows = await App.Database.DeleteTransactionByIdAsync(s.Id);
                // Diagnostics note: query helpers will be moved to DatabaseService in a follow-up change.

                if (rows > 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // remove any item with same Id from collection
                        var existing = Transactions.FirstOrDefault(t => t.Id == s.Id);
                        if (existing != null)
                            Transactions.Remove(existing);
                    });
                }
            }
            catch
            {
                // ignore individual delete errors; we'll refresh from DB below
            }
        }

        // Refresh summaries/labels from current collection (and re-sync with DB)
        await RefreshDataAsync();

        // Diagnostic: show counts and sums after deletion
        var after = await App.Database.GetTransactionsAsync();
        var afterCount = after.Count;
        var afterTotalIncome = after.Where(t => t.IsIncome).Sum(t => t.Amount);
        var afterTotalExpense = after.Where(t => !t.IsIncome).Sum(t => t.Amount);
        await DisplayAlert("Debug (sonra)", $"Toplam: {afterCount}\nGelir: {afterTotalIncome:N2}\nGider: {afterTotalExpense:N2}", "Tamam");
    }
}

// Simple drawable for last 6 months income/expense
class SimpleSixMonthChartDrawable : IDrawable
{
    private float[] _incomes = Array.Empty<float>();
    private float[] _expenses = Array.Empty<float>();
    private string[] _labels = Array.Empty<string>();

    public void SetData(IEnumerable<float> incomes, IEnumerable<float> expenses, IEnumerable<string> labels)
    {
        // avoid unnecessary ToArray() if caller already provided arrays
        _incomes = incomes as float[] ?? incomes.ToArray();
        _expenses = expenses as float[] ?? expenses.ToArray();
        _labels = labels as string[] ?? labels.ToArray();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Microsoft.Maui.Graphics.Colors.White;
        canvas.FillRectangle(dirtyRect);

        int points = Math.Max(1, Math.Max(_incomes.Length, _expenses.Length));

        float padding = 24;
        float w = dirtyRect.Width - padding * 2;
        float h = dirtyRect.Height - padding * 2 - 20; // leave for labels

        float maxVal = 1;
        if (_incomes.Length > 0) maxVal = Math.Max(maxVal, _incomes.Max());
        if (_expenses.Length > 0) maxVal = Math.Max(maxVal, _expenses.Max());

        // Draw expense bars (red) and income bars (green) side by side
        float barWidth = w / points * 0.35f;
        for (int i = 0; i < points; i++)
        {
            float x = padding + (w / points) * i + (w / points - barWidth) / 2;

            // expense
            float expenseVal = i < _expenses.Length ? _expenses[i] : 0f;
            float expenseH = maxVal > 0 ? (h * (expenseVal / maxVal)) : 0;
            canvas.FillColor = Microsoft.Maui.Graphics.Color.FromArgb("#F06257");
            canvas.FillRoundedRectangle(x, padding + (h - expenseH), barWidth, expenseH, 4);

            // income (slightly left)
            float incomeVal = i < _incomes.Length ? _incomes[i] : 0f;
            float incomeH = maxVal > 0 ? (h * (incomeVal / maxVal)) : 0;
            canvas.FillColor = Microsoft.Maui.Graphics.Color.FromArgb("#66BB6A");
            canvas.FillRoundedRectangle(x + barWidth + 4, padding + (h - incomeH), barWidth, incomeH, 4);

            // label
            canvas.FillColor = Microsoft.Maui.Graphics.Color.FromArgb("#666666");
            canvas.FontSize = 10;
            var label = i < _labels.Length ? _labels[i] : "";
            canvas.DrawString(label, x, padding + h + 4, HorizontalAlignment.Left);
        }
    }
}
