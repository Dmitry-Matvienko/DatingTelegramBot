using FluentValidation;

namespace DatingBot.Application.Validators;

public class AiDescriptionValidator : AbstractValidator<string>
{
    public AiDescriptionValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Error_AiBioEmpty")
            .MinimumLength(5).WithMessage("Error_AiBioMinLength")
            .MaximumLength(2000).WithMessage("Error_AiBioMaxLength");
    }
}
