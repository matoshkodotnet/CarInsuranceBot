namespace CarInsuranceBot.Data.Entities
{
    public class DocumentType
    {
        public int DocumentTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<Document> Documents { get; set; } = [];
    }
}
