using Telegram.Bot.Types;

namespace CarInsuranceBot.Services
{
    public interface IBotService
    {
        public Task HandleUpdateAsync(Update update);
    }
}
