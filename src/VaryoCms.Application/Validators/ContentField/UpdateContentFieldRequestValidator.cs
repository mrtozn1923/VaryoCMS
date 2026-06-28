using VaryoCms.Application.DTOs.ContentField;
using VaryoCms.Application.Localization;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Validators.ContentField;

public class UpdateContentFieldRequestValidator : AbstractValidator<UpdateContentFieldRequest>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateContentFieldRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MaximumLength(200)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage(_ => _localizer["Validation.Slug"]);

        RuleFor(x => x.FieldType)
            .IsInEnum().WithMessage(_ => _localizer["Validation.FieldType"]);

        RuleFor(x => x.OptionsJson)
            .Must(CreateContentFieldRequestValidator.BeValidJson).WithMessage(_ => _localizer["Validation.OptionsJson"])
            .When(x => !string.IsNullOrWhiteSpace(x.OptionsJson));
    }
}
