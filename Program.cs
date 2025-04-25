using Microsoft.EntityFrameworkCore;
using CarInsuranceBot.Data;
using CarInsuranceBot.Services;
using CarInsuranceBot.HostedServices;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(configuration["TelegramBot:Token"]));
services.AddScoped<IBotService, BotService>();
services.AddHostedService<PollingService>();
services.AddHttpClient();
services.AddScoped<IMindeeService>(sp =>
    new MindeeService(configuration["Mindee:ApiKey"]));

builder.Services.AddDbContext<BotDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

builder.WebHost.UseUrls("http://0.0.0.0:7170");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    dbContext.Database.Migrate();
}

app.Run();