using Microsoft.Maui.Controls.Shapes;

namespace FinanceAI.Helpers;

public readonly record struct TableColumn(string Title, GridLength Width, TextAlignment Align = TextAlignment.Start);

public static class DataTable
{
    public static View Create(
        IReadOnlyList<TableColumn> columns,
        IEnumerable<IReadOnlyList<View>> rows,
        string emptyText)
    {
        var materialized = rows.ToList();
        var host = new VerticalStackLayout { Spacing = 0 };
        host.Add(BuildHeader(columns));

        if (materialized.Count == 0)
        {
            host.Add(new VerticalStackLayout
            {
                Padding = new Thickness(20, 28),
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = emptyText,
                        TextColor = Palette.Muted,
                        FontSize = 14,
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            });
        }
        else
        {
            for (int i = 0; i < materialized.Count; i++)
            {
                host.Add(new BoxView { HeightRequest = 1, Color = Palette.Line });
                host.Add(BuildRow(columns, materialized[i], i % 2 == 1));
            }
        }

        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Palette.Line,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) },
            Padding = 0,
            Content = host
        };
    }

    public static View Cell(string text, Color? color = null, bool bold = false, TextAlignment align = TextAlignment.Start)
    {
        return new Label
        {
            Text = text,
            TextColor = color ?? Palette.Ink,
            FontSize = 13,
            FontAttributes = bold ? FontAttributes.Bold : FontAttributes.None,
            LineBreakMode = LineBreakMode.TailTruncation,
            MaxLines = 1,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = align
        };
    }

    public static View Actions(Func<Task> onEdit, Func<Task> onDelete)
    {
        var row = new HorizontalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        };
        row.Add(ActionChip("Düzenle", Palette.Accent, Color.FromArgb("#E5F4EF"), onEdit));
        row.Add(ActionChip("Sil", Palette.Expense, Color.FromArgb("#FDECEC"), onDelete));
        return row;
    }

    private static View ActionChip(string text, Color color, Color background, Func<Task> action)
    {
        var chip = new Border
        {
            BackgroundColor = background,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Padding = new Thickness(10, 6),
            Content = new Label
            {
                Text = text,
                TextColor = color,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center
            }
        };
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await action();
        chip.GestureRecognizers.Add(tap);
        return chip;
    }

    private static Grid BuildHeader(IReadOnlyList<TableColumn> columns)
    {
        var grid = MakeGrid(columns);
        grid.BackgroundColor = Palette.AccentSoft;
        grid.Padding = new Thickness(14, 10);

        for (int i = 0; i < columns.Count; i++)
        {
            grid.Add(new Label
            {
                Text = columns[i].Title,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Palette.Accent,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = columns[i].Align,
                LineBreakMode = LineBreakMode.TailTruncation
            }, i, 0);
        }

        return grid;
    }

    private static Grid BuildRow(IReadOnlyList<TableColumn> columns, IReadOnlyList<View> cells, bool alt)
    {
        var grid = MakeGrid(columns);
        grid.Padding = new Thickness(14, 13);
        grid.BackgroundColor = alt ? Color.FromArgb("#F7FBFA") : Colors.White;

        int count = Math.Min(columns.Count, cells.Count);
        for (int i = 0; i < count; i++)
        {
            cells[i].VerticalOptions = LayoutOptions.Center;
            grid.Add(cells[i], i, 0);
        }

        return grid;
    }

    private static Grid MakeGrid(IReadOnlyList<TableColumn> columns)
    {
        var grid = new Grid { ColumnSpacing = 10 };
        foreach (var column in columns)
            grid.ColumnDefinitions.Add(new ColumnDefinition(column.Width));
        return grid;
    }
}
