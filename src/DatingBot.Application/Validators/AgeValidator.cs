using FluentValidation;

namespace DatingBot.Application.Validators;

public class AgeValidator : AbstractValidator<int>
{
    public AgeValidator()
    {
        RuleFor(x => x)
            .InclusiveBetween(10, 100)
            .WithMessage("Error_AgeRange");
    }
}
