using System.Text.Json;
using VaryoCms.Application.DTOs.ContentField;
using VaryoCms.Application.Localization;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Validators.ContentField;

public class CreateContentFieldRequestValidator : AbstractValidator<CreateContentFieldRequest>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateContentFieldRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.ContentTypeId).GreaterThan(0);

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
            .Must(BeValidJson).WithMessage(_ => _localizer["Validation.OptionsJson"])
            .When(x => !string.IsNullOrWhiteSpace(x.OptionsJson));
    }

    internal static bool BeValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        try { using var _ = JsonDocument.Parse(json); return true; }
        catch (JsonException) { return false; }
    }
}
