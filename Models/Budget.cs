using SQLite;

namespace FinanceAI.Models;

public class Budget
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Category name (e.g., Yemek, Ulaşım)
    public string Category { get; set; } = string.Empty;

    // Monthly limit in currency
    public decimal MonthlyLimit { get; set; }

    // Optional year/month. If Year==0 and Month==0 -> applies to all months (recurring)
    public int Year { get; set; }
    public int Month { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
