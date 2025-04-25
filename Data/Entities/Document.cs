namespace CarInsuranceBot.Data.Entities
{
    public class Document
    {
        public int DocumentId { get; set; }
        public long ChatId { get; set; }
        public int UserId { get; set; }
        public int DocumentTypeId { get; set; }
        public string FileId { get; set; } = string.Empty;
        public string? ExtractedData { get; set; }
        public DateTime CreatedAt { get; set; }
        public User User { get; set; }
        public DocumentType DocumentType { get; set; }
    }
}
