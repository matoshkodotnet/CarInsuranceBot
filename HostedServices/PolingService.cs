using CarInsuranceBot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CarInsuranceBot.HostedServices
{
    public class PollingService : IHostedService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PollingService> _logger;
        private CancellationTokenSource _cts;

        public PollingService(ITelegramBotClient botClient, IServiceProvider serviceProvider, ILogger<PollingService> logger)
        {
            _botClient = botClient;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                _cts.Token
            );

            _logger.LogInformation("Bot started polling for updates.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            _logger.LogInformation("Bot stopped polling for updates.");
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var botService = scope.ServiceProvider.GetRequiredService<IBotService>();
            await botService.HandleUpdateAsync(update);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "Error occurred while polling updates.");
            return Task.CompletedTask;
        }
    }
}