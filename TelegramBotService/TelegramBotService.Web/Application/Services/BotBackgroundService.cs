using System.Net.Http.Headers;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
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
    private readonly HashSet<string> _admins;

    public BotBackgroundService(
        ITelegramBotClient bot,
        IHttpClientFactory httpClientFactory,
        AuthTokenService tokenService,
        IConfiguration configuration,
        ILogger<BotBackgroundService> logger)
    {
        _bot = bot;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _tokenService = tokenService;
        _admins = configuration
                      .GetSection("Telegram:Admins")
                      .Get<string[]>()? // Получаем массив string[]?
                      .Select(x => x?.TrimStart('@').ToLowerInvariant() ?? string.Empty) // Используем ?? string.Empty для обработки null внутри массива
                      .Where(x => !string.IsNullOrEmpty(x)) // Убираем пустые/null элементы, если они появились
                      .ToHashSet()
                  ?? []; // Если Get<string[]>() вернул null, создаем пустой HashSet

        _logger.LogInformation("Loaded admins: {admins}", string.Join(",", _admins));
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

            if (text?.StartsWith("/addgift") == true)
            {
                await HandleAddGift(bot, message, ct);
                return;
            }

            if (text?.StartsWith("/deletegift") == true)
            {
                await HandleDeleteGift(bot, message, ct);
                return;
            }

            if (text == "/start" || text == "/help")
            {
                await HandleHelp(bot, chatId, ct);
                return;
            }

            if (text == "/wishlist")
            {
                await HandleWishlist(bot, chatId, ct);
                return;
            }
        }

        if (update.CallbackQuery is { } callback)
        {
            await HandleCallback(bot, callback, ct);
        }
    }

    /// <summary>
    /// Callback query handler - buttons
    /// </summary>
    private async Task HandleCallback(ITelegramBotClient bot, CallbackQuery callback, CancellationToken ct)
    {
        if (callback.Message is null || string.IsNullOrEmpty(callback.Data))
        {
            await bot.AnswerCallbackQuery(callback.Id,
                "Ошибка: неверные данные запроса или отсутствует сообщение.",
                showAlert: true,
                cancellationToken: ct);
            return;
        }

        var parts = callback.Data.Split(':');
        if (parts.Length != 2)
        {
            return;
        }

        var giftId = Guid.Parse(parts[0]);
        var status = parts[1];

        var client = _httpClientFactory.CreateClient("WishlistApi");
        var token = await _tokenService.GetTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response;
        if (status.Equals("Free", StringComparison.OrdinalIgnoreCase))
        {
            // бронируем
            var reserveRequest = new ReserveGiftViewModel()
            {
                ReservedById = callback.From.Id.ToString(), // ИЗМЕНИ: Telegram User ID
                ReservedByNickname = callback.From.Username ?? string.Empty, // ИЗМЕНИ: username
                ReservedByFirstName = callback.From.FirstName,
                ReservedByLastName = callback.From.LastName
            };


            response = await client.PostAsJsonAsync($"api/gifts/{giftId}/reserve", reserveRequest, ct);
        }
        else
        {
            // снимаем бронь
            response = await client.PostAsJsonAsync($"api/gifts/{giftId}/unreserve", callback.From.Username, ct);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);

            // Ограничиваем длину ошибки для Telegram
            var shortError = error.Length > 100
                ? error[..100] + "..."
                : error;

            // Ещё лучше - извлекаем только основное сообщение об ошибке
            var cleanError = ExtractErrorMessage(error);

            await bot.AnswerCallbackQuery(callback.Id, $"Ошибка: {cleanError}", showAlert: true, cancellationToken: ct);
            return;
        }

        // ✅ действие прошло успешно → подтягиваем актуальный подарок
        var updatedGift = await client.GetFromJsonAsync<GiftViewModel>($"api/gifts/{giftId}", ct);
        if (updatedGift is null)
        {
            await bot.AnswerCallbackQuery(callback.Id, "Не удалось обновить данные", showAlert: true, cancellationToken: ct);
            return;
        }

        // НОВОЕ ИСПРАВЛЕНИЕ ДЛЯ ОШИБКИ 400(MESSAGE NOT MODIFIED)
        var oldStatusFromCallback = parts[1];
        var newStatusFromApi = updatedGift.Status;

        if (string.Equals(oldStatusFromCallback, newStatusFromApi, StringComparison.OrdinalIgnoreCase))
        {
            // Если статус, который мы получили от API, совпадает со статусом, 
            // который был в кнопке, значит, ничего не изменилось.
            // Мы отвечаем на колбэк и выходим, избегая EditMessageText на строке await bot.EditMessageText(
            await bot.AnswerCallbackQuery(callback.Id, $"Статус не изменился: {newStatusFromApi}", cancellationToken: ct);
            return;
        }

        var statusText = FormatReservedBy(updatedGift, MessageFormat.MarkdownV2);

        var newText =
            $"*{EscapeMarkdown(updatedGift.Title)}*\n" +
            (!string.IsNullOrEmpty(updatedGift.Link) ? $"🔗 {EscapeMarkdown(updatedGift.Link)}\n" : "") +
            $"📌 {statusText}";

        var newButton = InlineKeyboardButton.WithCallbackData(
            updatedGift.Status.Equals("Free", StringComparison.OrdinalIgnoreCase)
                ? "Забронировать"
                : "Снять бронь",
            $"{updatedGift.Id}:{updatedGift.Status}"
        );

        // ✏️ обновляем сообщение
        await bot.EditMessageText(
            chatId: callback.Message.Chat.Id,
            messageId: callback.Message.MessageId,
            text: newText,
            parseMode: ParseMode.MarkdownV2,
            replyMarkup: new InlineKeyboardMarkup(newButton),
            cancellationToken: ct);

        await bot.AnswerCallbackQuery(callback.Id, "✅ Успешно!", cancellationToken: ct);
    }

    /// <summary>
    /// /wishlist command handler
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="chatId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task HandleWishlist(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("WishlistApi");
        var token = await _tokenService.GetTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetFromJsonAsync<OperationResponse<List<GiftViewModel>>>("api/gifts", ct);
        var gifts = response?.Result ?? [];

        if (gifts.Count == 0)
        {
            await bot.SendMessage(chatId, "Список подарков пуст 🎁", cancellationToken: ct);
            return;
        }

        var sorted = gifts.OrderBy(g => g.Title).ToList();

        for (var i = 0; i < sorted.Count; i++)
        {
            var gift = sorted[i];
            var status = gift.Status.Equals("Free", StringComparison.OrdinalIgnoreCase)
                ? "✅ Свободен"
                : $"❌ Забронирован ({gift.ReservedByFirstName} {gift.ReservedByLastName})";

            var text =
                $"{EscapeMarkdown((i + 1).ToString())}\\." +
                $" *{EscapeMarkdown(gift.Title)}*\n" +
                (!string.IsNullOrEmpty(gift.Link) ? $"🔗 {EscapeMarkdown(gift.Link)}\n" : "") +
                $"📌 {EscapeMarkdown(status)}";

            var button = InlineKeyboardButton.WithCallbackData(
                gift.Status.Equals("Free", StringComparison.OrdinalIgnoreCase)
                    ? "Забронировать"
                    : "Снять бронь",
                $"{gift.Id}:{gift.Status}"
            );

            await bot.SendMessage(
                chatId,
                text,
                parseMode: ParseMode.MarkdownV2,
                replyMarkup: new InlineKeyboardMarkup(button),
                cancellationToken: ct);
        }
    }

    /// <summary>
    /// /start and /help command handler
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="chatId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static async Task HandleHelp(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        var helpText =
            "👋 Привет! Я бот‑вишлист 🎁\n\n" +
            "Доступные команды:\n" +
            "/wishlist – показать список подарков\n" +
            "/addgift Название | [ссылка] – добавить подарок\n" +
            "/deletegift [номер по списку] – удалить подарок (только для администраторов)\n" +
            "/help – помощь по командам\n";

        await bot.SendMessage(chatId, helpText, cancellationToken: ct);
    }

    /// <summary>
    /// /deletegift command handler
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="message"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task HandleDeleteGift(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var chatId = message.Chat.Id;
        var username = message.From?.Username?.ToLowerInvariant();

        // 1. **НОВАЯ ПРОВЕРКА:** Гарантируем, что Text не null.
        // Если Text null (хотя это маловероятно, если мы сюда попали),
        // мы не должны пытаться его разделить.
        if (message.Text is null)
        {
            await bot.SendMessage(chatId, "Ошибка: неверная команда.", cancellationToken: ct);
            return;
        }

        if (!_admins.Contains(username ?? ""))
        {
            _logger.LogInformation("[From TG]:username is {Username} \n[From appsettings]:{FirstOrDefault} - for /deletegift", username, _admins.FirstOrDefault());
            await bot.SendMessage(chatId, "⛔ У вас нет прав для удаления подарков.", cancellationToken: ct);
            return;
        }

        var parts = message.Text.Split(' ', 2);
        if (parts.Length < 2 || !int.TryParse(parts[1], out var index))
        {
            await bot.SendMessage(chatId, "Использование: /deletegift <номер>", cancellationToken: ct);
            return;
        }

        var client = _httpClientFactory.CreateClient("WishlistApi");
        var token = await _tokenService.GetTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetFromJsonAsync<OperationResponse<List<GiftViewModel>>>("api/gifts", ct);
        var gifts = response?.Result?.OrderBy(g => g.Title).ToList() ?? [];

        if (index < 1 || index > gifts.Count)
        {
            await bot.SendMessage(chatId, "❌ Неверный номер подарка.", cancellationToken: ct);
            return;
        }

        var gift = gifts[index - 1];
        var deleteResponse = await client.DeleteAsync($"api/gifts/{gift.Id}", ct);

        if (deleteResponse.IsSuccessStatusCode)
        {
            await bot.SendMessage(chatId, $"🗑 Подарок «{gift.Title}» удалён.", cancellationToken: ct);
        }
        else
        {
            var error = await deleteResponse.Content.ReadAsStringAsync(ct);
            await bot.SendMessage(chatId, $"Ошибка при удалении: {error}", cancellationToken: ct);
        }
    }

    /// <summary>
    /// /addgift command handler
    /// </summary>
    /// <param name="bot"></param>
    /// <param name="message"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task HandleAddGift(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        // 1. БЕЗОПАСНАЯ ПРОВЕРКА: Убедимся, что текст сообщения существует.
        if (message.Text is null)
        {
            await bot.SendMessage(message.Chat.Id, "Ошибка: неверная команда.", cancellationToken: ct);
            return;
        }

        var parts = message.Text.Split(' ', 2);
        if (parts.Length < 2)
        {
            await bot.SendMessage(message.Chat.Id,
                "Использование: /addgift Название | [ссылка]", cancellationToken: ct);
            return;
        }

        var args = parts[1].Split('|', 2, StringSplitOptions.TrimEntries);
        var title = args[0];
        var link = args.Length > 1 ? args[1] : null;

        var client = _httpClientFactory.CreateClient("WishlistApi");
        var token = await _tokenService.GetTokenAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var model = new GiftCreateViewModel { Title = title, Link = link };
        var response = await client.PostAsJsonAsync("api/gifts", model, ct);

        if (response.IsSuccessStatusCode)
        {
            await bot.SendMessage(message.Chat.Id, $"🎁 Подарок «{title}» добавлен!", cancellationToken: ct);
        }
        else
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            await bot.SendMessage(message.Chat.Id, $"Ошибка при добавлении подарка: {error}", cancellationToken: ct);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct)
    {
        _logger.LogError("Ошибка в Telegram Bot: {Exception}", ex);
        return Task.CompletedTask;
    }

    private static string EscapeMarkdown(string? text)
    {
        if (text is null)
        {
            return string.Empty;
        }

        // Экранируем все специальные символы MarkdownV2
        var charsToEscape = new[]
        {
            '_', '*', '[', ']', '(', ')', '~', '`', '>', '#',
            '+', '-', '=', '|', '{', '}', '.', '!'
        };

        foreach (var ch in charsToEscape)
        {
            text = text.Replace(ch.ToString(), "\\" + ch);
        }

        return text;
    }

    public enum MessageFormat
    {
        Html,
        MarkdownV2,
        PlainText
    }

    private string FormatReservedBy(GiftViewModel gift, MessageFormat format = MessageFormat.Html)
    {
        if (gift.Status == "Free")
        {
            return "✅ Свободен";
        }

        var displayName = string.IsNullOrEmpty(gift.ReservedByFirstName)
            ? "пользователем"
            : $"{gift.ReservedByFirstName} {gift.ReservedByLastName}".Trim();

        if (!string.IsNullOrEmpty(gift.ReservedById))
        {
            return format switch
            {
                MessageFormat.Html => $"❌ Забронирован <a href=\"tg://user?id={gift.ReservedById}\">{EscapeHtml(displayName)}</a>",
                MessageFormat.MarkdownV2 => $"❌ Забронирован [{EscapeMarkdown(displayName)}](tg://user?id={gift.ReservedById})",
                _ => $"❌ Забронирован {displayName}"
            };
        }

        return $"❌ Забронирован {displayName}";
    }

    private string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }

    private static string ExtractErrorMessage(string error)
    {
        // Если ошибка в JSON формате, пытаемся распарсить
        if (error.Contains("\"title\"") || error.Contains("\"message\""))
        {
            try
            {
                using var doc = JsonDocument.Parse(error);
                if (doc.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                {
                    return title.GetString() ?? "Неизвестная ошибка";
                }

                if (doc.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString() ?? "Неизвестная ошибка";
                }
            }
            catch
            {
                // Если не JSON, продолжаем с обычной обработкой
            }
        }

        // Ограничиваем длину и убираем переносы строк
        var clean = error.Replace("\r", "").Replace("\n", " ").Trim();
        return clean.Length > 80 ? clean[..80] + "..." : clean;
    }
}
