using SQLite;
using FinanceAI.Models;

namespace FinanceAI.Data;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _ready;

    public DatabaseService()
    {
        string databasePath = Path.Combine(FileSystem.AppDataDirectory, "FinanceAI.db3");
        _database = new SQLiteAsyncConnection(databasePath);
    }

    private async Task EnsureReadyAsync()
    {
        if (_ready)
            return;

        await _gate.WaitAsync();
        try
        {
            if (_ready)
                return;

            await _database.CreateTableAsync<Transaction>();
            await _database.CreateTableAsync<FinancialGoal>();
            await _database.CreateTableAsync<Budget>();
            await PurgeLegacySampleTransactionsAsync();
            _ready = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task PurgeLegacySampleTransactionsAsync()
    {
        if (Preferences.Default.Get("legacy_sample_transactions_purged", false))
            return;

        var columns = await _database.GetTableInfoAsync("Transaction");
        bool hasSampleFlag = columns.Any(c =>
            string.Equals(c.Name, "IsSample", StringComparison.OrdinalIgnoreCase));

        if (hasSampleFlag)
        {
            await _database.ExecuteAsync(
                """DELETE FROM "Transaction" WHERE "IsSample" = 1 OR "Description" IN (?, ?)""",
                "Günlük harcama",
                "Ek gelir");
        }
        else
        {
            await _database.ExecuteAsync(
                """DELETE FROM "Transaction" WHERE "Description" IN (?, ?)""",
                "Günlük harcama",
                "Ek gelir");
        }

        Preferences.Default.Set("legacy_sample_transactions_purged", true);
    }

    public async Task<int> AddTransactionAsync(Transaction transaction)
    {
        await EnsureReadyAsync();
        return await _database.InsertAsync(transaction);
    }

    public async Task<int> UpdateTransactionAsync(Transaction transaction)
    {
        await EnsureReadyAsync();
        if (transaction.Id <= 0)
            return 0;

        return await _database.UpdateAsync(transaction);
    }

    public async Task<List<Transaction>> GetTransactionsAsync()
    {
        await EnsureReadyAsync();
        return await _database.Table<Transaction>().OrderByDescending(x => x.Date).ToListAsync();
    }

    public async Task<int> DeleteTransactionByIdAsync(int id)
    {
        await EnsureReadyAsync();
        if (id <= 0)
            return 0;

        try
        {
            int deleted = await _database.DeleteAsync<Transaction>(id);
            if (deleted > 0)
                return deleted;
        }
        catch
        {
            // Some sqlite-net builds reject the generic delete. The statement below uses the same table.
        }

        return await _database.ExecuteAsync("DELETE FROM \"Transaction\" WHERE \"Id\" = ?", id);
    }

    public async Task<int> AddGoalAsync(FinancialGoal goal)
    {
        await EnsureReadyAsync();
        return await _database.InsertAsync(goal);
    }

    public async Task<List<FinancialGoal>> GetGoalsAsync()
    {
        await EnsureReadyAsync();
        return await _database.Table<FinancialGoal>().OrderBy(x => x.TargetDate).ToListAsync();
    }

    public async Task<int> UpdateGoalAsync(FinancialGoal goal)
    {
        await EnsureReadyAsync();
        if (goal.Id <= 0)
            return 0;

        return await _database.UpdateAsync(goal);
    }

    public async Task<int> DeleteGoalAsync(FinancialGoal goal)
    {
        await EnsureReadyAsync();
        if (goal.Id <= 0)
            return 0;

        return await _database.DeleteAsync(goal);
    }

    public async Task<int> AddBudgetAsync(Budget budget)
    {
        await EnsureReadyAsync();
        return await _database.InsertAsync(budget);
    }

    public async Task<List<Budget>> GetBudgetsAsync()
    {
        await EnsureReadyAsync();
        return await _database.Table<Budget>().OrderBy(x => x.Category).ToListAsync();
    }

    public async Task<int> UpdateBudgetAsync(Budget budget)
    {
        await EnsureReadyAsync();
        if (budget.Id <= 0)
            return 0;

        return await _database.UpdateAsync(budget);
    }

    public async Task<int> DeleteBudgetAsync(Budget budget)
    {
        await EnsureReadyAsync();
        if (budget.Id <= 0)
            return 0;

        return await _database.DeleteAsync(budget);
    }
}
