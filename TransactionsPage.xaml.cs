using FinanceAI.Helpers;
using FinanceAI.Models;

namespace FinanceAI;

public partial class TransactionsPage : ContentPage
{
    private List<Transaction> _allTransactions = [];
    private string _currentFilter = "all";

    public TransactionsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        App.TransactionsChanged -= OnTransactionsChanged;
        App.TransactionsChanged += OnTransactionsChanged;
        await ReloadAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        App.TransactionsChanged -= OnTransactionsChanged;
    }

    private async void OnTransactionsChanged()
    {
        if (!IsLoaded)
            return;

        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        try
        {
            _allTransactions = await App.Database.GetTransactionsAsync();
            ShowTransactions();
        }
        catch
        {
            await this.AlertAsync("İşlemler açılamadı", Notices.LoadFailed);
        }
    }

    private void ShowTransactions()
    {
        TransactionsLayout.Children.Clear();

        IEnumerable<Transaction> filtered = _allTransactions;
        if (_currentFilter == "income")
            filtered = filtered.Where(x => x.IsIncome);
        else if (_currentFilter == "expense")
            filtered = filtered.Where(x => !x.IsIncome);

        string searchText = SearchEntry.Text?.Trim() ?? "";
        if (searchText.Length > 0)
        {
            filtered = filtered.Where(x =>
                (x.Category ?? "").Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                (x.Description ?? "").Contains(searchText, StringComparison.CurrentCultureIgnoreCase));
        }

        var list = filtered.OrderByDescending(x => x.Date).ToList();
        CountLabel.Text = list.Count == 1 ? "1 kayıt · satırdan düzenle veya sil" : $"{list.Count} kayıt · satırdan düzenle veya sil";

        string empty = _allTransactions.Count == 0
            ? "Henüz işlem yok. Sağ üstten gelir veya gider ekle."
            : "Bu süzgeçte işlem yok.";

        TransactionsLayout.Children.Add(
            TransactionCards.CreateTable(list, OpenTransactionAsync, DeleteTransactionAsync, empty));
    }

    private async void AddIncome_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddTransactionPage(isIncome: true));
    }

    private async void AddExpense_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddTransactionPage(isIncome: false));
    }

    private async Task OpenTransactionAsync(Transaction transaction)
    {
        await Navigation.PushAsync(new TransactionDetailPage(transaction));
    }

    private async Task DeleteTransactionAsync(Transaction transaction)
    {
        bool confirm = await DisplayAlertAsync(
            "İşlemi sil",
            "Bu kayıt kalıcı olarak silinecek.",
            "Sil",
            "Vazgeç");

        if (!confirm)
            return;

        int deleted = await App.Database.DeleteTransactionByIdAsync(transaction.Id);
        if (deleted <= 0)
        {
            await DisplayAlertAsync("Silinemedi", "Kayıt veritabanında bulunamadı.", "Tamam");
            return;
        }

        App.RaiseTransactionsChanged();
    }

    private void AllButton_Clicked(object? sender, EventArgs e)
    {
        _currentFilter = "all";
        SetActiveButton(AllButton);
        ShowTransactions();
    }

    private void IncomeButton_Clicked(object? sender, EventArgs e)
    {
        _currentFilter = "income";
        SetActiveButton(IncomeButton);
        ShowTransactions();
    }

    private void ExpenseButton_Clicked(object? sender, EventArgs e)
    {
        _currentFilter = "expense";
        SetActiveButton(ExpenseButton);
        ShowTransactions();
    }

    private void SetActiveButton(Button activeButton)
    {
        foreach (var button in new[] { AllButton, IncomeButton, ExpenseButton })
        {
            button.BackgroundColor = Colors.White;
            button.TextColor = Palette.Ink;
        }

        activeButton.BackgroundColor = Palette.Accent;
        activeButton.TextColor = Colors.White;
    }

    private void SearchEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        ShowTransactions();
    }
}
