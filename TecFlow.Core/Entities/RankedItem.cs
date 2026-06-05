namespace TecFlow.Core.Entities
{
    public class RankedItem
    {
        public int ItemId { get; set; }
        public decimal Score { get; set; }
        public string? Name { get; set; } // Opcional, para identifica��o
        public int OwnerId { get; set; }
        public UserAccount? Owner { get; set; }
    }
}