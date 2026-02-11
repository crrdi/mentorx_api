using FluentValidation;
using MentorX.Application.DTOs.Requests;

namespace MentorX.Application.Validators;

public class CreateInsightRequestValidator : AbstractValidator<CreateInsightRequest>
{
    public CreateInsightRequestValidator()
    {
        RuleFor(x => x.MentorId)
            .NotEmpty().WithMessage("MentorId is required");

        RuleFor(x => x.Quote)
            .MaximumLength(280).WithMessage("Quote must be at most 280 characters")
            .When(x => !string.IsNullOrEmpty(x.Quote));
    }
}
