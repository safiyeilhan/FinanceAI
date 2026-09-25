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

    // Mark sample/test rows so we can treat them differently if needed
    public bool IsSample { get; set; }
}
