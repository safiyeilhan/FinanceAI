using FinanceAI.Models;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace FinanceAI.Pages;

public partial class FinancialGoalsPage : ContentPage
{
    public FinancialGoalsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadGoalsAsync();
    }

    private async Task LoadGoalsAsync()
    {
        GoalsLayout.Children.Clear();
        var goals = await App.Database.GetGoalsAsync();

        foreach (var g in goals)
        {
            GoalsLayout.Children.Add(CreateGoalCard(g));
        }
    }

    private Border CreateGoalCard(FinancialGoal g)
    {
        double progress = g.TargetAmount > 0 ? (double)(g.CurrentAmount / g.TargetAmount) : 0.0;
        if (progress > 1) progress = 1;

        var nameLabel = new Label { Text = g.Name, FontAttributes = FontAttributes.Bold, FontSize = 16 };
        var amountsLabel = new Label { Text = $"₺{g.CurrentAmount:N2} / ₺{g.TargetAmount:N2}", FontSize = 14 };
        var dateLabel = new Label { Text = $"Hedef Tarihi: {g.TargetDate:dd.MM.yyyy}", FontSize = 12, TextColor = Color.FromArgb("#666") };

        var progressBar = new ProgressBar { Progress = progress, HeightRequest = 8 };

        var addButton = new Button { Text = "Para Ekle", BackgroundColor = Color.FromArgb("#1976D2"), TextColor = Colors.White };
        addButton.Clicked += async (s, e) =>
        {
            string? input = await DisplayPromptAsync("Para Ekle", "Eklenecek miktarı girin:", keyboard: Keyboard.Numeric);
            if (decimal.TryParse(input, out decimal amount) && amount > 0)
            {
                g.CurrentAmount += amount;
                await App.Database.UpdateGoalAsync(g);
                await LoadGoalsAsync();
            }
        };

        var deleteButton = new Button { Text = "Sil", BackgroundColor = Colors.Transparent, TextColor = Color.FromArgb("#C62828") };
        deleteButton.Clicked += async (s, e) =>
        {
            bool ok = await DisplayAlertAsync("Onay", $"'{g.Name}' hedefini silmek istediğinize emin misiniz?", "Evet", "Hayır");
            if (ok)
            {
                await App.Database.DeleteGoalAsync(g);
                await LoadGoalsAsync();
            }
        };

        var buttons = new HorizontalStackLayout { Spacing = 8, Children = { addButton, deleteButton } };

        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children = { nameLabel, amountsLabel, progressBar, dateLabel, buttons }
            }
        };

        // Visual completed state
        if (g.CurrentAmount >= g.TargetAmount)
        {
            var doneLabel = new Label { Text = "Tamamlandı", TextColor = Color.FromArgb("#2E7D32"), FontAttributes = FontAttributes.Bold };
            (card.Content as VerticalStackLayout)!.Children.Insert(1, doneLabel);
        }

        return card;
    }

    private async void AddGoalButton_Clicked(object? sender, EventArgs e)
    {
        string? name = await DisplayPromptAsync("Yeni Hedef", "Hedef adı:");
        if (string.IsNullOrWhiteSpace(name)) return;

        string? targetStr = await DisplayPromptAsync("Hedef Tutarı", "Hedef tutarı (örn. 30000):", keyboard: Keyboard.Numeric);
        if (!decimal.TryParse(targetStr, out decimal target) || target <= 0) return;

        string? desc = await DisplayPromptAsync("Açıklama (isteğe bağlı)", "Açıklama (isteğe bağlı):");

        string? dateStr = await DisplayPromptAsync("Hedef Tarihi", "Hedef tarihi (gg.aa.yyyy):");
        DateTime targetDate = DateTime.Now.AddMonths(1);
        if (!string.IsNullOrWhiteSpace(dateStr) && DateTime.TryParse(dateStr, out var parsed))
            targetDate = parsed;

        var goal = new FinancialGoal
        {
            Name = name,
            TargetAmount = target,
            CurrentAmount = 0,
            Description = desc,
            TargetDate = targetDate,
            CreatedDate = DateTime.Now
        };

        await App.Database.AddGoalAsync(goal);
        await LoadGoalsAsync();
    }
}
