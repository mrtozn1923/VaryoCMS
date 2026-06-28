using VaryoCms.Application.DTOs.Localization;
using VaryoCms.Application.Localization;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Validators.Localization;

public class UpdateLanguageRequestValidator : AbstractValidator<UpdateLanguageRequest>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateLanguageRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MaximumLength(100);

        RuleFor(x => x.FlagIcon).MaximumLength(100);
    }
}
