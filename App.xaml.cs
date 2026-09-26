using FinanceAI.Data;

namespace FinanceAI;

public partial class App : Application
{
    public static DatabaseService Database { get; private set; } = null!;

    public static event Action? TransactionsChanged;

    public static void RaiseTransactionsChanged()
    {
        if (MainThread.IsMainThread)
            TransactionsChanged?.Invoke();
        else
            MainThread.BeginInvokeOnMainThread(() => TransactionsChanged?.Invoke());
    }

    public App()
    {
        UserAppTheme = AppTheme.Light;
        InitializeComponent();
        Database = new DatabaseService();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell())
        {
            Title = "FinanceAI",
            Width = 1200,
            Height = 800,
            MinimumWidth = 980,
            MinimumHeight = 640
        };
    }
}
