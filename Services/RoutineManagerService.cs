using LifeHacks.Models;
using Microsoft.JSInterop;

namespace LifeHacks.Services;

public class RoutineManagerService
{
    private readonly IStorageService _storage;
    private readonly IJSRuntime _js;

    public List<DailyCheckupItem> Habits { get; private set; } = [];
    public List<DoseTracker> Trackers { get; private set; } = [];
    public List<PurchasePlanItem> Purchases { get; private set; } = [];
    public List<DayRecord> History { get; private set; } = [];
    public AppSettings Settings { get; private set; } = new();
    public DateOnly LastActiveDate { get; private set; } = DateOnly.FromDateTime(DateTime.Now);
    public int CurrentStreak { get; private set; } = 0;
    public bool IsInitialized { get; private set; } = false;

    public event Action? OnChange;

    public RoutineManagerService(IStorageService storage, IJSRuntime js)
    {
        _storage = storage;
        _js = js;
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        Settings = await _storage.GetItemAsync<AppSettings>("lifehacks_settings") ?? new AppSettings();
        Habits = await _storage.GetItemAsync<List<DailyCheckupItem>>("lifehacks_habits") ?? [];
        Trackers = await _storage.GetItemAsync<List<DoseTracker>>("lifehacks_trackers") ?? [];
        Purchases = await _storage.GetItemAsync<List<PurchasePlanItem>>("lifehacks_purchases") ?? [];
        History = await _storage.GetItemAsync<List<DayRecord>>("lifehacks_history") ?? [];

        var storedDateStr = await _storage.GetItemAsync<string>("lifehacks_last_date");
        var today = DateOnly.FromDateTime(DateTime.Now);

        if (DateOnly.TryParse(storedDateStr, out var storedDate))
        {
            LastActiveDate = storedDate;
        }
        else
        {
            LastActiveDate = today;
        }

        // 1. Seed padrão para hábitos diários (se vazio)
        if (Habits.Count == 0)
        {
            SeedDefaultHabits();
            await SaveHabitsAsync();
        }

        // 2. Seed padrão para Trackers quantitativos (se vazio)
        if (Trackers.Count == 0)
        {
            SeedDefaultTrackers();
            await SaveTrackersAsync();
        }

        // 3. Verificação de virada de dia (reset automático 00:00)
        if (today > LastActiveDate)
        {
            await HandleDayRolloverAsync(LastActiveDate, today);
        }

        CalculateStreak();
        IsInitialized = true;
        NotifyStateChanged();
    }

    private void SeedDefaultHabits()
    {
        Habits =
        [
            new DailyCheckupItem
            {
                Title = "Creatina",
                Icon = "💊",
                TargetDeadline = new TimeOnly(10, 0),
                IsSystemItem = true
            },
            new DailyCheckupItem
            {
                Title = "Pílulas tomadas",
                Icon = "🩺",
                TargetDeadline = null,
                IsSystemItem = true
            },
            new DailyCheckupItem
            {
                Title = "Treino realizado",
                Icon = "🏋️",
                TargetDeadline = null,
                IsSystemItem = true
            }
        ];
    }

    private void SeedDefaultTrackers()
    {
        Trackers =
        [
            new DoseTracker
            {
                Name = "Água",
                Unit = "ml",
                DoseAmount = 500,
                TargetAmount = 2000,
                Icon = "💧",
                AccentColor = "#06b6d4",
                ResetDaily = true,
                IsSystemDefault = true,
                ReminderEnabled = true,
                ReminderIntervalMinutes = 150,
                ReminderStartHour = 9,
                ReminderEndHour = 20
            }
        ];
    }

    public async Task HandleDayRolloverAsync(DateOnly previousDate, DateOnly currentDate)
    {
        var activeHabits = Habits.Where(h => !h.IsArchived).ToList();
        var completedHabitsCount = activeHabits.Count(h => h.IsCompleted);
        var totalHabitsCount = activeHabits.Count;

        var activeTrackers = Trackers.Where(t => !t.IsArchived && t.ResetDaily).ToList();
        var allTrackersDone = activeTrackers.Count == 0 || activeTrackers.All(t => t.IsTargetReached);
        var allGoalsCompleted = totalHabitsCount > 0 && completedHabitsCount == totalHabitsCount && allTrackersDone;

        // Salvar resumo do dia anterior no histórico
        var existing = History.FirstOrDefault(h => h.Date == previousDate);
        if (existing != null)
        {
            existing.CompletedItemsCount = completedHabitsCount;
            existing.TotalItemsCount = totalHabitsCount;
            existing.AllGoalsCompleted = allGoalsCompleted;
        }
        else
        {
            History.Add(new DayRecord
            {
                Date = previousDate,
                CompletedItemsCount = completedHabitsCount,
                TotalItemsCount = totalHabitsCount,
                AllGoalsCompleted = allGoalsCompleted,
                LoggedAt = DateTime.UtcNow
            });
        }

        // Resetar hábitos diários
        foreach (var habit in Habits)
        {
            habit.IsCompleted = false;
            habit.CompletedAt = null;
        }

        // Resetar trackers com ResetDaily ativo
        foreach (var tracker in Trackers.Where(t => t.ResetDaily))
        {
            tracker.DosesConsumed = 0;
            tracker.LastConsumedAt = null;
            tracker.LastReminderSentAt = null;
        }

        LastActiveDate = currentDate;
        await _storage.SetItemAsync("lifehacks_last_date", currentDate.ToString("O"));

        await SaveHabitsAsync();
        await SaveTrackersAsync();
        await SaveHistoryAsync();
        CalculateStreak();
    }

