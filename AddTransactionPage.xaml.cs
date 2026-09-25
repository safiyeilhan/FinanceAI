using FinanceAI.Models;

namespace FinanceAI;

public partial class AddTransactionPage : ContentPage
{
    private bool isIncome = true;

    public AddTransactionPage()
    {
        InitializeComponent();
    }

    private void IncomeButton_Clicked(object? sender, EventArgs e)
    {
        isIncome = true;

        IncomeButton.BackgroundColor = Color.FromArgb("#171717");
        IncomeButton.TextColor = Colors.White;

        ExpenseButton.BackgroundColor = Colors.White;
        ExpenseButton.TextColor = Color.FromArgb("#171717");
    }

    private void ExpenseButton_Clicked(object? sender, EventArgs e)
    {
        isIncome = false;

        ExpenseButton.BackgroundColor = Color.FromArgb("#171717");
        ExpenseButton.TextColor = Colors.White;

        IncomeButton.BackgroundColor = Colors.White;
        IncomeButton.TextColor = Color.FromArgb("#171717");
    }

    private async void BackButton_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void SaveButton_Clicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AmountEntry.Text))
        {
            await DisplayAlertAsync(
                "Eksik Bilgi",
                "Lütfen bir tutar girin.",
                "Tamam");

            return;
        }

        if (!decimal.TryParse(AmountEntry.Text, out decimal amount) || amount <= 0)
        {
            await DisplayAlertAsync(
                "Geçersiz Tutar",
                "Lütfen geçerli bir tutar girin.",
                "Tamam");

            return;
        }

        if (CategoryPicker.SelectedIndex == -1)
        {
            await DisplayAlertAsync(
                "Eksik Bilgi",
                "Lütfen bir kategori seçin.",
                "Tamam");

            return;
        }

        var transaction = new Transaction
        {
            Amount = amount,
            IsIncome = isIncome,
            Category = CategoryPicker.SelectedItem?.ToString() ?? "Diğer",
            Description = DescriptionEditor.Text ?? "",
            Date = DateTime.Now
        };

        await App.Database.AddTransactionAsync(transaction);

        await DisplayAlertAsync(
            "İşlem Kaydedildi",
            isIncome
                ? "Gelir işleminiz başarıyla kaydedildi."
                : "Gider işleminiz başarıyla kaydedildi.",
            "Tamam");

        // Return to previous page then notify others to refresh their data
        await Navigation.PopAsync();

        App.RaiseTransactionsChanged();
    }
}