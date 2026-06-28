using VaryoCms.Application.DTOs.ContentItem;
using VaryoCms.Application.Localization;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Validators.ContentItem;

public class SaveContentItemRequestValidator : AbstractValidator<SaveContentItemRequest>
{
    private static readonly string[] AllowedStatuses = { "draft", "published", "archived" };

    private readonly IStringLocalizer<SharedResource> _localizer;

    public SaveContentItemRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.ContentTypeId).GreaterThan(0);

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage(_ => _localizer["Err.Title.Required"])
            .MaximumLength(500);

        RuleFor(x => x.LanguageCode)
            .NotEmpty().MaximumLength(5);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage(_ => _localizer["Validation.Status"]);

        RuleFor(x => x.Slug)
            .MaximumLength(500)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage(_ => _localizer["Validation.Slug"]);
    }
}
