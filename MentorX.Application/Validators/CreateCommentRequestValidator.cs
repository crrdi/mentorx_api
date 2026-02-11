using FluentValidation;
using MentorX.Application.DTOs.Requests;

namespace MentorX.Application.Validators;

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MinimumLength(1).WithMessage("Content must be at least 1 character")
            .When(x => !x.MentorId.HasValue);

        RuleFor(x => x.MentorId)
            .NotEmpty().WithMessage("MentorId is required")
            .When(x => string.IsNullOrEmpty(x.Content));
    }
}
