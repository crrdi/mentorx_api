using FluentValidation;
using MentorX.Application.DTOs.Requests;

namespace MentorX.Application.Validators;

public class CreateMentorRequestValidator : AbstractValidator<CreateMentorRequest>
{
    public CreateMentorRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(1).WithMessage("Name must be at least 1 character");

        RuleFor(x => x.PublicBio)
            .NotEmpty().WithMessage("PublicBio is required")
            .MinimumLength(10).WithMessage("PublicBio must be at least 10 characters");

        RuleFor(x => x.ExpertisePrompt)
            .NotEmpty().WithMessage("ExpertisePrompt is required")
            .MinimumLength(20).WithMessage("ExpertisePrompt must be at least 20 characters");

        RuleFor(x => x.ExpertiseTags)
            .NotEmpty().WithMessage("ExpertiseTags are required")
            .Must(tags => tags.Count <= 5).WithMessage("Maximum 5 tags allowed")
            .ForEach(tag => tag.NotEmpty().WithMessage("Tag cannot be empty"));
    }
}
