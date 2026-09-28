namespace LifeHacks.Models;

public class AppSettings
{
    public bool NotificationsEnabled { get; set; } = true;
    public bool HapticsEnabled { get; set; } = true;
    public int CreatineDeadlineHour { get; set; } = 10;
    public int CreatineDeadlineMinute { get; set; } = 0;
    public DateTime? LastCreatineNotificationTime { get; set; }
}
