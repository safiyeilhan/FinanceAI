using FinanceAI.Helpers;
using FinanceAI.Models;

namespace FinanceAI;

public partial class TransactionDetailPage : ContentPage
{
    private readonly Transaction _transaction;
    private bool _isIncome;
    private bool _busy;

    public TransactionDetailPage(Transaction transaction)
    {
        InitializeComponent();
        _transaction = transaction;
        _isIncome = transaction.IsIncome;

        AmountEntry.Text = transaction.Amount.ToString("0.##", FinanceFormat.Culture);
        DescriptionEditor.Text = transaction.Description;
        WhenPicker.Date = transaction.Date.Date;
        ApplyType(keepCategory: transaction.Category);
    }

    private void IncomeButton_Clicked(object? sender, EventArgs e)
    {
        _isIncome = true;
        ApplyType(keepCategory: CategoryPicker.SelectedItem?.ToString());
    }

    private void ExpenseButton_Clicked(object? sender, EventArgs e)
    {
        _isIncome = false;
        ApplyType(keepCategory: CategoryPicker.SelectedItem?.ToString());
    }

    private void ApplyType(string? keepCategory)
    {
        Notices.SetInline(ErrorLabel, null);
        IncomeButton.BackgroundColor = _isIncome ? Palette.Accent : Colors.White;
        IncomeButton.TextColor = _isIncome ? Colors.White : Palette.Ink;
        ExpenseButton.BackgroundColor = _isIncome ? Colors.White : Palette.Expense;
        ExpenseButton.TextColor = _isIncome ? Palette.Ink : Colors.White;

        CategoryPicker.Items.Clear();
        foreach (string category in Categories.For(_isIncome))
            CategoryPicker.Items.Add(category);

        if (!string.IsNullOrWhiteSpace(keepCategory) && !CategoryPicker.Items.Contains(keepCategory))
            CategoryPicker.Items.Insert(0, keepCategory);

        if (!string.IsNullOrWhiteSpace(keepCategory))
            CategoryPicker.SelectedItem = keepCategory;
    }

    private async void SaveButton_Clicked(object? sender, EventArgs e)
    {
        if (_busy)
            return;

        Notices.SetInline(ErrorLabel, null);

        string amountError = Notices.AmountError(AmountEntry.Text);
        if (amountError.Length > 0)
        {
            Notices.SetInline(ErrorLabel, amountError);
            return;
        }

        MoneyParser.TryParse(AmountEntry.Text, out decimal amount);

        if (CategoryPicker.SelectedItem is null)
        {
            Notices.SetInline(ErrorLabel, Notices.NeedCategory);
            return;
        }

        var day = WhenPicker.Date ?? _transaction.Date;
        string category = CategoryPicker.SelectedItem.ToString() ?? "Diğer";

        string? future = Notices.FutureDateQuestion(day);
        if (future != null && !await this.ConfirmAsync("İleri tarih", future, "Kaydet"))
            return;

        string? large = Notices.LargeAmountQuestion(amount, _isIncome);
        if (large != null && !await this.ConfirmAsync("Yüksek tutar", large, "Kaydet"))
            return;

        if (!_isIncome)
        {
            var budgets = await App.Database.GetBudgetsAsync();
            var existing = await App.Database.GetTransactionsAsync();
            string? over = Notices.BudgetOverspend(budgets, existing, category, amount, day, _transaction.Id);
            if (over != null && !await this.ConfirmAsync("Bütçe aşımı", over, "Yine de kaydet"))
                return;
        }

        _transaction.Amount = amount;
        _transaction.IsIncome = _isIncome;
        _transaction.Category = category;
        _transaction.Description = DescriptionEditor.Text?.Trim() ?? "";
        _transaction.Date = new DateTime(
            day.Year,
            day.Month,
            day.Day,
            _transaction.Date.Hour,
            _transaction.Date.Minute,
            _transaction.Date.Second,
            DateTimeKind.Local);

        _busy = true;
        SaveButton.IsEnabled = false;
        try
        {
            int updated = await App.Database.UpdateTransactionAsync(_transaction);
            if (updated <= 0)
            {
                Notices.SetInline(ErrorLabel, Notices.MissingRecord);
                SaveButton.IsEnabled = true;
                return;
            }

            await Navigation.PopAsync();
            App.RaiseTransactionsChanged();
        }
        catch
        {
            Notices.SetInline(ErrorLabel, Notices.SaveFailed);
            SaveButton.IsEnabled = true;
        }
        finally
        {
            _busy = false;
        }
    }

    private async void DeleteButton_Clicked(object? sender, EventArgs e)
    {
        if (_busy)
            return;

        bool confirm = await this.ConfirmAsync(
            "İşlemi sil",
            Notices.DeleteTransaction(_transaction),
            "Sil");

        if (!confirm)
            return;

        _busy = true;
        DeleteButton.IsEnabled = false;
        try
        {
            int deleted = await App.Database.DeleteTransactionByIdAsync(_transaction.Id);
            if (deleted <= 0)
            {
                Notices.SetInline(ErrorLabel, Notices.MissingRecord);
                DeleteButton.IsEnabled = true;
                return;
            }

            await Navigation.PopAsync();
            App.RaiseTransactionsChanged();
        }
        catch
        {
            Notices.SetInline(ErrorLabel, Notices.SaveFailed);
            DeleteButton.IsEnabled = true;
        }
        finally
        {
            _busy = false;
        }
    }
}
