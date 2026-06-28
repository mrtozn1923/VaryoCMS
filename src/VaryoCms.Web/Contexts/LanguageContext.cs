using VaryoCms.Application.Interfaces;

namespace VaryoCms.Web.Contexts;

// Scoped implementation. Resolved by LanguageResolutionMiddleware after the tenant is known.
public class LanguageContext : ILanguageContext
{
    public string CurrentCode { get; private set; } = "tr";

    public void Set(string code) => CurrentCode = code;
}
