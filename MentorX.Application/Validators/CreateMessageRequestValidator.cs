using FluentValidation;
using MentorX.Application.DTOs.Requests;

namespace MentorX.Application.Validators;

public class CreateMessageRequestValidator : AbstractValidator<CreateMessageRequest>
{
    public CreateMessageRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MinimumLength(1).WithMessage("Content must be at least 1 character");
    }
}
