using FinanceAI.Models;

namespace FinanceAI.Helpers;

public static class TransactionCards
{
    public static View CreateTable(
        IReadOnlyList<Transaction> transactions,
        Func<Transaction, Task> onEdit,
        Func<Transaction, Task> onDelete,
        string emptyText)
    {
        TableColumn[] columns =
        [
            new("Tarih", new GridLength(112)),
            new("Tür", new GridLength(72)),
            new("Kategori", new GridLength(130)),
            new("Açıklama", GridLength.Star),
            new("Tutar", new GridLength(120), TextAlignment.End),
            new("İşlem", new GridLength(130), TextAlignment.End)
        ];

        var rows = transactions.Select(transaction =>
        {
            var kindColor = transaction.IsIncome ? Palette.Income : Palette.Expense;
            IReadOnlyList<View> cells =
            [
                DataTable.Cell(FinanceFormat.Day(transaction.Date), Palette.Muted),
                DataTable.Cell(transaction.IsIncome ? "Gelir" : "Gider", kindColor, bold: true),
                DataTable.Cell(string.IsNullOrWhiteSpace(transaction.Category) ? "—" : transaction.Category, bold: true),
                DataTable.Cell(string.IsNullOrWhiteSpace(transaction.Description) ? "—" : transaction.Description.Trim(), Palette.Muted),
                DataTable.Cell(FinanceFormat.SignedMoney(transaction.Amount, transaction.IsIncome), kindColor, bold: true, align: TextAlignment.End),
                DataTable.Actions(() => onEdit(transaction), () => onDelete(transaction))
            ];
            return cells;
        });

        return DataTable.Create(columns, rows, emptyText);
    }
}
