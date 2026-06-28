using VaryoCms.Application.Common;

namespace VaryoCms.Application.Interfaces;

// In-memory, cached view over the global UI translations. Singleton.
// Used by the DB-backed string localizer and by the middleware that sets the request culture.
public interface IUiTranslationStore
{
    IReadOnlyDictionary<string, string> GetForCulture(string culture);
    IReadOnlyCollection<string> ActiveCultures { get; }
    IReadOnlyList<UiCultureItem> ActiveCultureItems { get; }   // code + display name (for the UI switcher)
    string DefaultCulture { get; }
    bool IsActiveCulture(string code);
    void Invalidate();   // drop caches after SystemAdmin edits/imports (Phase 4)
}
