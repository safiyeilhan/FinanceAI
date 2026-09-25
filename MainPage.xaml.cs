using FinanceAI.Models;
using Microsoft.Maui.Controls.Shapes;
using System;

namespace FinanceAI;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Subscribe to transaction change notifications so we update when data changes
        App.TransactionsChanged += OnTransactionsChanged;

        await LoadFinancialData();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        App.TransactionsChanged -= OnTransactionsChanged;
    }

    private async void OnTransactionsChanged()
    {
        await LoadFinancialData();
    }

    private async Task LoadFinancialData()
    {
        List<Transaction> transactions =
            await App.Database.GetTransactionsAsync();

        decimal totalIncome = transactions
            .Where(x => x.IsIncome)
            .Sum(x => x.Amount);

        decimal totalExpense = transactions
            .Where(x => !x.IsIncome)
            .Sum(x => x.Amount);

        decimal balance = totalIncome - totalExpense;

        TotalBalanceLabel.Text = $"₺{balance:N2}";
        IncomeLabel.Text = $"₺{totalIncome:N2}";
        ExpenseLabel.Text = $"₺{totalExpense:N2}";

        LoadRecentTransactions(transactions);
    }

    private void LoadRecentTransactions(List<Transaction> transactions)
    {
        TransactionsLayout.Children.Clear();

        var recentTransactions = transactions
            .OrderByDescending(x => x.Date)
            .Take(5)
            .ToList();

        foreach (var transaction in recentTransactions)
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
                    new ColumnDefinition
                    {
                        Width = GridLength.Star
                    },

                    new ColumnDefinition
                    {
                        Width = GridLength.Auto
                    }
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

            TransactionsLayout.Children.Add(card);
        }
    }

    private async void AddIncome_Clicked(
        object? sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new AddTransactionPage());
    }

    private async void AddExpense_Clicked(
        object? sender,
        EventArgs e)
    {
        await Navigation.PushAsync(
            new AddTransactionPage());
    }

    private async void AllTransactions_Tapped(
        object? sender,
        TappedEventArgs e)
    {
        await Navigation.PushAsync(
            new TransactionsPage());
    }

    private async void Dashboard_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new Pages.DashboardPage());
    }

    private async void Assistant_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new Pages.FinancialAssistantPage());
    }
}