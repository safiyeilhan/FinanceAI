using SQLite;

namespace FinanceAI.Models;

public class Transaction
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public bool IsIncome { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    // Eski "Sahte Veri Ekle" düğmesinin bıraktığı satırlar. Yeni kayıtlar false kalır.
    public bool IsSample { get; set; }
}
