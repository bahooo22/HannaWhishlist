using Calabonga.AspNetCore.AppDefinitions;
using Telegram.Bot;
using TelegramBotService.Web.Application.Services;

namespace TelegramBotService.Web.Definitions.Bot;

public class BotDefinition : AppDefinition
{
    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        // сервис для получения токена
        builder.Services.AddHttpClient<AuthTokenService>();

        // HttpClient для Wishlist API
        builder.Services.AddHttpClient("WishlistApi", (sp, client) =>
        {
            client.BaseAddress = new Uri(builder.Configuration["WishlistApi:BaseUrl"]!);
        });

        builder.Configuration.AddJsonFile("appsettings.secret.json", optional: true, reloadOnChange: true);

        // Telegram Bot Client
        builder.Services.AddSingleton<ITelegramBotClient>(
            new TelegramBotClient(builder.Configuration["Telegram:BotToken"]!));

        Console.WriteLine($"Проверочный токен: {builder.Configuration["Telegram:BotToken"]}");

        // Регистрируем фоновый сервис
        builder.Services.AddHostedService<BotBackgroundService>();
    }
}
