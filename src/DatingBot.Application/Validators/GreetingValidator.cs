using FluentValidation;

namespace DatingBot.Application.Validators;

public class GreetingValidator : AbstractValidator<string>
{
    public GreetingValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Error_GreetingEmpty")
            .MinimumLength(2).WithMessage("Error_GreetingMinLength")
            .MaximumLength(300).WithMessage("Error_GreetingMaxLength");
    }
}
