namespace CarInsuranceBot.Data.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public long ChatId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public UserStep CurrentStep { get; set; }
        public List<Document> Documents { get; set; } = [];
    }
}
