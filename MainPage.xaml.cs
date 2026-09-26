using FinanceAI.Helpers;
using FinanceAI.Models;
using FinanceAI.Pages;

namespace FinanceAI;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        DateLabel.Text = DateTime.Now.ToString("d MMMM yyyy", FinanceFormat.Culture);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        App.TransactionsChanged -= OnTransactionsChanged;
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
        if (!IsLoaded)
            return;

        await LoadFinancialData();
    }

    private async Task LoadFinancialData()
    {
        try
        {
            List<Transaction> transactions = await App.Database.GetTransactionsAsync();

            decimal totalIncome = transactions.Where(x => x.IsIncome).Sum(x => x.Amount);
            decimal totalExpense = transactions.Where(x => !x.IsIncome).Sum(x => x.Amount);
            decimal balance = totalIncome - totalExpense;
            var now = DateTime.Now;
            var thisMonth = transactions.Where(x => x.Date.Year == now.Year && x.Date.Month == now.Month).ToList();
            decimal monthIncome = thisMonth.Where(x => x.IsIncome).Sum(x => x.Amount);
            decimal monthExpense = thisMonth.Where(x => !x.IsIncome).Sum(x => x.Amount);

            TotalBalanceLabel.Text = FinanceFormat.Money(balance);
            IncomeLabel.Text = FinanceFormat.Money(totalIncome);
            ExpenseLabel.Text = FinanceFormat.Money(totalExpense);
            BalanceHintLabel.Text = balance < 0
                ? "Giderler gelirlerden fazla"
                : "Tüm zamanlar · gelir eksi gider";

            FillNotices(transactions.Count, balance, monthIncome, monthExpense);
            LoadRecentTransactions(transactions);
        }
        catch
        {
            await this.AlertAsync("Veriler yüklenemedi", Notices.LoadFailed);
        }
    }

    private void FillNotices(int count, decimal balance, decimal monthIncome, decimal monthExpense)
    {
        NoticeHost.Children.Clear();
        if (count == 0)
        {
            NoticeHost.Children.Add(Notices.Banner("Henüz işlem yok. Gelir veya gider ekleyerek bakiyeyi doldur.", NoticeKind.Info));
            return;
        }

        if (balance < 0)
            NoticeHost.Children.Add(Notices.Banner($"Toplam giderler, gelirleri {FinanceFormat.Money(Math.Abs(balance))} geçmiş.", NoticeKind.Danger));
        else if (monthIncome > 0 && monthExpense > monthIncome)
            NoticeHost.Children.Add(Notices.Banner("Bu ay giderler geliri geçti. Büyük kalemleri bütçeyle sınırlamak işe yarar.", NoticeKind.Warning));
        else if (monthIncome == 0 && monthExpense > 0)
            NoticeHost.Children.Add(Notices.Banner("Bu ay gelir kaydı yok, gider var. Maaş veya ek gelir eklenmemiş olabilir.", NoticeKind.Warning));
    }

    private void LoadRecentTransactions(List<Transaction> transactions)
    {
        TransactionsLayout.Children.Clear();

        var recent = transactions
            .OrderByDescending(x => x.Date)
            .Take(8)
            .ToList();

        TransactionsLayout.Children.Add(TransactionCards.CreateTable(
            recent,
            OpenTransactionAsync,
            DeleteTransactionAsync,
            "Henüz işlem yok. Sağdaki gelir veya gider ile ilk kaydı ekle."));
    }

    private async Task OpenTransactionAsync(Transaction transaction)
    {
        await Navigation.PushAsync(new TransactionDetailPage(transaction));
    }

    private async Task DeleteTransactionAsync(Transaction transaction)
    {
        bool confirm = await this.ConfirmAsync(
            "İşlemi sil",
            Notices.DeleteTransaction(transaction),
            "Sil");

        if (!confirm)
            return;

        int deleted = await App.Database.DeleteTransactionByIdAsync(transaction.Id);
        if (deleted <= 0)
        {
            await this.AlertAsync("Silinemedi", Notices.MissingRecord);
            return;
        }

        App.RaiseTransactionsChanged();
    }

    private async void AddIncome_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddTransactionPage(isIncome: true));
    }

    private async void AddExpense_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new AddTransactionPage(isIncome: false));
    }

    private async void AllTransactions_Tapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//transactions");
    }

    private async void Reports_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new FinancialReportsPage());
    }

    private async void Assistant_Clicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new FinancialAssistantPage());
    }
}
