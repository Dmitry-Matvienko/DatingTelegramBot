using FluentValidation;

namespace DatingBot.Application.Validators;

public class HeightValidator : AbstractValidator<int>
{
    public HeightValidator()
    {
        RuleFor(x => x)
            .InclusiveBetween(100, 250)
            .WithMessage("Error_HeightRange");
    }
}
