using Calabonga.AspNetCore.AppDefinitions;
using WishlistService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace WishlistService.Web.Definitions.DbContext;

/// <summary>
/// ASP.NET Core services registration and configurations
/// </summary>
public class DbContextDefinition : AppDefinition
{
    /// <summary>
    /// Configure services for current microservice
    /// </summary>
    /// <param name="builder"></param>
    public override void ConfigureServices(WebApplicationBuilder builder)
        => builder.Services.AddDbContext<ApplicationDbContext>(config =>
        {
            // ⚠️ SQLite вместо InMemory
            config.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
        });
}
