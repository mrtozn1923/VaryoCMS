using VaryoCms.Application.DTOs.ContentType;
using VaryoCms.Application.Localization;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Validators.ContentType;

public class CreateContentTypeRequestValidator : AbstractValidator<CreateContentTypeRequest>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateContentTypeRequestValidator(IStringLocalizer<SharedResource> localizer)
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

        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.Icon).MaximumLength(100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
