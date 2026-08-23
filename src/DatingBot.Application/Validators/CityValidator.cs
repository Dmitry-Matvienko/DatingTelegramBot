using FluentValidation;

namespace DatingBot.Application.Validators;

public class CityValidator : AbstractValidator<string>
{
    public CityValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Error_CityEmpty")
            .MinimumLength(2).WithMessage("Error_CityMinLength")
            .MaximumLength(100).WithMessage("Error_CityMaxLength")
            .Matches(@"^[\p{L}\p{M}\s\-'.]+$").WithMessage("Error_CityLetters");
    }
}
