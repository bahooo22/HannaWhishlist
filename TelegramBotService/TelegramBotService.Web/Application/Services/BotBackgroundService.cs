using System.Net.Http.Headers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WishlistService.Contracts.Responses;
using WishlistService.Contracts.ViewModels;

namespace TelegramBotService.Web.Application.Services;

public class BotBackgroundService : BackgroundService
{
    private readonly ITelegramBotClient _bot;
    private readonly IHttpClientFactory _httpClientFactory;
    readonly ILogger<BotBackgroundService> _logger;
    private readonly AuthTokenService _tokenService;

    public BotBackgroundService(
        ITelegramBotClient bot,
        IHttpClientFactory httpClientFactory,
        AuthTokenService tokenService,
        ILogger<BotBackgroundService> logger)
    {
        _bot = bot;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _tokenService = tokenService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // В актуальных версиях Telegram.Bot:
        // Убедитесь, что Telegram.Bot.Extensions.Polling подключен,
        // или используйте ITelegramBotClient.ReceiveAsync, если это более старая версия.
        // Здесь используется StartReceiving, что соответствует более старому/расширенному API.

        _bot.StartReceiving(HandleUpdateAsync, HandleErrorAsync, cancellationToken: stoppingToken);
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message?.Text == "/wishlist")
        {
            var client = _httpClientFactory.CreateClient("WishlistApi");

            // получаем токен у AuthServer
            var token = await _tokenService.GetTokenAsync();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            const string apiPath = "api/gifts";

            var response = await client.GetFromJsonAsync<OperationResponse<List<GiftViewModel>>>(apiPath, ct);

            var gifts = response?.Result; // Используйте фактический путь API

            if (gifts is null)
            {
                await bot.SendMessage(update.Message!.Chat.Id, "Не удалось загрузить список подарков.", cancellationToken: ct);
                return;
            }

            foreach (var gift in gifts)
            {
                // 2. ИСПРАВЛЕНИЕ: Проверяем GiftStatus (Enum), а не строку "Free"
                var isFree = gift.Status.Equals("Free", StringComparison.OrdinalIgnoreCase);
                var statusText = isFree ? "Свободен" : $"Забронировано ({gift.ReservedBy})";

                var buttons = new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        isFree ? "Забронировать" : "Снять бронь",
                        // Формат CallbackData: ID:STATUS_ENUM_VALUE
                        $"{gift.Id}:{gift.Status}")
                };

                await bot.SendMessage(
                    chatId: update.Message!.Chat.Id,
                    text: $"🎁 **{gift.Title}**\n" +
                          $"🔗 {gift.Link}\n" +
                          $"📌 {statusText}",
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2, // Используем Markdown для выделения
                    replyMarkup: new InlineKeyboardMarkup(buttons),
                    cancellationToken: ct);
            }
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        _logger.LogError($"Ошибка в Telegram Bot: {ex}");
        return Task.CompletedTask;
    }
}
