namespace LifeHacks.Models;

public class PurchasePlanItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Hardware"; // Hardware, Eletrônicos, Casa, Setup, Outros
    public decimal? EstimatedPrice { get; set; }
    public string Priority { get; set; } = "Alta"; // Essencial, Alta, Média, Baixa
    public string TargetCycle { get; set; } = "Virada do Cartão"; // Virada do Cartão, Próxima Fatura, Futuro
    public bool IsPurchased { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PurchasedAt { get; set; }

    // Diário cronológico de pesquisa (ex: comparações, impressões de reviews, preços vistos)
    public List<PurchaseDiaryEntry> DiaryNotes { get; set; } = [];

    // Links diretos para reviews, comparativos e anúncios
    public List<PurchaseLink> Links { get; set; } = [];
}
