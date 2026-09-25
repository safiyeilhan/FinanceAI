using FinanceAI.Models;
using Microsoft.Maui.Graphics;

namespace FinanceAI.Pages;

public partial class FinancialReportsPage : ContentPage
{
    public FinancialReportsPage()
    {
        InitializeComponent();

        PeriodPicker.SelectedIndexChanged += PeriodPicker_SelectedIndexChanged;
        CategoryPieView.Drawable = new PieChartDrawable();
        IncomeExpenseView.Drawable = new BarLineChartDrawable();
        TrendView.Drawable = new TrendChartDrawable();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (PeriodPicker.SelectedIndex == -1) PeriodPicker.SelectedIndex = 0;
        await RefreshAsync();
    }

    private async void PeriodPicker_SelectedIndexChanged(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private (DateTime from, DateTime to) GetRangeForSelection()
    {
        var now = DateTime.Now;
        switch (PeriodPicker.SelectedIndex)
        {
            case 0: // Bu ay
                return (new DateTime(now.Year, now.Month, 1), now);
            case 1: // Geçen ay
                var prev = now.AddMonths(-1);
                return (new DateTime(prev.Year, prev.Month, 1), new DateTime(prev.Year, prev.Month, DateTime.DaysInMonth(prev.Year, prev.Month)));
            case 2: // Son 3 ay
                var from3 = new DateTime(now.Year, now.Month, 1).AddMonths(-2);
                return (from3, now);
            case 3: // Son 6 ay
                var from6 = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
                return (from6, now);
            case 4: // Bu yıl
                return (new DateTime(now.Year, 1, 1), now);
            default:
                return (new DateTime(now.Year, now.Month, 1), now);
        }
    }

    private async Task RefreshAsync()
    {
        var (from, to) = GetRangeForSelection();
        var transactions = await App.Database.GetTransactionsAsync();
        transactions ??= new();

        var selected = transactions.Where(t => t.Date >= from && t.Date <= to).ToList();

        decimal totalIncome = selected.Where(t => t.IsIncome).Sum(t => t.Amount);
        decimal totalExpense = selected.Where(t => !t.IsIncome).Sum(t => t.Amount);
        decimal net = totalIncome - totalExpense;
        decimal savingsRate = totalIncome > 0 ? (totalIncome - totalExpense) / totalIncome : 0m;

        TotalIncomeLabel.Text = $"Toplam gelir: ₺{totalIncome:N2}";
        TotalExpenseLabel.Text = $"Toplam gider: ₺{totalExpense:N2}";
        NetChangeLabel.Text = $"Net değişim: ₺{net:N2}";
        SavingsRateLabel.Text = $"Tasarruf oranı: {savingsRate:P0}";

        // category analysis
        var categories = new[] { "Alışveriş", "Yemek", "Ulaşım", "Fatura", "Eğitim", "Sağlık", "Abonelik", "Diğer" };
        var catGroups = selected.Where(t => !t.IsIncome)
            .GroupBy(t => categories.Contains(t.Category) ? t.Category : "Diğer")
            .Select(g => new { Category = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderByDescending(x => x.Amount)
            .ToList();

        CategoryListLayout.Children.Clear();
        foreach (var c in catGroups)
        {
            double pct = totalExpense > 0 ? (double)(c.Amount / totalExpense) * 100 : 0;
            var label = new Label { Text = $"{c.Category}: ₺{c.Amount:N2} ({pct:F0}%)", FontSize = 14 };
            CategoryListLayout.Children.Add(label);
        }

        // Pie chart data
        float[] pieValues = catGroups.Select(x => (float)x.Amount).ToArray();
        string[] pieLabels = catGroups.Select(x => x.Category).ToArray();
        (CategoryPieView.Drawable as PieChartDrawable)!.SetData(pieValues, pieLabels);
        CategoryPieView.Invalidate();

        // Income/expense time series for selection: use months within range
        List<string> months = new();
        List<float> incomes = new();
        List<float> expenses = new();
        // no-op: adjust context for analyzer fixes
        var cursor = new DateTime(from.Year, from.Month, 1);
        while (cursor <= to)
        {
            months.Add(cursor.ToString("MM.yyyy"));
            var mInc = selected.Where(t => t.IsIncome && t.Date.Year == cursor.Year && t.Date.Month == cursor.Month).Sum(t => t.Amount);
            var mExp = selected.Where(t => !t.IsIncome && t.Date.Year == cursor.Year && t.Date.Month == cursor.Month).Sum(t => t.Amount);
            incomes.Add((float)mInc);
            expenses.Add((float)mExp);
            cursor = cursor.AddMonths(1);
        }

        (IncomeExpenseView.Drawable as BarLineChartDrawable)!.SetData(incomes.ToArray()!, expenses.ToArray()!, months.ToArray()!);
        IncomeExpenseView.Invalidate();

        // Trend: last 6 months
        var now = DateTime.Now;
        var last6 = Enumerable.Range(0, 6).Select(i => new DateTime(now.Year, now.Month, 1).AddMonths(-i)).Reverse().ToList();
        string[] trendLabels = last6.Select(d => d.ToString("MM.yyyy")).ToArray();
        float[] trendIncomes = last6.Select(d => (float)transactions.Where(t => t.IsIncome && t.Date.Year == d.Year && t.Date.Month == d.Month).Sum(t => t.Amount)).ToArray();
        float[] trendExpenses = last6.Select(d => (float)transactions.Where(t => !t.IsIncome && t.Date.Year == d.Year && t.Date.Month == d.Month).Sum(t => t.Amount)).ToArray();
        (TrendView.Drawable as TrendChartDrawable)!.SetData(trendIncomes, trendExpenses, trendLabels);
        TrendView.Invalidate();

        // Top 3 categories overall in selection
        TopCategoriesLayoutReports.Children.Clear();
        var top3 = catGroups.Take(3).ToList();
        foreach (var t in top3)
        {
            double pct = totalExpense > 0 ? (double)(t.Amount / totalExpense) * 100 : 0;
            TopCategoriesLayoutReports.Children.Add(new Label { Text = $"{t.Category}: ₺{t.Amount:N2} ({pct:F0}%)", FontSize = 14 });
        }

        // insights via simple rules
        List<string> insights = new();
        if (catGroups.Count > 0) insights.Add($"Bu dönemde en yüksek harcama kategorin {catGroups[0].Category}.");
        if (selected.Count > 0)
        {
            var prevRangeEnd = from.AddMonths(-1);
            var prevRangeStart = new DateTime(prevRangeEnd.Year, prevRangeEnd.Month, 1);
            var prevSel = transactions.Where(t => t.Date >= prevRangeStart && t.Date <= prevRangeEnd).ToList();
            var prevExp = prevSel.Where(t => !t.IsIncome).Sum(t => t.Amount);
            if (prevExp > 0)
            {
                var change = ((totalExpense - prevExp) / prevExp) * 100m;
                insights.Add($"Giderlerin önceki döneme göre %{Math.Abs(change):F1} {(change >= 0 ? "arttı" : "azaldı" )}.");
            }
        }
        InsightsLabelReports.Text = string.Join("\n", insights);
    }

    // Simple pie drawable
    class PieChartDrawable : IDrawable
    {
        private float[] _values = Array.Empty<float>();
        private string[] _labels = Array.Empty<string>();

        public void SetData(float[] values, string[] labels)
        {
            _values = values ?? Array.Empty<float>();
            _labels = labels ?? Array.Empty<string>();
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Colors.Transparent;
            canvas.FillRectangle(dirtyRect);
            float cx = dirtyRect.Center.X;
            float cy = dirtyRect.Center.Y - 10;
            float r = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - 10;
            float total = Math.Max(1, _values.Sum());
            float start = -90;
            var palette = new[] {
                Microsoft.Maui.Graphics.Color.FromArgb("#1976D2"),
                Microsoft.Maui.Graphics.Color.FromArgb("#66BB6A"),
                Microsoft.Maui.Graphics.Color.FromArgb("#F06257"),
                Microsoft.Maui.Graphics.Color.FromArgb("#FFB300"),
                Microsoft.Maui.Graphics.Color.FromArgb("#7E57C2"),
                Microsoft.Maui.Graphics.Color.FromArgb("#29B6F6"),
                Microsoft.Maui.Graphics.Color.FromArgb("#8D6E63"),
                Microsoft.Maui.Graphics.Color.FromArgb("#BDBDBD")
            };
            for (int i = 0; i < _values.Length; i++)
            {
                float sweep = _values[i] / total * 360f;
                canvas.FillColor = palette[i % palette.Length];
                canvas.FillArc(cx - r, cy - r, r * 2, r * 2, start, sweep, true);
                start += sweep;
            }
        }
    }

    // Simple bar/line chart drawable for income vs expense
    class BarLineChartDrawable : IDrawable
    {
        private float[] _incomes = Array.Empty<float>();
        private float[] _expenses = Array.Empty<float>();
        private string[] _labels = Array.Empty<string>();

        public void SetData(float[] incomes, float[] expenses, string[] labels)
        {
            _incomes = incomes ?? Array.Empty<float>();
            _expenses = expenses ?? Array.Empty<float>();
            _labels = labels ?? Array.Empty<string>();
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);
            int n = Math.Max(1, Math.Max(_incomes.Length, _expenses.Length));
            float pad = 24;
            float w = dirtyRect.Width - pad * 2;
            float h = dirtyRect.Height - pad * 2 - 20;
            float max = 1;
            if (_incomes.Length > 0) max = Math.Max(max, _incomes.Max());
            if (_expenses.Length > 0) max = Math.Max(max, _expenses.Max());
            float bw = w / n * 0.35f;
            for (int i = 0; i < n; i++)
            {
                float x = pad + (w / n) * i + (w / n - bw) / 2;
                float exp = i < _expenses.Length ? _expenses[i] : 0f;
                float inc = i < _incomes.Length ? _incomes[i] : 0f;
                float eh = max > 0 ? h * (exp / max) : 0;
                float ih = max > 0 ? h * (inc / max) : 0;
                canvas.FillColor = Microsoft.Maui.Graphics.Color.FromArgb("#F06257");
                canvas.FillRoundedRectangle(x, pad + (h - eh), bw, eh, 4);
                canvas.FillColor = Microsoft.Maui.Graphics.Color.FromArgb("#66BB6A");
                canvas.FillRoundedRectangle(x + bw + 4, pad + (h - ih), bw, ih, 4);
                // labels
                canvas.FontSize = 10;
                var label = i < _labels.Length ? _labels[i] : "";
                canvas.FillColor = Microsoft.Maui.Graphics.Color.FromArgb("#666666");
                canvas.DrawString(label, x, pad + h + 4, HorizontalAlignment.Left);
            }
        }
    }

    class TrendChartDrawable : IDrawable
    {
        private float[] _incomes = Array.Empty<float>();
        private float[] _expenses = Array.Empty<float>();
        private string[] _labels = Array.Empty<string>();

        public void SetData(float[] incomes, float[] expenses, string[] labels)
        {
            _incomes = incomes ?? Array.Empty<float>();
            _expenses = expenses ?? Array.Empty<float>();
            _labels = labels ?? Array.Empty<string>();
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);
            int n = Math.Max(1, Math.Max(_incomes.Length, _expenses.Length));
            float pad = 24;
            float w = dirtyRect.Width - pad * 2;
            float h = dirtyRect.Height - pad * 2 - 20;
            float max = 1;
            if (_incomes.Length > 0) max = Math.Max(max, _incomes.Max());
            if (_expenses.Length > 0) max = Math.Max(max, _expenses.Max());
            // draw lines
            canvas.StrokeColor = Microsoft.Maui.Graphics.Color.FromArgb("#66BB6A");
            canvas.StrokeSize = 2;
            for (int i = 0; i < _incomes.Length; i++) { }
            // draw incomes polyline
            if (_incomes.Length > 0)
            {
                var pts = new PointF[_incomes.Length];
                for (int i = 0; i < _incomes.Length; i++) pts[i] = new PointF(pad + i * (w / Math.Max(1, _incomes.Length - 1)), pad + h - (max > 0 ? h * (_incomes[i] / max) : 0));
                for (int i = 1; i < pts.Length; i++) canvas.DrawLine(pts[i - 1].X, pts[i - 1].Y, pts[i].X, pts[i].Y);
            }
            // draw expenses polyline
            canvas.StrokeColor = Microsoft.Maui.Graphics.Color.FromArgb("#F06257");
            if (_expenses.Length > 0)
            {
                var pts = new PointF[_expenses.Length];
                for (int i = 0; i < _expenses.Length; i++) pts[i] = new PointF(pad + i * (w / Math.Max(1, _expenses.Length - 1)), pad + h - (max > 0 ? h * (_expenses[i] / max) : 0));
                for (int i = 1; i < pts.Length; i++) canvas.DrawLine(pts[i - 1].X, pts[i - 1].Y, pts[i].X, pts[i].Y);
            }
        }
    }
}
