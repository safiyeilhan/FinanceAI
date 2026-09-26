namespace FinanceAI.Charts;

public static class ChartPalette
{
    public static readonly Color[] Colors =
    [
        Color.FromArgb("#0E7C66"),
        Color.FromArgb("#D14343"),
        Color.FromArgb("#C4A35A"),
        Color.FromArgb("#3D6B8C"),
        Color.FromArgb("#7C6A9A"),
        Color.FromArgb("#D4845A"),
        Color.FromArgb("#5E8F7B"),
        Color.FromArgb("#8A8175")
    ];
}

public class GroupedBarChartDrawable : IDrawable
{
    private float[] _incomes = [];
    private float[] _expenses = [];
    private string[] _labels = [];

    public void SetData(IEnumerable<float> incomes, IEnumerable<float> expenses, IEnumerable<string> labels)
    {
        _incomes = incomes.ToArray();
        _expenses = expenses.ToArray();
        _labels = labels.ToArray();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Microsoft.Maui.Graphics.Colors.White;
        canvas.FillRectangle(dirtyRect);

        int points = Math.Max(_incomes.Length, _expenses.Length);
        if (points == 0)
        {
            DrawEmpty(canvas, dirtyRect);
            return;
        }

        float paddingLeft = 8;
        float paddingRight = 8;
        float paddingTop = 12;
        float paddingBottom = 28;
        float width = dirtyRect.Width - paddingLeft - paddingRight;
        float height = dirtyRect.Height - paddingTop - paddingBottom;
        if (width <= 0 || height <= 0)
            return;

        float max = 1;
        if (_incomes.Length > 0)
            max = Math.Max(max, _incomes.Max());
        if (_expenses.Length > 0)
            max = Math.Max(max, _expenses.Max());

        canvas.StrokeColor = Color.FromArgb("#E3EBE7");
        canvas.StrokeSize = 1;
        canvas.DrawLine(paddingLeft, paddingTop + height, paddingLeft + width, paddingTop + height);

        float slot = width / points;
        for (int i = 0; i < points; i++)
        {
            float gap = Math.Min(6, slot * 0.12f);
            float barWidth = Math.Max(4, (slot - gap * 3) / 2f);
            float x = paddingLeft + slot * i + gap;

            float expense = i < _expenses.Length ? _expenses[i] : 0;
            float income = i < _incomes.Length ? _incomes[i] : 0;
            float expenseHeight = height * (expense / max);
            float incomeHeight = height * (income / max);

            canvas.FillColor = Color.FromArgb("#E07A73");
            if (expenseHeight > 0)
                canvas.FillRoundedRectangle(x, paddingTop + height - expenseHeight, barWidth, expenseHeight, 4);

            canvas.FillColor = Color.FromArgb("#1F9D80");
            if (incomeHeight > 0)
                canvas.FillRoundedRectangle(x + barWidth + gap, paddingTop + height - incomeHeight, barWidth, incomeHeight, 4);

            canvas.FontColor = Color.FromArgb("#5E726A");
            canvas.FontSize = 10;
            var label = i < _labels.Length ? _labels[i] : "";
            canvas.DrawString(label, x - gap, paddingTop + height + 4, slot, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }

    private static void DrawEmpty(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FontColor = Color.FromArgb("#5E726A");
        canvas.FontSize = 14;
        canvas.DrawString("Bu dönem için veri yok", dirtyRect, HorizontalAlignment.Center, VerticalAlignment.Center);
    }
}

public class PieChartDrawable : IDrawable
{
    private float[] _values = [];

    public void SetData(IEnumerable<float> values)
    {
        _values = values.ToArray();
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Microsoft.Maui.Graphics.Colors.Transparent;
        canvas.FillRectangle(dirtyRect);

        float total = _values.Sum();
        float size = Math.Min(dirtyRect.Width, dirtyRect.Height) - 8;
        if (size <= 0)
            return;

        float x = dirtyRect.Center.X - size / 2;
        float y = dirtyRect.Center.Y - size / 2;

        if (total <= 0 || _values.Length == 0)
        {
            canvas.FillColor = Color.FromArgb("#E3EBE7");
            canvas.FillCircle(dirtyRect.Center.X, dirtyRect.Center.Y, size / 2);
            return;
        }

        float start = -90;
        for (int i = 0; i < _values.Length; i++)
        {
            float sweep = _values[i] / total * 360f;
            if (sweep <= 0)
                continue;

            canvas.FillColor = ChartPalette.Colors[i % ChartPalette.Colors.Length];
            canvas.FillArc(x, y, size, size, start, sweep, true);
            start += sweep;
        }
    }
}
