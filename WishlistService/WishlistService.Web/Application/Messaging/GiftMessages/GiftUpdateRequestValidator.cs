using FluentValidation;
using WishlistService.Web.Application.Messaging.GiftMessages.Queries;

namespace WishlistService.Web.Application.Messaging.GiftMessages;

/// <summary>
/// Validator для обновления Gift
/// </summary>
public class GiftUpdateRequestValidator : AbstractValidator<UpdateGift.Request>
{
    public GiftUpdateRequestValidator()
    {
        RuleSet("default", () =>
        {
            RuleFor(x => x.Model.Title)
                .NotEmpty().WithMessage("Title is required")
                .MaximumLength(255);

            RuleFor(x => x.Model.Link)
                .MaximumLength(1000);

            RuleFor(x => x.Model.ReservedBy)
                .MaximumLength(255);

            RuleFor(x => x.Model.ReservedAt)
                .LessThanOrEqualTo(DateTime.UtcNow)
                .When(x => x.Model.ReservedAt.HasValue);
        });
    }
}
