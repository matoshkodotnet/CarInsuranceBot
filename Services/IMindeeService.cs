namespace CarInsuranceBot.Services
{
    public interface IMindeeService
    {
        public Task<string> ExtractPassportDataAsync(string filePath);
        public Task<string> ExtractVehicleDocDataAsync(string filePath);
    }
}
