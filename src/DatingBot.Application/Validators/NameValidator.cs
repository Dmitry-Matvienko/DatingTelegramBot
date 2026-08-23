using FluentValidation;

namespace DatingBot.Application.Validators;

public class NameValidator : AbstractValidator<string>
{
    public NameValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Error_NameEmpty")
            .MinimumLength(2).WithMessage("Error_NameMinLength")
            .MaximumLength(50).WithMessage("Error_NameMaxLength")
            .Matches(@"^[\p{L}\p{M}\s\-']+$").WithMessage("Error_NameLetters");
    }
}