    public void CalculateStreak()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var streak = 0;

        var activeHabits = Habits.Where(h => !h.IsArchived).ToList();
        var activeTrackers = Trackers.Where(t => !t.IsArchived && t.ResetDaily).ToList();
        var todayDone = activeHabits.Count > 0 &&
                        activeHabits.All(h => h.IsCompleted) &&
                        (activeTrackers.Count == 0 || activeTrackers.All(t => t.IsTargetReached));

        if (todayDone)
        {
            streak++;
        }

        var checkDate = today.AddDays(-1);
        while (true)
        {
            var record = History.FirstOrDefault(r => r.Date == checkDate);
            if (record != null && (record.AllGoalsCompleted || (record.TotalItemsCount > 0 && record.CompletedItemsCount >= record.TotalItemsCount)))
            {
                streak++;
                checkDate = checkDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        CurrentStreak = streak;
    }

    // --- MÉTODOS DE HÁBITOS ---
    public async Task ToggleHabitAsync(string id)
    {
        var habit = Habits.FirstOrDefault(h => h.Id == id);
        if (habit == null) return;

        habit.IsCompleted = !habit.IsCompleted;
        habit.CompletedAt = habit.IsCompleted ? DateTime.UtcNow : null;

        await TriggerHapticAsync(15);
        await SaveHabitsAsync();
        await CheckVictoryCelebrationAsync();
        CalculateStreak();
        NotifyStateChanged();
    }

    public async Task AddHabitAsync(string title, string? icon, TimeOnly? deadline)
    {
        if (string.IsNullOrWhiteSpace(title)) return;

        Habits.Add(new DailyCheckupItem
        {
            Title = title.Trim(),
            Icon = string.IsNullOrWhiteSpace(icon) ? "⚡" : icon.Trim(),
            TargetDeadline = deadline,
            IsSystemItem = false,
            IsCompleted = false
        });

        await TriggerHapticAsync(25);
        await SaveHabitsAsync();
        NotifyStateChanged();
    }

    public async Task DeleteHabitAsync(string id)
    {
        var habit = Habits.FirstOrDefault(h => h.Id == id);
        if (habit != null)
        {
            Habits.Remove(habit);
            await SaveHabitsAsync();
            NotifyStateChanged();
        }
    }

    // --- MÉTODOS DE TRACKERS QUANTITATIVOS (Água, Chá, Café, Refri, etc.) ---
    public async Task AddTrackerAsync(DoseTracker tracker)
    {
        Trackers.Add(tracker);
        await TriggerHapticAsync(25);
        await SaveTrackersAsync();
        NotifyStateChanged();
    }

    public async Task UpdateTrackerAsync(DoseTracker updated)
    {
        var idx = Trackers.FindIndex(t => t.Id == updated.Id);
        if (idx >= 0)
        {
            Trackers[idx] = updated;
            await SaveTrackersAsync();
            NotifyStateChanged();
        }
    }

    public async Task DeleteTrackerAsync(string id)
    {
        var tracker = Trackers.FirstOrDefault(t => t.Id == id);
        if (tracker != null)
        {
            Trackers.Remove(tracker);
            await SaveTrackersAsync();
            NotifyStateChanged();
        }
    }

    public async Task IncrementTrackerDoseAsync(string id)
    {
        var tracker = Trackers.FirstOrDefault(t => t.Id == id);
        if (tracker == null) return;

        tracker.DosesConsumed++;
        tracker.LastConsumedAt = DateTime.UtcNow;

        await TriggerHapticAsync(20);
        await SaveTrackersAsync();
        await CheckVictoryCelebrationAsync();
        CalculateStreak();
        NotifyStateChanged();
    }

    public async Task DecrementTrackerDoseAsync(string id)
    {
        var tracker = Trackers.FirstOrDefault(t => t.Id == id);
        if (tracker == null || tracker.DosesConsumed <= 0) return;

        tracker.DosesConsumed--;
        await TriggerHapticAsync(15);
        await SaveTrackersAsync();
        CalculateStreak();
        NotifyStateChanged();
    }

    public async Task SetTrackerDosesAsync(string id, int doses)
    {
        var tracker = Trackers.FirstOrDefault(t => t.Id == id);
        if (tracker == null) return;

        tracker.DosesConsumed = Math.Max(0, doses);
        tracker.LastConsumedAt = DateTime.UtcNow;

        await TriggerHapticAsync(15);
        await SaveTrackersAsync();
        await CheckVictoryCelebrationAsync();
        CalculateStreak();
        NotifyStateChanged();
    }

    // --- MÉTODOS DE COMPRAS & DIÁRIO DE HARDWARE/WISHLIST ---
    public async Task<PurchasePlanItem> AddPurchaseItemAsync(string title, string category, decimal? price, string priority, string targetCycle)
    {
        var item = new PurchasePlanItem
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Novo Item" : title.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "Geral" : category.Trim(),
            EstimatedPrice = price,
            Priority = string.IsNullOrWhiteSpace(priority) ? "Alta" : priority,
            TargetCycle = string.IsNullOrWhiteSpace(targetCycle) ? "Virada do Cartão" : targetCycle
        };

        Purchases.Insert(0, item);
        await TriggerHapticAsync(20);
        await SavePurchasesAsync();
        NotifyStateChanged();
        return item;
    }

