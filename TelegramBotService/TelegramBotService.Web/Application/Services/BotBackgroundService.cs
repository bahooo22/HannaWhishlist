using System.Net.Http.Headers;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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
        if (update.Message is { } message)
        {
            var chatId = message.Chat.Id;
            var text = message.Text;

            // /start
            if (text == "/start" || text == "/help")
            {
                var helpText =
                    "👋 Привет! Я бот‑вишлист 🎁\n\n" +
                    "Доступные команды:\n" +
                    "/wishlist – показать список подарков\n" +
                    "/addgift Название | [ссылка] – добавить подарок\n" +
                    "/help – помощь по командам";

                await bot.SendMessage(chatId, helpText, cancellationToken: ct);
                return;
            }

            // /wishlist
            if (text == "/wishlist")
            {
                var client = _httpClientFactory.CreateClient("WishlistApi");
                var token = await _tokenService.GetTokenAsync();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetFromJsonAsync<OperationResponse<List<GiftViewModel>>>("api/gifts", ct);
                var gifts = response?.Result ?? new List<GiftViewModel>();

                if (!gifts.Any())
                {
                    await bot.SendMessage(chatId, "Список подарков пуст 🎁", cancellationToken: ct);
                    return;
                }

                // Заголовок
                var sb = new StringBuilder();
                sb.AppendLine($"🎈🎀 *{EscapeMarkdown("Вишлист Анютке на 1 годик!")}* 🎀🎈");
                sb.AppendLine(EscapeMarkdown("Скоро день рождения нашей малышки! 🍼"));
                sb.AppendLine(EscapeMarkdown("Спасибо, что разделяете этот праздник с нами ❤️") + "\n");

                // Группировка по статусу
                foreach (var gift in gifts)
                {
                    var status = gift.Status.Equals("Free", StringComparison.OrdinalIgnoreCase)
                        ? "✅ Свободен"
                        : $"❌ Забронирован ({gift.ReservedBy})";

                    sb.AppendLine($"🌼 *{EscapeMarkdown(gift.Title)}*");
                    if (!string.IsNullOrEmpty(gift.Link))
                    {
                        sb.AppendLine($"🔗 {EscapeMarkdown(gift.Link)}");
                    }

                    sb.AppendLine($"📌 {EscapeMarkdown(status)}\n");
                }

                await bot.SendMessage(
                    chatId,
                    sb.ToString(),
                    parseMode: ParseMode.MarkdownV2,
                    cancellationToken: ct);
            }
        }
        // 2. Обработка нажатия кнопки
        if (update.CallbackQuery is { } callback)
        {
            var data = callback.Data; // например "GUID:Free" или "GUID:Reserved"
            var parts = data.Split(':');
            if (parts.Length == 2)
            {
                var giftId = Guid.Parse(parts[0]);
                var status = parts[1];

                var client = _httpClientFactory.CreateClient("WishlistApi");
                var token = await _tokenService.GetTokenAsync();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage response;

                if (status.Equals("Free", StringComparison.OrdinalIgnoreCase))
                {
                    // бронируем
                    response = await client.PostAsJsonAsync($"api/gifts/{giftId}/reserve", callback.From.Username, ct);
                }
                else
                {
                    // снимаем бронь
                    response = await client.PostAsync($"api/gifts/{giftId}/unreserve", null, ct);
                }

                if (response.IsSuccessStatusCode)
                {
                    await bot.AnswerCallbackQuery(callback.Id, "✅ Успешно!");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync(ct);
                    await bot.AnswerCallbackQuery(callback.Id, $"Ошибка: {error}", showAlert: true);
                }
            }
        }
        if (update.Message?.Text?.StartsWith("/addgift") == true)
        {
            var parts = update.Message.Text.Split(' ', 2);
            if (parts.Length < 2)
            {
                await bot.SendMessage(update.Message.Chat.Id,
                    "Использование: /addgift Название | [ссылка]", cancellationToken: ct);
                return;
            }

            var args = parts[1].Split('|', 2, StringSplitOptions.TrimEntries);
            var title = args[0];
            var link = args.Length > 1 ? args[1] : null;

            var client = _httpClientFactory.CreateClient("WishlistApi");
            var token = await _tokenService.GetTokenAsync();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var model = new GiftCreateViewModel
            {
                Title = title,
                Link = link
            };

            var response = await client.PostAsJsonAsync("api/gifts", model, ct);

            if (response.IsSuccessStatusCode)
            {
                await bot.SendMessage(update.Message.Chat.Id,
                    $"🎁 Подарок «{title}» добавлен!", cancellationToken: ct);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                await bot.SendMessage(update.Message.Chat.Id,
                    $"Ошибка при добавлении подарка: {error}", cancellationToken: ct);
            }
        }

    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        _logger.LogError($"Ошибка в Telegram Bot: {ex}");
        return Task.CompletedTask;
    }
    private static string EscapeMarkdown(string text)
    {
        var charsToEscape = new[] { "_", "*", "[", "]", "(", ")", "~", "`", ">", "#", "+", "-", "=", "|", "{", "}", ".", "!" };
        foreach (var c in charsToEscape)
        {
            text = text.Replace(c, "\\" + c);
        }

        return text;
    }

}
