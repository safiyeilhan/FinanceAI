using FinanceAI.Data;

namespace FinanceAI;

public partial class App : Application
{
    public static DatabaseService Database { get; private set; } = null!;

    // Raised when transactions change (added/deleted) so pages can refresh UI
    public static event Action? TransactionsChanged;

    // External callers should use this to notify subscribers.
    public static void RaiseTransactionsChanged()
    {
        TransactionsChanged?.Invoke();
    }

    public App()
    {
        InitializeComponent();

        Database = new DatabaseService();
        InitializeDatabase();
    }

    private static async void InitializeDatabase()
    {
        await Database.InitializeAsync();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}