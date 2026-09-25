using SQLite;
using FinanceAI.Models;

namespace FinanceAI.Data;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _database;

    public DatabaseService()
    {
        string databasePath = Path.Combine(
            FileSystem.AppDataDirectory,
            "FinanceAI.db3");

        _database = new SQLiteAsyncConnection(databasePath);
    }

    public async Task InitializeAsync()
    {
        await _database.CreateTableAsync<Transaction>();
        await _database.CreateTableAsync<FinanceAI.Models.FinancialGoal>();
        await _database.CreateTableAsync<FinanceAI.Models.Budget>();
    }

    public async Task<int> AddTransactionAsync(Transaction transaction)
    {
        return await _database.InsertAsync(transaction);
    }

    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        return await _database
            .Table<Transaction>()
            .OrderByDescending(x => x.Date)
            .ToListAsync();
    }

    public async Task<int> DeleteTransactionAsync(Transaction transaction)
    {
        if (transaction == null) return 0;
        // Use ORM delete to avoid SQL identifier conflicts (e.g. reserved word 'Transaction')
        return await _database.DeleteAsync(transaction);
    }

    public async Task<int> DeleteTransactionByIdAsync(int id)
    {
        // Use SQLite-net API to delete by primary key rather than hard-coding the table name.
        // This avoids mismatches between model/table naming (Transaction vs Transactions).
        try
        {
            return await _database.DeleteAsync<Transaction>(id);
        }
        catch
        {
            // Fallback for older sqlite-net versions: delete by passing a temporary instance.
            var temp = new Transaction { Id = id };
            return await _database.DeleteAsync(temp);
        }
    }

    // FinancialGoal methods
    public async Task<int> AddGoalAsync(Models.FinancialGoal goal)
    {
        return await _database.InsertAsync(goal);
    }

    public async Task<List<Models.FinancialGoal>> GetGoalsAsync()
    {
        return await _database.Table<Models.FinancialGoal>().OrderBy(x => x.TargetDate).ToListAsync();
    }

    public async Task<int> UpdateGoalAsync(Models.FinancialGoal goal)
    {
        return await _database.UpdateAsync(goal);
    }

    public async Task<int> DeleteGoalAsync(Models.FinancialGoal goal)
    {
        return await _database.DeleteAsync(goal);
    }

    // Budget methods
    public async Task<int> AddBudgetAsync(Models.Budget budget)
    {
        return await _database.InsertAsync(budget);
    }

    public async Task<List<Models.Budget>> GetBudgetsAsync()
    {
        return await _database.Table<Models.Budget>().OrderBy(x => x.Category).ToListAsync();
    }

    public async Task<int> UpdateBudgetAsync(Models.Budget budget)
    {
        return await _database.UpdateAsync(budget);
    }

    public async Task<int> DeleteBudgetAsync(Models.Budget budget)
    {
        return await _database.DeleteAsync(budget);
    }

    // Diagnostics: return PRAGMA table_info results mapped to a POCO
    public async Task<List<TableInfoRow>> GetTableInfoAsync(string tableName)
    {
        var sql = $"PRAGMA table_info(\"{tableName}\");";
        var rows = await _database.QueryAsync<TableInfoRow>(sql);
        return rows;
    }

    // Diagnostics: return Transaction rows (maps to Transaction model)
    public async Task<List<Transaction>> GetRawTableRowsAsync(string tableName)
    {
        // Use sqlite-net's QueryAsync to map rows to Transaction; this avoids low-level SQLitePCL usage
        var sql = $"SELECT * FROM \"{tableName}\";";
        var rows = await _database.QueryAsync<Transaction>(sql);
        return rows;
    }

    public class TableInfoRow
    {
        public int Cid { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Notnull { get; set; }
        [Column("dflt_value")]
        public string DfltValue { get; set; } = string.Empty;
        public int Pk { get; set; }
    }

}