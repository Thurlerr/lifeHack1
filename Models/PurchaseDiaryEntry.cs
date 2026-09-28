namespace LifeHacks.Models;

public class PurchaseDiaryEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Note { get; set; } = string.Empty;
}
