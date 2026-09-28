namespace LifeHacks.Models;

public class DoseTracker
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Água";
    public string Unit { get; set; } = "ml"; // ml, xícaras, latas, copos, doses, páginas
    public int DoseAmount { get; set; } = 500;
    public int TargetAmount { get; set; } = 2000;
    public int DosesConsumed { get; set; } = 0;
    public string Icon { get; set; } = "💧";
    public string AccentColor { get; set; } = "#06b6d4"; // Cyan, Emerald, Amber, Rose, Purple
    public bool ResetDaily { get; set; } = true;
    public bool IsArchived { get; set; } = false;
    public bool IsSystemDefault { get; set; } = false;
    public DateTime? LastConsumedAt { get; set; }

    // Configurações de lembrete periódicos
    public bool ReminderEnabled { get; set; } = true;
    public int ReminderIntervalMinutes { get; set; } = 150; // ex: 2h30
    public int ReminderStartHour { get; set; } = 9;
    public int ReminderEndHour { get; set; } = 20;
    public DateTime? LastReminderSentAt { get; set; }

    public int CurrentAmount => DosesConsumed * DoseAmount;
    public int TotalDoses => TargetAmount <= 0 ? 0 : (int)Math.Ceiling((double)TargetAmount / DoseAmount);
    public int Percentage => TargetAmount <= 0 ? 0 : Math.Clamp((int)((double)CurrentAmount / TargetAmount * 100), 0, 100);
    public bool IsTargetReached => CurrentAmount >= TargetAmount;
}
