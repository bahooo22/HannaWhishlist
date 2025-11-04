using AuthServer.Web.Application.Services;
using AuthServer.Web.Definitions.Authorizations;
using Calabonga.AspNetCore.AppDefinitions;

namespace AuthServer.Web.Definitions.DependencyContainer
{
    /// <summary>
    /// Dependency container definition
    /// </summary>
    public class ContainerDefinition : AppDefinition
    {
        public override void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<IAccountService, AccountService>();
            builder.Services.AddTransient<ApplicationUserClaimsPrincipalFactory>();
        }
    }
}
