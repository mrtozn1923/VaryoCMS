namespace VaryoCms.Application.Interfaces;

// Scoped, per-request. Resolved by LanguageResolutionMiddleware.
// CurrentCode is used in repository joins for localized values.
public interface ILanguageContext
{
    string CurrentCode { get; }
}
