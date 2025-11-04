using Calabonga.AspNetCore.AppDefinitions;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using WishlistService.Contracts.ViewModels;
using WishlistService.Domain.Entities;
using WishlistService.Web.Application.Messaging.GiftMessages.Commands;
using WishlistService.Web.Application.Messaging.GiftMessages.Queries;

namespace WishlistService.Web.Endpoints;

public sealed class GiftEndpoints : AppDefinition
{
    public override void ConfigureApplication(WebApplication app) => app.MapGiftEndpoints();
}

internal static class GiftEndpointsExtensions
{
    public static void MapGiftEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/gifts/").WithTags(nameof(Gift));

        // POST: reserve gift
        group.MapPost("{id:guid}/reserve", async (
                    [FromServices] IMediator mediator,
                    Guid id,
                    [FromBody] string userName, // или DTO с UserId/UserName
                    HttpContext context)
                => await mediator.Send(new ReserveGift.Request(id, context.User.Identity!.Name!, userName), context.RequestAborted))
            //            .RequireAuthorization(x => x.AddAuthenticationSchemes(AuthData.AuthSchemes).RequireAuthenticatedUser())
            .Produces(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .WithOpenApi();

        // POST: unreserve gift
        group.MapPost("{id:guid}/unreserve", async (
                    [FromServices] IMediator mediator,
                    Guid id,
                    HttpContext context)
                => await mediator.Send(new UnreserveGift.Request(id, context.User.Identity!.Name!), context.RequestAborted))
            //            .RequireAuthorization(x => x.AddAuthenticationSchemes(AuthData.AuthSchemes).RequireAuthenticatedUser())
            .Produces(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .WithOpenApi();

        // GET all gifts
        group.MapGet("", async (
                    [FromServices] IMediator mediator,
                    HttpContext context)
                => await mediator.Send(new GetGiftList.Request(), context.RequestAborted))
            //            .RequireAuthorization(x => x.AddAuthenticationSchemes(AuthData.AuthSchemes).RequireAuthenticatedUser())
            .Produces(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .WithOpenApi();

        // Get paged gifts
        group.MapGet("paged/{pageIndex:int}", async (
                    [FromServices] IMediator mediator,
                    string? search,
                    HttpContext context,
                    int pageIndex = 0,
                    int pageSize = 10)
                => await mediator.Send(new GetGiftPaged.Request(pageIndex, pageSize, search), context.RequestAborted))
            //            .RequireAuthorization(x => x.AddAuthenticationSchemes(AuthData.AuthSchemes).RequireAuthenticatedUser())
            .Produces(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .WithOpenApi();

        // GET by Id
        group.MapGet("{id:guid}", async (
                [FromServices] IMediator mediator,
                Guid id,
                HttpContext context)
                => await mediator.Send(new GetGiftById.Request(id), context.RequestAborted))
            //            .RequireAuthorization(x => x.AddAuthenticationSchemes(AuthData.AuthSchemes).RequireAuthenticatedUser())
            .Produces(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .WithOpenApi();

        // DELETE
        group.MapDelete("{id:guid}", async (
                [FromServices] IMediator mediator,
                Guid id,
                HttpContext context)
                => await mediator.Send(new DeleteGift.Request(id), context.RequestAborted))
            //            .RequireAuthorization(x => x.AddAuthenticationSchemes(AuthData.AuthSchemes).RequireAuthenticatedUser())
            .Produces(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .WithOpenApi();

        // POST (create)
        group.MapPost("", async (
                [FromServices] IMediator mediator,
                GiftCreateViewModel model,
                HttpContext context)
                => await mediator.Send(new PostGift.Request(model), context.RequestAborted))
            //            .RequireAuthorization(x => x.AddAuthenticationSchemes(AuthData.AuthSchemes).RequireAuthenticatedUser())
            .Produces(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .WithOpenApi();

        // PUT (update)
        group.MapPut("{id:guid}", async (
                [FromServices] IMediator mediator,
                Guid id,
                GiftUpdateViewModel model,
                HttpContext context)
                => await mediator.Send(new UpdateGift.Request(id, model), context.RequestAborted))
            //            .RequireAuthorization(x => x.AddAuthenticationSchemes(AuthData.AuthSchemes).RequireAuthenticatedUser())
            .Produces(200)
            .ProducesProblem(401)
            .ProducesProblem(404)
            .WithOpenApi();
    }
}
