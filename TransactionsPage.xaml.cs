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
        App.TransactionsChanged += OnTransactionsChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ReloadAsync();
    }

    private void OnTransactionsChanged()
    {
        if (Handler is null)
            return;

        _ = ReloadAsync();
    }

    private async void RefreshButton_Clicked(object? sender, EventArgs e) => await ReloadAsync();

    private int _loadTicket;

    private async Task ReloadAsync()
    {
        int ticket = Interlocked.Increment(ref _loadTicket);
        try
        {
            var transactions = await App.Database.GetTransactionsAsync();
            if (ticket != _loadTicket)
                return;

            _allTransactions = transactions;
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
        StyleFilter(AllButton, activeButton == AllButton, Glyph.List, Palette.Ink);
        StyleFilter(IncomeButton, activeButton == IncomeButton, Glyph.Up, Palette.Income);
        StyleFilter(ExpenseButton, activeButton == ExpenseButton, Glyph.Down, Palette.Expense);
    }

    private static void StyleFilter(Button button, bool active, string glyph, Color idle)
    {
        button.BackgroundColor = active ? Palette.Accent : Colors.White;
        button.TextColor = active ? Colors.White : idle;
        AppIcons.Apply(button, glyph, active ? Colors.White : idle);
    }

    private void SearchEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        ShowTransactions();
    }
}
