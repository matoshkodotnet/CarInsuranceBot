using CarInsuranceBot.Data;
using CarInsuranceBot.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Document = CarInsuranceBot.Data.Entities.Document;
using User = CarInsuranceBot.Data.Entities.User;

namespace CarInsuranceBot.Services
{
    public class BotService : IBotService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IMindeeService _mindeeService;
        private readonly BotDbContext _dbContext;

        public BotService(ITelegramBotClient botClient, IMindeeService mindeeService, BotDbContext dbContext)
        {
            _botClient = botClient;
            _dbContext = dbContext;
            _mindeeService = mindeeService;
        }

        public async Task HandleUpdateAsync(Update update)
        {
            if (update.Message == null) return;

            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text ?? "";

            try
            {
                var user = await _dbContext.Users
                    .Include(u => u.CurrentStep)
                    .ThenInclude(us => us.Step)
                    .Include(u => u.Documents)
                    .FirstOrDefaultAsync(u => u.ChatId == chatId);

                if (user == null)
                {
                    user = new User { ChatId = chatId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
                    _dbContext.Users.Add(user);
                    await _dbContext.SaveChangesAsync();
                }

                if (messageText.Equals("/start", StringComparison.OrdinalIgnoreCase))
                {
                    if (user.CurrentStep == null)
                    {
                        user.CurrentStep = new UserStep
                        {
                            ChatId = chatId,
                            UserId = user.UserId,
                            StepId = 1,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _dbContext.UserSteps.Add(user.CurrentStep);
                    }
                    else
                    {
                        user.CurrentStep.StepId = 1;
                        user.CurrentStep.UpdatedAt = DateTime.UtcNow;
                    }

                    var existingDocs = _dbContext.Documents.Where(d => d.ChatId == chatId);
                    _dbContext.Documents.RemoveRange(existingDocs);

                    user.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();

                    await _botClient.SendMessage(chatId,
                        "Привіт! Я бот для автострахування. Я допоможу вам придбати страховий поліс. " +
                        "Будь ласка, надішліть фото вашого паспорта.");
                }
                else if (user.CurrentStep?.StepId == 1 && (update.Message.Photo != null || update.Message.Document != null))
                {
                    var existingPassport = user.Documents?.FirstOrDefault(d => d.DocumentTypeId == 1);
                    if (existingPassport != null)
                    {
                        _dbContext.Documents.Remove(existingPassport);
                        await _dbContext.SaveChangesAsync();
                    }

                    string fileId;
                    if (update.Message.Photo != null)
                    {
                        var photo = update.Message.Photo.Last();
                        fileId = photo.FileId;
                    }
                    else
                    {
                        fileId = update.Message.Document.FileId;
                    }

                    var document = new Document
                    {
                        ChatId = chatId,
                        UserId = user.UserId,
                        DocumentTypeId = 1,
                        FileId = fileId,
                        ExtractedData = null,
                        CreatedAt = DateTime.UtcNow,
                        User = user
                    };

                    _dbContext.Documents.Add(document);
                    await _dbContext.SaveChangesAsync();

                    var passportDoc = await _dbContext.Documents
                        .FirstOrDefaultAsync(d => d.ChatId == chatId && d.DocumentTypeId == 1);

                    if (passportDoc == null)
                    {
                        await _botClient.SendMessage(chatId,
                            "Помилка: не вдалося зберегти документ. Будь ласка, почніть заново, надіславши /start.");
                        return;
                    }

                    var passportFile = await _botClient.GetFile(passportDoc.FileId);
                    var passportPath = Path.Combine(Path.GetTempPath(), $"{passportDoc.DocumentId}_passport.jpg");

                    try
                    {
                        using (var passportStream = new FileStream(passportPath, FileMode.Create))
                        {
                            await _botClient.DownloadFile(passportFile.FilePath, passportStream);
                        }

                        passportDoc.ExtractedData = await _mindeeService.ExtractPassportDataAsync(passportPath);
                        await _dbContext.SaveChangesAsync();

                        user.CurrentStep.StepId = 2;
                        user.CurrentStep.UpdatedAt = DateTime.UtcNow;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync();

                        await _botClient.SendMessage(chatId,
                            "Фото паспорта отримано! Будь ласка, надішліть фото документа вашого транспортного засобу.");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to extract data from the passport. Please ensure that this is a photo of a passport."))
                    {
                        _dbContext.Documents.Remove(passportDoc);
                        await _dbContext.SaveChangesAsync();

                        await _botClient.SendMessage(chatId,
                            "Помилка: не вдалося витягнути дані з паспорта. Переконайтеся, що це фото паспорта, і надішліть його ще раз.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"BotService error on StepId 1: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        await _botClient.SendMessage(chatId,
                            $"Помилка обробки паспорта: {ex.Message}\nБудь ласка, спробуйте ще раз або використайте /start.");
                    }
                    finally
                    {
                        if (File.Exists(passportPath)) File.Delete(passportPath);
                    }
                }
                else if (user.CurrentStep?.StepId == 2 && (update.Message.Photo != null || update.Message.Document != null))
                {
                    var existingVehicleDoc = user.Documents?.FirstOrDefault(d => d.DocumentTypeId == 2);
                    if (existingVehicleDoc != null)
                    {
                        _dbContext.Documents.Remove(existingVehicleDoc);
                        await _dbContext.SaveChangesAsync();
                    }

                    string fileId;
                    if (update.Message.Photo != null)
                    {
                        var photo = update.Message.Photo.Last();
                        fileId = photo.FileId;
                    }
                    else
                    {
                        fileId = update.Message.Document.FileId;
                    }

                    var document = new Document
                    {
                        ChatId = chatId,
                        UserId = user.UserId,
                        DocumentTypeId = 2,
                        FileId = fileId,
                        ExtractedData = null,
                        CreatedAt = DateTime.UtcNow,
                        User = user
                    };

                    _dbContext.Documents.Add(document);
                    await _dbContext.SaveChangesAsync();

                    var vehicleDoc = await _dbContext.Documents
                        .FirstOrDefaultAsync(d => d.ChatId == chatId && d.DocumentTypeId == 2);

                    if (vehicleDoc == null)
                    {
                        await _botClient.SendMessage(chatId,
                            "Помилка: не вдалося зберегти документ ТЗ. Будь ласка, почніть заново, надіславши /start.");
                        return;
                    }

                    user = await _dbContext.Users
                        .Include(u => u.Documents)
                        .FirstOrDefaultAsync(u => u.ChatId == chatId);

                    var passportDoc = user?.Documents?.FirstOrDefault(d => d.DocumentTypeId == 1);
                    if (passportDoc == null)
                    {
                        await _botClient.SendMessage(chatId,
                            "Помилка: не вдалося знайти документ паспорта. Будь ласка, почніть заново, надіславши /start.");
                        return;
                    }

                    var vehicleFile = await _botClient.GetFile(vehicleDoc.FileId);
                    var vehiclePath = Path.Combine(Path.GetTempPath(), $"{vehicleDoc.DocumentId}_vehicle.jpg");

                    try
                    {
                        using (var vehicleStream = new FileStream(vehiclePath, FileMode.Create))
                        {
                            await _botClient.DownloadFile(vehicleFile.FilePath, vehicleStream);
                        }

                        vehicleDoc.ExtractedData = await _mindeeService.ExtractVehicleDocDataAsync(vehiclePath);
                        await _dbContext.SaveChangesAsync();

                        user.CurrentStep.StepId = 3;
                        user.CurrentStep.UpdatedAt = DateTime.UtcNow;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync();

                        await _botClient.SendMessage(chatId,
                            "Дані витягнуто:\n" +
                            $"Паспорт:\n{passportDoc.ExtractedData}\n" +
                            $"Документ ТЗ: {vehicleDoc.ExtractedData}\n" +
                            "Чи підтверджуєте ви ці дані? Надішліть 'Так' або 'Ні'.");
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Не вдалося витягнути дані з документа ТЗ"))
                    {
                        _dbContext.Documents.Remove(vehicleDoc);
                        await _dbContext.SaveChangesAsync();

                        await _botClient.SendMessage(chatId,
                            "Помилка: не вдалося витягнути дані з документа ТЗ. Переконайтеся, що це фото документа транспортного засобу, і надішліть його ще раз.");
                        Console.WriteLine($"BotService error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"BotService error on StepId 2: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        await _botClient.SendMessage(chatId,
                            $"Помилка обробки документа ТЗ: {ex.Message}\nБудь ласка, спробуйте ще раз або використайте /start.");
                    }
                    finally
                    {
                        if (File.Exists(vehiclePath)) File.Delete(vehiclePath);
                    }
                }
                else if (user.CurrentStep?.StepId == 3)
                {
                    if (messageText.Equals("Так", StringComparison.OrdinalIgnoreCase))
                    {
                        user.CurrentStep.StepId = 4;
                        user.CurrentStep.UpdatedAt = DateTime.UtcNow;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync();

                        await _botClient.SendMessage(chatId,
                            "Чудово! Вартість страхового поліса становить 100 USD. Чи згодні ви з ціною? Надішліть 'Так' або 'Ні'.");
                    }
                    else if (messageText.Equals("Ні", StringComparison.OrdinalIgnoreCase))
                    {
                        var existingDocs = _dbContext.Documents.Where(d => d.ChatId == chatId);
                        _dbContext.Documents.RemoveRange(existingDocs);

                        user.CurrentStep.StepId = 1;
                        user.CurrentStep.UpdatedAt = DateTime.UtcNow;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync();

                        await _botClient.SendMessage(chatId,
                            "Добре, давайте спробуємо ще раз. Будь ласка, надішліть фото вашого паспорта.");
                    }
                    else
                    {
                        await _botClient.SendMessage(chatId,
                            "Будь ласка, надішліть 'Так' або 'Ні' для підтвердження даних.");
                    }
                }
                else if (user.CurrentStep?.StepId == 4)
                {
                    if (messageText.Equals("Так", StringComparison.OrdinalIgnoreCase))
                    {
                        user = await _dbContext.Users
                            .Include(u => u.Documents)
                            .FirstOrDefaultAsync(u => u.ChatId == chatId);

                        var passportDoc = user?.Documents?.FirstOrDefault(d => d.DocumentTypeId == 1);
                        var vehicleDoc = user?.Documents?.FirstOrDefault(d => d.DocumentTypeId == 2);

                        if (passportDoc == null || vehicleDoc == null || string.IsNullOrEmpty(passportDoc.ExtractedData) || string.IsNullOrEmpty(vehicleDoc.ExtractedData))
                        {
                            await _botClient.SendMessage(chatId,
                                "Помилка: не вдалося знайти витягнуті дані. Будь ласка, почніть заново, надіславши /start.");
                            return;
                        }

                        string givenName = "Невідомо";
                        string surname = "Невідомо";
                        string expiryDate = "Невідомо";
                        var passportLines = passportDoc.ExtractedData.Split('\n');
                        foreach (var line in passportLines)
                        {
                            if (line.StartsWith("Ім'я: ")) givenName = line.Substring("Ім'я: ".Length);
                            if (line.StartsWith("Прізвище: ")) surname = line.Substring("Прізвище: ".Length);
                            if (line.StartsWith("Дата закінчення: ")) expiryDate = line.Substring("Дата закінчення: ".Length);
                        }

                        if (givenName == "Невідомо" || surname == "Невідомо")
                        {
                            await _botClient.SendMessage(chatId,
                                "Помилка: не вдалося витягнути ім'я або прізвище з паспорта. Будь ласка, почніть заново, надіславши /start.");
                            return;
                        }

                        string vin = "Невідомо";
                        string model = "Невідомо";
                        var vehicleLines = vehicleDoc.ExtractedData.Split(new[] { ", " }, StringSplitOptions.None);
                        foreach (var line in vehicleLines)
                        {
                            if (line.StartsWith("VIN: ")) vin = line.Substring("VIN: ".Length);
                            if (line.StartsWith("Модель: ")) model = line.Substring("Модель: ".Length);
                        }

                        string policyExpiryDate = "01.01.2026";
                        if (DateTime.TryParse(expiryDate, out var passportExpiryDate))
                        {
                            var defaultExpiryDate = new DateTime(2026, 1, 1);
                            if (passportExpiryDate < defaultExpiryDate)
                            {
                                policyExpiryDate = passportExpiryDate.ToString("dd.MM.yyyy");
                            }
                        }

                        string policyNumber = $"INS-{DateTime.UtcNow.Year}-{chatId}";

                        var policy = "Страховий поліс\n" +
                                     $"Номер: {policyNumber}\n" +
                                     $"Клієнт: {givenName} {surname}\n" +
                                     $"Транспортний засіб: {model}\n" +
                                     $"Термін дії: {policyExpiryDate}\n" +
                                     "Вартість: 100 USD";

                        user.CurrentStep.StepId = 5;
                        user.CurrentStep.UpdatedAt = DateTime.UtcNow;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _dbContext.SaveChangesAsync();

                        await _botClient.SendMessage(chatId,
                            "Вітаємо! Ваш страховий поліс оформлено:\n" + policy +
                            "\nВикористайте /start, щоб оформити новий поліс.");
                    }
                    else if (messageText.Equals("Ні", StringComparison.OrdinalIgnoreCase))
                    {
                        await _botClient.SendMessage(chatId,
                            "Вибачте, але 100 USD — єдина доступна ціна. Чи бажаєте продовжити? Надішліть 'Так' або 'Ні'.");
                    }
                    else
                    {
                        await _botClient.SendMessage(chatId,
                            "Будь ласка, надішліть 'Так' або 'Ні' для підтвердження ціни.");
                    }
                }
                else
                {
                    await _botClient.SendMessage(chatId,
                        "Будь ласка, використайте /start для початку або надішліть потрібне фото/відповідь.");
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendMessage(chatId,
                    "Сталася помилка. Спробуйте ще раз або використайте /start.");
                Console.WriteLine($"BotService error: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }
    }
}

