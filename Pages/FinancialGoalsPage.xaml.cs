using FinanceAI.Helpers;
using FinanceAI.Models;

namespace FinanceAI.Pages;

public partial class FinancialGoalsPage : ContentPage
{
    private FinancialGoal? _editing;

    public FinancialGoalsPage()
    {
        InitializeComponent();
        GoalDatePicker.MinimumDate = new DateTime(2000, 1, 1);
        GoalDatePicker.Date = DateTime.Today.AddMonths(3);
        App.TransactionsChanged += OnTransactionsChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadGoalsAsync();
    }

    private void OnTransactionsChanged()
    {
        if (Handler is null)
            return;

        _ = LoadGoalsAsync();
    }

    private async void RefreshButton_Clicked(object? sender, EventArgs e) => await LoadGoalsAsync();

    private int _loadTicket;

    private async Task LoadGoalsAsync()
    {
        int ticket = Interlocked.Increment(ref _loadTicket);
        try
        {
            var goals = await App.Database.GetGoalsAsync();
            if (ticket != _loadTicket)
                return;
            var rows = new List<IReadOnlyList<View>>();

            foreach (var goal in goals)
            {
                double progress = goal.TargetAmount > 0
                    ? Math.Clamp((double)(goal.CurrentAmount / goal.TargetAmount), 0, 1)
                    : 0;
                bool done = goal.TargetAmount > 0 && goal.CurrentAmount >= goal.TargetAmount;
                var captured = goal;
                rows.Add(
                [
                    DataTable.Cell(captured.Name, bold: true),
                    DataTable.Cell(FinanceFormat.Money(captured.CurrentAmount), Palette.Income, bold: true),
                    DataTable.Cell(FinanceFormat.Money(captured.TargetAmount)),
                    DataTable.Cell(done ? "Tamam" : $"%{progress * 100:0}", done ? Palette.Income : Palette.Ink, bold: true, TextAlignment.End),
                    DataTable.Cell(FinanceFormat.Day(captured.TargetDate), Palette.Muted),
                    DataTable.Actions(() => BeginEdit(captured), () => DeleteGoalAsync(captured))
                ]);
            }

            GoalsLayout.Children.Clear();
            GoalsLayout.Children.Add(DataTable.Create(
            [
                new TableColumn("Hedef", GridLength.Star),
                new TableColumn("Biriken", new GridLength(120)),
                new TableColumn("Hedef tutar", new GridLength(120)),
                new TableColumn("İlerleme", new GridLength(90), TextAlignment.End),
                new TableColumn("Tarih", new GridLength(120)),
                new TableColumn("İşlem", new GridLength(130), TextAlignment.End)
            ], rows, "Henüz hedef yok."));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Veri hatası", "Hedefler okunamadı. " + ex.Message, "Tamam");
        }
    }

    private Task BeginEdit(FinancialGoal goal)
    {
        _editing = goal;
        FormTitleLabel.Text = $"{goal.Name} hedefini güncelle";
        SaveGoalButton.Text = "Değişiklikleri kaydet";
        AppIcons.Apply(SaveGoalButton, Glyph.Save, Colors.White);
        CancelGoalButton.IsVisible = true;
        GoalNameEntry.Text = goal.Name;
        GoalTargetEntry.Text = goal.TargetAmount.ToString("0.##", FinanceFormat.Culture);
        GoalCurrentEntry.Text = goal.CurrentAmount.ToString("0.##", FinanceFormat.Culture);
        GoalNoteEditor.Text = goal.Description;
        var minimum = GoalDatePicker.MinimumDate ?? new DateTime(2000, 1, 1);
        GoalDatePicker.Date = goal.TargetDate.Date < minimum ? minimum : goal.TargetDate.Date;
        return Task.CompletedTask;
    }

    private void CancelGoalButton_Clicked(object? sender, EventArgs e) => ClearForm();

    private void ClearForm()
    {
        _editing = null;
        FormTitleLabel.Text = "Yeni hedef";
        SaveGoalButton.Text = "Hedef oluştur";
        AppIcons.Apply(SaveGoalButton, Glyph.Add, Colors.White);
        CancelGoalButton.IsVisible = false;
        GoalNameEntry.Text = "";
        GoalTargetEntry.Text = "";
        GoalCurrentEntry.Text = "";
        GoalNoteEditor.Text = "";
        GoalDatePicker.Date = DateTime.Today.AddMonths(3);
    }

    private async Task DeleteGoalAsync(FinancialGoal goal)
    {
        bool ok = await DisplayAlertAsync("Hedefi sil", $"\"{goal.Name}\" silinecek.", "Sil", "Vazgeç");
        if (!ok)
            return;

        int deleted = await App.Database.DeleteGoalAsync(goal);
        if (deleted <= 0)
        {
            await DisplayAlertAsync("Silinemedi", "Hedef bulunamadı.", "Tamam");
            return;
        }

        if (_editing?.Id == goal.Id)
            ClearForm();

        await LoadGoalsAsync();
        App.RaiseTransactionsChanged();
    }

    private async void AddGoalButton_Clicked(object? sender, EventArgs e)
    {
        string name = GoalNameEntry.Text?.Trim() ?? "";
        if (name.Length == 0)
        {
            await DisplayAlertAsync("Hedef adı", "Hedefe bir ad ver.", "Tamam");
            return;
        }

        if (!MoneyParser.TryParse(GoalTargetEntry.Text, out decimal target) || target <= 0)
        {
            await DisplayAlertAsync("Geçersiz tutar", "Sıfırdan büyük bir hedef tutarı gir.", "Tamam");
            return;
        }

        decimal current = 0;
        if (!string.IsNullOrWhiteSpace(GoalCurrentEntry.Text))
        {
            if (!MoneyParser.TryParse(GoalCurrentEntry.Text, out current) || current < 0)
            {
                await DisplayAlertAsync("Geçersiz tutar", "Biriken tutar sıfır veya daha büyük olmalı.", "Tamam");
                return;
            }
        }

        var date = (GoalDatePicker.Date ?? DateTime.Today.AddMonths(3)).Date;
        string? note = string.IsNullOrWhiteSpace(GoalNoteEditor.Text) ? null : GoalNoteEditor.Text.Trim();

        try
        {
            if (_editing != null)
            {
                _editing.Name = name;
                _editing.TargetAmount = target;
                _editing.CurrentAmount = current;
                _editing.Description = note;
                _editing.TargetDate = date;
                int updated = await App.Database.UpdateGoalAsync(_editing);
                if (updated <= 0)
                {
                    await DisplayAlertAsync("Kaydedilemedi", "Hedef bulunamadı.", "Tamam");
                    return;
                }
            }
            else
            {
                await App.Database.AddGoalAsync(new FinancialGoal
                {
                    Name = name,
                    TargetAmount = target,
                    CurrentAmount = current,
                    Description = note,
                    TargetDate = date,
                    CreatedDate = DateTime.Now
                });
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Kayıt hatası", "Hedef kaydedilemedi. " + ex.Message, "Tamam");
            return;
        }

        ClearForm();
        await LoadGoalsAsync();
        App.RaiseTransactionsChanged();
    }
}
