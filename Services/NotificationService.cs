using LifeHacks.Models;
using Microsoft.JSInterop;

namespace LifeHacks.Services;

public class NotificationService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly RoutineManagerService _routineManager;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _timerTask;

    public bool HasPermission { get; private set; }

    public NotificationService(IJSRuntime js, RoutineManagerService routineManager)
    {
        _js = js;
        _routineManager = routineManager;
    }

    public async Task StartAsync()
    {
        await CheckPermissionAsync();

        if (_timerTask != null) return;

        _cts = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        _timerTask = RunPeriodicCheckLoopAsync(_cts.Token);
    }

    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
            var res = await _js.InvokeAsync<string>("appInterop.requestNotificationPermission");
            HasPermission = res == "granted";
            _routineManager.Settings.NotificationsEnabled = HasPermission;
            await _routineManager.UpdateSettingsAsync(_routineManager.Settings);
            return HasPermission;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Error requesting permission: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CheckPermissionAsync()
    {
        try
        {
            HasPermission = await _js.InvokeAsync<bool>("appInterop.hasNotificationPermission");
            return HasPermission;
        }
        catch
        {
            HasPermission = false;
            return false;
        }
    }

    private async Task RunPeriodicCheckLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _timer != null && await _timer.WaitForNextTickAsync(ct))
        {
            try
            {
                await EvaluateRemindersAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationService] Check error: {ex.Message}");
            }
        }
    }

    public async Task EvaluateRemindersAsync()
    {
        if (!_routineManager.IsInitialized || !_routineManager.Settings.NotificationsEnabled)
        {
            return;
        }

        var now = DateTime.Now;
        var currentTime = TimeOnly.FromDateTime(now);
        var settings = _routineManager.Settings;

        // 1. Regra da Creatina e itens com horário limite
        var creatina = _routineManager.Habits.FirstOrDefault(h => h.Title.Contains("Creatina", StringComparison.OrdinalIgnoreCase));
        if (creatina != null && !creatina.IsCompleted && creatina.TargetDeadline.HasValue)
        {
            if (currentTime > creatina.TargetDeadline.Value)
            {
                var shouldNotify = settings.LastCreatineNotificationTime == null ||
                                   settings.LastCreatineNotificationTime.Value.Date != now.Date;

                if (shouldNotify)
                {
                    await SendNotificationAsync(
                        "⚠️ Creatina Atrasada!",
                        "Já passou das 10:00! Não se esqueça de tomar sua creatina hoje.",
                        "creatine-delayed"
                    );

                    settings.LastCreatineNotificationTime = now;
                    await _routineManager.UpdateSettingsAsync(settings);
                }
            }
        }

        // 2. Regra para cada DoseTracker com lembrete ativado (Água, Chá, etc.)
        foreach (var tracker in _routineManager.Trackers.Where(t => !t.IsArchived && t.ReminderEnabled && !t.IsTargetReached))
        {
            if (currentTime.Hour >= tracker.ReminderStartHour && currentTime.Hour < tracker.ReminderEndHour)
            {
                var interval = TimeSpan.FromMinutes(Math.Max(30, tracker.ReminderIntervalMinutes));
                var lastNotify = tracker.LastReminderSentAt;

                var isIntervalElapsed = lastNotify == null || (now - lastNotify.Value) >= interval;

                if (isIntervalElapsed)
                {
                    await SendNotificationAsync(
                        $"{tracker.Icon} Hora de {tracker.Name}!",
                        $"Progresso atual: {tracker.CurrentAmount} / {tracker.TargetAmount} {tracker.Unit} ({tracker.DosesConsumed}/{tracker.TotalDoses} doses).",
                        $"tracker-reminder-{tracker.Id}"
                    );

                    tracker.LastReminderSentAt = now;
                    await _routineManager.UpdateTrackerAsync(tracker);
                }
            }
        }
    }

    public async Task SendNotificationAsync(string title, string body, string tag)
    {
        try
        {
            await _js.InvokeVoidAsync("appInterop.sendNotification", title, body, tag);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NotificationService] Send failed: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        _timer?.Dispose();

        if (_timerTask != null)
        {
            try
            {
                await _timerTask;
            }
            catch (OperationCanceledException) { }
        }
    }
}
