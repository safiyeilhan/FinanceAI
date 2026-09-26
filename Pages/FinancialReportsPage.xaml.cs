using FinanceAI.Charts;
using FinanceAI.Helpers;
using FinanceAI.Models;

namespace FinanceAI.Pages;

public partial class FinancialReportsPage : ContentPage
{
    private readonly PieChartDrawable _pie = new();
    private readonly GroupedBarChartDrawable _bars = new();

    public FinancialReportsPage()
    {
        InitializeComponent();
        PeriodPicker.SelectedIndexChanged += PeriodPicker_SelectedIndexChanged;
        CategoryPieView.Drawable = _pie;
        IncomeExpenseView.Drawable = _bars;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (PeriodPicker.SelectedIndex < 0)
            PeriodPicker.SelectedIndex = 0;
        else
            await RefreshAsync();
    }

    private async void PeriodPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private static (DateTime from, DateTime toExclusive) GetRange(int index, DateTime now)
    {
        var monthStart = new DateTime(now.Year, now.Month, 1);
        return index switch
        {
            0 => (monthStart, monthStart.AddMonths(1)),
            1 => (monthStart.AddMonths(-1), monthStart),
            2 => (monthStart.AddMonths(-2), monthStart.AddMonths(1)),
            3 => (monthStart.AddMonths(-5), monthStart.AddMonths(1)),
            4 => (new DateTime(now.Year, 1, 1), new DateTime(now.Year + 1, 1, 1)),
            _ => (monthStart, monthStart.AddMonths(1))
        };
    }

    private async Task RefreshAsync()
    {
        if (PeriodPicker.SelectedIndex < 0)
            return;

        try
        {
            var now = DateTime.Now;
            var (from, toExclusive) = GetRange(PeriodPicker.SelectedIndex, now);
            var transactions = await App.Database.GetTransactionsAsync();
            var selected = transactions.Where(t => t.Date >= from && t.Date < toExclusive).ToList();

            decimal totalIncome = selected.Where(t => t.IsIncome).Sum(t => t.Amount);
            decimal totalExpense = selected.Where(t => !t.IsIncome).Sum(t => t.Amount);
            decimal net = totalIncome - totalExpense;
            decimal savingsRate = totalIncome > 0 ? net / totalIncome : 0;

            TotalIncomeLabel.Text = "Gelir: " + FinanceFormat.Money(totalIncome);
            TotalExpenseLabel.Text = "Gider: " + FinanceFormat.Money(totalExpense);
            NetChangeLabel.Text = "Net: " + FinanceFormat.Money(net);
            NetChangeLabel.TextColor = net >= 0 ? Palette.Income : Palette.Expense;
            SavingsRateLabel.Text = $"Tasarruf oranı: %{Math.Round(savingsRate * 100):0}";

            var groups = selected
                .Where(t => !t.IsIncome)
                .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "Diğer" : t.Category)
                .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Amount)
                .ToList();

            CategoryListLayout.Children.Clear();
            TopCategoriesLayout.Children.Clear();

            if (groups.Count == 0)
            {
                CategoryListLayout.Children.Add(new Label { Text = "Bu dönemde gider yok.", TextColor = Palette.Muted });
                TopCategoriesLayout.Children.Add(new Label { Text = "Bu dönemde gider yok.", TextColor = Palette.Muted });
            }
            else
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    var group = groups[i];
                    double percent = totalExpense > 0 ? (double)(group.Amount / totalExpense) : 0;
                    var row = new Grid
                    {
                        ColumnSpacing = 8,
                        ColumnDefinitions =
                        {
                            new ColumnDefinition(14),
                            new ColumnDefinition(GridLength.Star)
                        }
                    };
                    row.Add(new BoxView
                    {
                        Color = ChartPalette.Colors[i % ChartPalette.Colors.Length],
                        WidthRequest = 10,
                        HeightRequest = 10,
                        CornerRadius = 5,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
                    }, 0, 0);
                    row.Add(new Label
                    {
                        Text = $"{group.Category}: {FinanceFormat.Money(group.Amount)} · %{percent * 100:0}",
                        TextColor = Palette.Ink,
                        VerticalOptions = LayoutOptions.Center
                    }, 1, 0);
                    CategoryListLayout.Children.Add(row);
                }

                int rank = 1;
                foreach (var group in groups.Take(3))
                {
                    double percent = totalExpense > 0 ? (double)(group.Amount / totalExpense) : 0;
                    TopCategoriesLayout.Children.Add(new Label
                    {
                        Text = $"{rank}. {group.Category} · {FinanceFormat.Money(group.Amount)} · %{percent * 100:0}",
                        TextColor = Palette.Ink
                    });
                    rank++;
                }
            }

            _pie.SetData(groups.Select(x => (float)x.Amount));
            CategoryPieView.Invalidate();

            var months = new List<DateTime>();
            for (var cursor = new DateTime(from.Year, from.Month, 1); cursor < toExclusive; cursor = cursor.AddMonths(1))
                months.Add(cursor);

            _bars.SetData(
                months.Select(m => (float)selected.Where(t => t.IsIncome && t.Date.Year == m.Year && t.Date.Month == m.Month).Sum(t => t.Amount)),
                months.Select(m => (float)selected.Where(t => !t.IsIncome && t.Date.Year == m.Year && t.Date.Month == m.Month).Sum(t => t.Amount)),
                months.Select(m => m.ToString("MM.yy", FinanceFormat.Culture)));
            IncomeExpenseView.Invalidate();

            var insights = new List<string>();
            if (groups.Count > 0)
            {
                double share = totalExpense > 0 ? (double)(groups[0].Amount / totalExpense) * 100 : 0;
                insights.Add($"En büyük gider kalemi {groups[0].Category} (%{share:0}).");
            }

            var length = toExclusive - from;
            var previous = transactions.Where(t => t.Date >= from - length && t.Date < from).ToList();
            decimal previousExpense = previous.Where(t => !t.IsIncome).Sum(t => t.Amount);
            if (previousExpense > 0)
            {
                decimal change = (totalExpense - previousExpense) / previousExpense * 100m;
                insights.Add($"Giderler bir önceki eşit döneme göre %{Math.Abs(change):0} {(change >= 0 ? "arttı" : "azaldı")}.");
            }
            else if (totalExpense > 0)
            {
                insights.Add("Bir önceki dönemde gider yok, bu dönem harcama var.");
            }

            if (totalIncome > 0 && savingsRate < 0)
                insights.Add("Bu dönemde giderler gelirleri aştı.");
            else if (savingsRate >= 0.2m)
                insights.Add("Gelirin en az %20'si elde kaldı.");

            InsightsLabel.Text = insights.Count == 0
                ? "Bu dönem için karşılaştırılacak yeterli kayıt yok."
                : string.Join(Environment.NewLine, insights);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Veri hatası", "Rapor hazırlanamadı. " + ex.Message, "Tamam");
        }
    }
}
