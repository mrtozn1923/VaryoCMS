using VaryoCms.Application.Interfaces;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Infrastructure.Localization;

// Every requested resource type maps to the same global key space (resource type/baseName is ignored),
// so IViewLocalizer, IStringLocalizer<T> and DataAnnotations localization all read the one DB store.
public class DbStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly IUiTranslationStore _store;

    public DbStringLocalizerFactory(IUiTranslationStore store) => _store = store;

    public IStringLocalizer Create(Type resourceSource) => new DbStringLocalizer(_store);

    public IStringLocalizer Create(string baseName, string location) => new DbStringLocalizer(_store);
}
