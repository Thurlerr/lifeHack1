namespace LifeHacks.Models;

public class DailyCheckupItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = "⚡";
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeOnly? TargetDeadline { get; set; }
    public bool IsSystemItem { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDelayed(TimeOnly currentTime)
    {
        if (IsCompleted || !TargetDeadline.HasValue)
        {
            return false;
        }

        return currentTime > TargetDeadline.Value;
    }
}
