namespace LifeHacks.Models;

public class DayRecord
{
    public DateOnly Date { get; set; }
    public int CompletedItemsCount { get; set; }
    public int TotalItemsCount { get; set; }
    public bool AllGoalsCompleted { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;

    public int CompletionPercentage
    {
        get
        {
            if (TotalItemsCount == 0) return 0;
            return (int)((double)CompletedItemsCount / TotalItemsCount * 100);
        }
    }
}
