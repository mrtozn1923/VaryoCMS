using VaryoCms.Application.DTOs.Dictionary;
using VaryoCms.Application.Localization;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Validators.Dictionary;

public class SaveDictionaryEntryRequestValidator : AbstractValidator<SaveDictionaryEntryRequest>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SaveDictionaryEntryRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.KeyName)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MaximumLength(300)
            .Matches("^[A-Za-z0-9]+([._-][A-Za-z0-9]+)*$")
            .WithMessage(_ => _localizer["Validation.DictionaryKey"]);

        RuleFor(x => x.Category).MaximumLength(100);
    }
}