    public async Task UpdatePurchaseItemAsync(PurchasePlanItem item)
    {
        var idx = Purchases.FindIndex(p => p.Id == item.Id);
        if (idx >= 0)
        {
            Purchases[idx] = item;
            await SavePurchasesAsync();
            NotifyStateChanged();
        }
    }

    public async Task TogglePurchasePurchasedAsync(string id)
    {
        var item = Purchases.FirstOrDefault(p => p.Id == id);
        if (item == null) return;

        item.IsPurchased = !item.IsPurchased;
        item.PurchasedAt = item.IsPurchased ? DateTime.UtcNow : null;

        await TriggerHapticAsync(20);
        await SavePurchasesAsync();
        NotifyStateChanged();
    }

    public async Task DeletePurchaseItemAsync(string id)
    {
        var item = Purchases.FirstOrDefault(p => p.Id == id);
        if (item != null)
        {
            Purchases.Remove(item);
            await SavePurchasesAsync();
            NotifyStateChanged();
        }
    }

    public async Task AddDiaryNoteAsync(string itemId, string note)
    {
        if (string.IsNullOrWhiteSpace(note)) return;

        var item = Purchases.FirstOrDefault(p => p.Id == itemId);
        if (item == null) return;

        item.DiaryNotes.Insert(0, new PurchaseDiaryEntry
        {
            Note = note.Trim(),
            Timestamp = DateTime.UtcNow
        });

        await TriggerHapticAsync(15);
        await SavePurchasesAsync();
        NotifyStateChanged();
    }

    public async Task DeleteDiaryNoteAsync(string itemId, string noteId)
    {
        var item = Purchases.FirstOrDefault(p => p.Id == itemId);
        if (item == null) return;

        item.DiaryNotes.RemoveAll(n => n.Id == noteId);
        await SavePurchasesAsync();
        NotifyStateChanged();
    }

    public async Task AddReviewLinkAsync(string itemId, string title, string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        var item = Purchases.FirstOrDefault(p => p.Id == itemId);
        if (item == null) return;

        item.Links.Add(new PurchaseLink
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Link / Review" : title.Trim(),
            Url = url.Trim()
        });

        await TriggerHapticAsync(15);
        await SavePurchasesAsync();
        NotifyStateChanged();
    }

    public async Task DeleteReviewLinkAsync(string itemId, string linkId)
    {
        var item = Purchases.FirstOrDefault(p => p.Id == itemId);
        if (item == null) return;

        item.Links.RemoveAll(l => l.Id == linkId);
        await SavePurchasesAsync();
        NotifyStateChanged();
    }

    public async Task UpdateSettingsAsync(AppSettings newSettings)
    {
        Settings = newSettings;
        await _storage.SetItemAsync("lifehacks_settings", Settings);
        NotifyStateChanged();
    }

    public async Task ReloadAllAsync()
    {
        IsInitialized = false;
        await InitializeAsync();
    }

    private async Task CheckVictoryCelebrationAsync()
    {
        var activeHabits = Habits.Where(h => !h.IsArchived).ToList();
        var activeTrackers = Trackers.Where(t => !t.IsArchived && t.ResetDaily).ToList();

        var habitsDone = activeHabits.Count > 0 && activeHabits.All(h => h.IsCompleted);
        var trackersDone = activeTrackers.Count == 0 || activeTrackers.All(t => t.IsTargetReached);

        if (habitsDone && trackersDone)
        {
            try
            {
                if (Settings.HapticsEnabled)
                {
                    await _js.InvokeVoidAsync("appInterop.vibrate", new int[] { 40, 60, 40, 60, 100 });
                }
                await _js.InvokeVoidAsync("appInterop.triggerConfetti");
            }
            catch { }
        }
    }

    private async Task SaveHabitsAsync() => await _storage.SetItemAsync("lifehacks_habits", Habits);
    private async Task SaveTrackersAsync() => await _storage.SetItemAsync("lifehacks_trackers", Trackers);
    private async Task SavePurchasesAsync() => await _storage.SetItemAsync("lifehacks_purchases", Purchases);
    private async Task SaveHistoryAsync() => await _storage.SetItemAsync("lifehacks_history", History);

    private async Task TriggerHapticAsync(int ms)
    {
        if (!Settings.HapticsEnabled) return;
        try { await _js.InvokeVoidAsync("appInterop.vibrate", ms); } catch { }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
