using VaryoCms.Application.DTOs.Localization;
using VaryoCms.Application.Localization;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Validators.Localization;

public class CreateLanguageRequestValidator : AbstractValidator<CreateLanguageRequest>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateLanguageRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .Matches("^[a-zA-Z]{2,5}$").WithMessage(_ => _localizer["Validation.LanguageCode"]);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MaximumLength(100);

        RuleFor(x => x.FlagIcon).MaximumLength(100);
    }
}
