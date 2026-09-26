using FinanceAI.Helpers;
using FinanceAI.Models;

namespace FinanceAI;

public partial class AddTransactionPage : ContentPage
{
    private bool _isIncome = true;
    private bool _saving;

    public AddTransactionPage() : this(true)
    {
    }

    public AddTransactionPage(bool isIncome)
    {
        InitializeComponent();
        DatePicker.Date = DateTime.Today;
        SetIncome(isIncome);
    }

    private void IncomeButton_Clicked(object? sender, EventArgs e) => SetIncome(true);

    private void ExpenseButton_Clicked(object? sender, EventArgs e) => SetIncome(false);

    private void SetIncome(bool isIncome)
    {
        _isIncome = isIncome;
        Title = isIncome ? "Gelir ekle" : "Gider ekle";
        TypeHintLabel.Text = isIncome
            ? "Gelir bakiyeyi artırır. Maaş ve ek kazançları buradan gir."
            : "Gider bakiyeyi düşürür. Kategorisi bütçen varsa limit aşımında uyarı çıkar.";
        Notices.SetInline(ErrorLabel, null);

        IncomeButton.BackgroundColor = isIncome ? Palette.Accent : Colors.White;
        IncomeButton.TextColor = isIncome ? Colors.White : Palette.Ink;

        ExpenseButton.BackgroundColor = isIncome ? Colors.White : Palette.Expense;
        ExpenseButton.TextColor = isIncome ? Palette.Ink : Colors.White;

        var selected = CategoryPicker.SelectedItem?.ToString();
        CategoryPicker.Items.Clear();
        foreach (string category in Categories.For(isIncome))
            CategoryPicker.Items.Add(category);

        if (selected != null && CategoryPicker.Items.Contains(selected))
            CategoryPicker.SelectedItem = selected;
        else
            CategoryPicker.SelectedIndex = -1;
    }

    private async void SaveButton_Clicked(object? sender, EventArgs e)
    {
        if (_saving)
            return;

        Notices.SetInline(ErrorLabel, null);

        string amountError = Notices.AmountError(AmountEntry.Text);
        if (amountError.Length > 0)
        {
            Notices.SetInline(ErrorLabel, amountError);
            return;
        }

        MoneyParser.TryParse(AmountEntry.Text, out decimal amount);

        if (CategoryPicker.SelectedIndex < 0 || CategoryPicker.SelectedItem is null)
        {
            Notices.SetInline(ErrorLabel, Notices.NeedCategory);
            return;
        }

        var pickedDate = DatePicker.Date ?? DateTime.Today;
        string? future = Notices.FutureDateQuestion(pickedDate);
        if (future != null && !await this.ConfirmAsync("İleri tarih", future, "Kaydet"))
            return;

        string? large = Notices.LargeAmountQuestion(amount, _isIncome);
        if (large != null && !await this.ConfirmAsync("Yüksek tutar", large, "Kaydet"))
            return;

        string category = CategoryPicker.SelectedItem.ToString() ?? "Diğer";
        if (!_isIncome)
        {
            var budgets = await App.Database.GetBudgetsAsync();
            var existing = await App.Database.GetTransactionsAsync();
            string? over = Notices.BudgetOverspend(budgets, existing, category, amount, pickedDate);
            if (over != null && !await this.ConfirmAsync("Bütçe aşımı", over, "Yine de kaydet"))
                return;
        }

        var when = new DateTime(
            pickedDate.Year,
            pickedDate.Month,
            pickedDate.Day,
            DateTime.Now.Hour,
            DateTime.Now.Minute,
            DateTime.Now.Second,
            DateTimeKind.Local);

        var transaction = new Transaction
        {
            Amount = amount,
            IsIncome = _isIncome,
            Category = category,
            Description = DescriptionEditor.Text?.Trim() ?? "",
            Date = when
        };

        _saving = true;
        SaveButton.IsEnabled = false;
        try
        {
            await App.Database.AddTransactionAsync(transaction);
            if (transaction.Id <= 0)
            {
                Notices.SetInline(ErrorLabel, Notices.SaveFailed);
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
            _saving = false;
        }
    }
}
