using FluentValidation;
using MentorX.Application.DTOs.Requests;

namespace MentorX.Application.Validators;

public class PurchaseCreditsRequestValidator : AbstractValidator<PurchaseCreditsRequest>
{
    public PurchaseCreditsRequestValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty().WithMessage("PackageId is required");
    }
}
