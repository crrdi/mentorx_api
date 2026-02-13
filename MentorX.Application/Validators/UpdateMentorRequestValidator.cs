using FluentValidation;
using MentorX.Application.DTOs.Requests;

namespace MentorX.Application.Validators;

public class UpdateMentorRequestValidator : AbstractValidator<UpdateMentorRequest>
{
    public UpdateMentorRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MinimumLength(1).WithMessage("Name must be at least 1 character")
            .MaximumLength(255).WithMessage("Name must be at most 255 characters");

        RuleFor(x => x.PublicBio)
            .NotEmpty().WithMessage("PublicBio is required")
            .MinimumLength(10).WithMessage("PublicBio must be at least 10 characters");

        RuleFor(x => x.ExpertisePrompt)
            .NotEmpty().WithMessage("ExpertisePrompt is required")
            .MinimumLength(20).WithMessage("ExpertisePrompt must be at least 20 characters");

        RuleFor(x => x.ExpertiseTags)
            .NotEmpty().WithMessage("At least one expertise tag is required")
            .Must(tags => tags.Count <= 5).WithMessage("You can add up to 5 tags only")
            .ForEach(tag => tag
                .NotEmpty().WithMessage("Tag cannot be empty")
                .MaximumLength(100).WithMessage("Each tag must be at most 100 characters"));
    }
}
