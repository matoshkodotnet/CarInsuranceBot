namespace CarInsuranceBot.Data.Entities
{
    public class UserStep
    {
        public int UserStepId { get; set; }
        public long ChatId { get; set; }
        public int UserId { get; set; }
        public int StepId { get; set; }
        public DateTime UpdatedAt { get; set; }
        public User User { get; set; }
        public Step Step { get; set; }
    }
}
