using System.Text.Json;

namespace TelegramBotService.Web.Application.Services
{
    public class AuthTokenService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private string? _accessToken;
        private DateTime _expiresAt;

        public AuthTokenService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GetTokenAsync()
        {
            // Если токен ещё живой — возвращаем его
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresAt)
            {
                return _accessToken;
            }

            // Берём client_id и secret из конфигурации (секреты!)
            var clientId = _configuration["Auth:ClientId"];
            var clientSecret = _configuration["Auth:ClientSecret"];
            var scope = _configuration["Auth:Scope"] ?? "api";
            var authServerUrl = _configuration["Auth:Url"] ?? "https://localhost:10001";

            var body = new Dictionary<string, string>
            {
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!,
                ["grant_type"] = "client_credentials",
                ["scope"] = scope
            };

            var response = await _httpClient.PostAsync(
                $"{authServerUrl}/connect/token",
                new FormUrlEncodedContent(body));

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();

            // Запоминаем время истечения токена
            _expiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 30); // небольшой запас

            return _accessToken!;
        }
    }
}
