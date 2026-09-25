using SQLite;

namespace FinanceAI.Models;

public class FinancialGoal
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal TargetAmount { get; set; }

    public decimal CurrentAmount { get; set; }

    public string? Description { get; set; }

    public DateTime TargetDate { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;
}
