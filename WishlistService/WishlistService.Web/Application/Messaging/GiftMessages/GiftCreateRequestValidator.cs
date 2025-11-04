using FluentValidation;
using WishlistService.Web.Application.Messaging.GiftMessages.Queries;

namespace WishlistService.Web.Application.Messaging.GiftMessages;

/// <summary>
/// Validator для создания Gift
/// </summary>
public class GiftCreateRequestValidator : AbstractValidator<PostGift.Request>
{
    public GiftCreateRequestValidator()
    {
        RuleSet("default", () =>
        {
            RuleFor(x => x.Model.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(255);
            RuleFor(x => x.Model.Link)
                .MaximumLength(2048)
                .When(x => !string.IsNullOrEmpty(x.Model.Link));
        });
    }
}
