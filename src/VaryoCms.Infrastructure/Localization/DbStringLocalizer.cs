using System.Globalization;
using VaryoCms.Application.Interfaces;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Infrastructure.Localization;

// Resolves keys from the global UI translation store using CultureInfo.CurrentUICulture.
// Fallback chain: current culture -> default culture -> the key itself.
public class DbStringLocalizer : IStringLocalizer
{
    private readonly IUiTranslationStore _store;

    public DbStringLocalizer(IUiTranslationStore store) => _store = store;

    public LocalizedString this[string name]
    {
        get
        {
            var value = Lookup(name, out bool found);
            return new LocalizedString(name, value, resourceNotFound: !found);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var format = Lookup(name, out bool found);
            var value = string.Format(CultureInfo.CurrentCulture, format, arguments);
            return new LocalizedString(name, value, resourceNotFound: !found);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        foreach (var kv in _store.GetForCulture(culture))
            yield return new LocalizedString(kv.Key, kv.Value, resourceNotFound: false);
    }

    private string Lookup(string name, out bool found)
    {
        var culture = CultureInfo.CurrentUICulture.Name;
        if (_store.GetForCulture(culture).TryGetValue(name, out var value))
        {
            found = true;
            return value;
        }

        var def = _store.DefaultCulture;
        if (!string.Equals(culture, def, StringComparison.OrdinalIgnoreCase)
            && _store.GetForCulture(def).TryGetValue(name, out var defValue))
        {
            found = true;
            return defValue;
        }

        found = false;
        return name;   // surface the key when no translation exists
    }
}
