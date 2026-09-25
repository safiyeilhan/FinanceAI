using FinanceAI.Models;

namespace FinanceAI;

public partial class TransactionDetailPage : ContentPage
{
    private readonly Transaction _transaction;

    public TransactionDetailPage(Transaction transaction)
    {
        InitializeComponent();

        _transaction = transaction;

        LoadDetails();

        DeleteButton.Clicked += DeleteButton_Clicked;
    }

    private void LoadDetails()
    {
        TypeLabel.Text = _transaction.IsIncome ? "Tür: Gelir" : "Tür: Gider";
        AmountLabel.Text = $"Tutar: ₺{_transaction.Amount:N2}";
        CategoryLabel.Text = $"Kategori: {_transaction.Category}";
        DescriptionLabel.Text = $"Açıklama: {_transaction.Description}";
        DateLabel.Text = $"Tarih: {_transaction.Date:dd.MM.yyyy HH:mm}";
    }

    private async void DeleteButton_Clicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync(
            "Onay",
            "Bu işlemi silmek istediğinizden emin misiniz?",
            "Sil",
            "İptal");

        if (!confirm)
            return;

        // Prefer delete by Id to ensure removal even if object instance differs
        await App.Database.DeleteTransactionByIdAsync(_transaction.Id);

        // Return to transactions list first so it can re-subscribe, then notify
        await Navigation.PopAsync();

        App.RaiseTransactionsChanged();
    }
}
