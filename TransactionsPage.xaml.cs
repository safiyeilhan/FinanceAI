using System;
using FinanceAI.Models;
using Microsoft.Maui.Controls.Shapes;

#pragma warning disable IDE0028 // Collection initialization can be simplified
namespace FinanceAI;

public partial class TransactionsPage : ContentPage
{
    private List<Transaction> allTransactions = new();

    private string currentFilter = "all";

    public TransactionsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to global transaction change notifications
        App.TransactionsChanged += OnTransactionsChanged;

        allTransactions = await App.Database.GetTransactionsAsync();

        ShowTransactions();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Unsubscribe to avoid memory leaks
        App.TransactionsChanged -= OnTransactionsChanged;
    }

    private async void OnTransactionsChanged()
    {
        // Refresh transaction list when notified
        allTransactions = await App.Database.GetTransactionsAsync();
        ShowTransactions();
    }

    private void ShowTransactions()
    {
        TransactionsLayout.Children.Clear();

        IEnumerable<Transaction> filteredTransactions = allTransactions;

        // Gelir / gider filtresi
        if (currentFilter == "income")
        {
            filteredTransactions = filteredTransactions
                .Where(x => x.IsIncome);
        }
        else if (currentFilter == "expense")
        {
            filteredTransactions = filteredTransactions
                .Where(x => !x.IsIncome);
        }

        // Arama filtresi
        string searchText = SearchEntry.Text?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filteredTransactions = filteredTransactions.Where(x =>
                x.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                x.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var transaction in filteredTransactions)
        {
            AddTransactionCard(transaction);
        }

        if (!filteredTransactions.Any())
        {
            var emptyLabel = new Label
            {
                Text = "Bu kriterlere uygun işlem bulunamadı.",
                FontSize = 15,
                TextColor = Color.FromArgb("#888888"),
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 30)
            };

            TransactionsLayout.Children.Add(emptyLabel);
        }
    }

    private void AddTransactionCard(Transaction transaction)
    {
        string sign = transaction.IsIncome ? "+" : "-";

        string amountColor = transaction.IsIncome
            ? "#2E7D32"
            : "#C62828";

        string title = string.IsNullOrWhiteSpace(transaction.Description)
            ? transaction.Category
            : transaction.Description;

        var titleLabel = new Label
        {
            Text = title,
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#171717")
        };

        var categoryLabel = new Label
        {
            Text = $"{transaction.Category} • {transaction.Date:dd.MM.yyyy HH:mm}",
            FontSize = 12,
            TextColor = Color.FromArgb("#888888")
        };

        var amountLabel = new Label
        {
            Text = $"{sign}₺{transaction.Amount:N2}",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb(amountColor),
            VerticalOptions = LayoutOptions.Center
        };

        var infoLayout = new VerticalStackLayout
        {
            Spacing = 3,
            Children =
            {
                titleLabel,
                categoryLabel
            }
        };

        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Padding = new Thickness(16)
        };

        row.Add(infoLayout, 0, 0);
        row.Add(amountLabel, 1, 0);

        var card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle
            {
                CornerRadius = new CornerRadius(16)
            },
            Content = row
        };

        // Make the whole card tappable to view details
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, e) =>
        {
            await Navigation.PushAsync(new TransactionDetailPage(transaction));
        };

        card.GestureRecognizers.Add(tap);

        TransactionsLayout.Children.Add(card);
    }

    private void AllButton_Clicked(object? sender, EventArgs e)
    {
        currentFilter = "all";

        SetActiveButton(AllButton);

        ShowTransactions();
    }

    private void IncomeButton_Clicked(object? sender, EventArgs e)
    {
        currentFilter = "income";

        SetActiveButton(IncomeButton);

        ShowTransactions();
    }

    private void ExpenseButton_Clicked(object? sender, EventArgs e)
    {
        currentFilter = "expense";

        SetActiveButton(ExpenseButton);

        ShowTransactions();
    }

    private void SetActiveButton(Button activeButton)
    {
        AllButton.BackgroundColor = Colors.White;
        AllButton.TextColor = Color.FromArgb("#171717");

        IncomeButton.BackgroundColor = Colors.White;
        IncomeButton.TextColor = Color.FromArgb("#171717");

        ExpenseButton.BackgroundColor = Colors.White;
        ExpenseButton.TextColor = Color.FromArgb("#171717");

        activeButton.BackgroundColor = Color.FromArgb("#171717");
        activeButton.TextColor = Colors.White;
    }

    private void SearchEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        ShowTransactions();
    }

    private async void BackButton_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}