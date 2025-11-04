using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using OpenIddict.Server.AspNetCore;
using WishlistService.Domain.Base;

namespace TelegramBotService.Web.Definitions.OpenApi;

/// <summary>
/// Swagger security configuration scheme
/// </summary>
public sealed class OAuth2SecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    // Меняем IAuthenticationSchemeProvider на IServiceProvider
    private readonly IServiceProvider _serviceProvider;
    private readonly string? _authServerUrl;

    // Внедряем IServiceProvider вместо IAuthenticationSchemeProvider
    public OAuth2SecuritySchemeTransformer(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _authServerUrl = configuration.GetSection("Auth").GetValue<string>("Url");

        ArgumentNullException.ThrowIfNull(_authServerUrl);
    }

    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // 1. ОТЛОЖЕННОЕ РАЗРЕШЕНИЕ: 
        // Получаем IAuthenticationSchemeProvider во время выполнения TransformAsync,
        // когда служба уже гарантированно зарегистрирована (после .AddAuthentication()).
        var authenticationSchemeProvider = _serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();

        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        if (authenticationSchemes.Any(authScheme => authScheme.Name == OpenIddictServerAspNetCoreDefaults.AuthenticationScheme))
        {
            var reference = new OpenApiReference() { Id = "oauth2", Type = ReferenceType.SecurityScheme };
            var scheme = new OpenApiSecurityScheme
            {
                Reference = reference,
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri($"{_authServerUrl}/connect/token", UriKind.Absolute),
                        AuthorizationUrl = new Uri($"{_authServerUrl}/connect/authorize", UriKind.Absolute),
                        Scopes = new Dictionary<string, string> { { "api", "Default scope" } }
                    }
                }
            };

            var requirement = new OpenApiSecurityRequirement
            {
                { new OpenApiSecurityScheme { Reference = reference, In = ParameterLocation.Cookie, Type = SecuritySchemeType.OAuth2 }, new List<string>() }
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
            document.SecurityRequirements = [requirement];
            document.Components.SecuritySchemes[scheme.Reference.Id] = scheme;
            document.Info = new()
            {
                License = new OpenApiLicense() { Name = "MIT", Url = new Uri("https://mit-license.org/") },
                Title = AppData.ServiceName,
                Version = OpenApiDefinition.AppVersion,
                Description = AppData.ServiceDescription,
                Contact = new() { Name = "Sergei Calabonga", Url = new("https://www.calabonga.net"), }
            };
        }
    }
}
