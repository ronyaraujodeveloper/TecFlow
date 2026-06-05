namespace TecFlow.Core.Entities
{
    public class Item : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Exemplo de m�trica de ranqueamento
        public decimal PopularityScore { get; set; }
        public int OwnerId { get; set; }
    }
}