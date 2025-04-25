using Mindee;
using Mindee.Input;
using Mindee.Http;
using Mindee.Product.Generated;
using Mindee.Parsing.Generated;
using Mindee.Parsing;

namespace CarInsuranceBot.Services
{
    public class MindeeService : IMindeeService
    {
        private readonly MindeeClient _mindeeClient;
        private readonly string _apiKey;

        public MindeeService(string apiKey)
        {
            _apiKey = apiKey;
            _mindeeClient = new MindeeClient(_apiKey);
        }

        public async Task<string> ExtractPassportDataAsync(string filePath)
        {
            try
            {
                var inputSource = new LocalInputSource(filePath);

                CustomEndpoint endpoint = new CustomEndpoint(
                    endpointName: "ukrainianpassport",
                    accountName: "komatoshko1123",
                    version: "1"
                );

                var response = await _mindeeClient.EnqueueAndParseAsync<GeneratedV1>(inputSource, endpoint);

                if (response.Document == null || response.Document.Inference == null || response.Document.Inference.Prediction == null)
                {
                    throw new InvalidOperationException("Mindee API did not return valid prediction data.");
                }

                var prediction = response.Document.Inference.Prediction;

                var fields = prediction.Fields;

                var fieldLabels = new Dictionary<string, string>
                {
                    { "given_name", "Ім'я" },
                    { "surname", "Прізвище" },
                    { "date_of_birth", "Дата народження" },
                    { "document_number", "Номер документа" },
                    { "country", "Країна" },
                    { "expiry_date", "Дата закінчення" },
                    { "gender", "Стать" }
                };

                var extractedFields = fieldLabels.ToDictionary(
                    kvp => kvp.Value,
                    kvp => ExtractFieldValue(fields, kvp.Key)
                );

                if (extractedFields.Any(kvp => string.IsNullOrWhiteSpace(kvp.Value)))
                {
                    throw new InvalidOperationException("Failed to extract data from the passport. Please ensure that this is a photo of a passport.");
                }

                var formatted = extractedFields.Select(kvp => $"{kvp.Key}: {kvp.Value}");
                return string.Join("\n", formatted);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<string> ExtractVehicleDocDataAsync(string filePath)
        {
            try
            {
                await Task.Delay(1000);
                return "VIN: 1HGCM82633A004352, Модель: Toyota Camry 2020";
            }
            catch (Exception)
            {
                throw;
            }
        }

        string ExtractFieldValue(Dictionary<string, GeneratedFeature> fields, string key)
        {
            if (fields.TryGetValue(key, out var feature) && feature?.Count > 0)
            {
                var rawValue = feature[0]?.ToString();

                if (rawValue?.StartsWith(":value: ") == true)
                {
                    var value = rawValue.Substring(":value: ".Length);
                    return string.IsNullOrWhiteSpace(value) ? "" : value;
                }

                return rawValue ?? "";
            }

            return "";
        }
    }
}
