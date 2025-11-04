using Calabonga.AspNetCore.AppDefinitions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace TelegramBotService.Web.Definitions.Authorizations;

public class AuthorizationDefinition : AppDefinition
{
    // Используем ServiceOrderIndex и ApplicationOrderIndex
    // Аутентификация должна быть зарегистрирована ОЧЕНЬ рано (низкое число)
    public override int ServiceOrderIndex => 100; // Регистрация AddAuthentication

    // Применение Middlewares (UseAuthentication/UseAuthorization) должно быть раньше, чем Endpoints
    public override int ApplicationOrderIndex => 100;

    public override void ConfigureServices(WebApplicationBuilder builder)
    {
        // РЕГИСТРАЦИЯ: Добавляем IAuthenticationSchemeProvider
        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                // Используем конфигурацию из appsettings.json
                var authServerUrl = builder.Configuration.GetSection("Auth").GetValue<string>("Url");

                options.Authority = authServerUrl;
                options.RequireHttpsMetadata = false; // Установите 'true' для Production
                options.Audience = "telegram-bot";

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        // Также регистрируем авторизацию, если она используется
        builder.Services.AddAuthorization();
    }

    public override void ConfigureApplication(WebApplication app)
    {
        // Middleware для аутентификации и авторизации (обязательно!)
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
