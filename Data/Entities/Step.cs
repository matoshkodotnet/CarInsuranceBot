namespace CarInsuranceBot.Data.Entities
{
    public class Step
    {
        public int StepId { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<UserStep> UserSteps { get; set; } = [];
    }
}
