namespace GameData.Resources.Text;

/// <summary>
/// The ambient UI-string catalog. Ambient rather than injected because the consumers
/// (<c>MoneyFormatter</c>, <c>DialogSlotPopulator</c>) are static pure functions whose call sites
/// would otherwise all have to thread a catalog through. The trade-off is hidden state; it is
/// bounded by this being the ONLY mutable global here, and by the setter existing solely so a
/// mod's merged catalog can replace the embedded default at startup.
/// </summary>
public static class UiStrings {
    private static UiStringCatalog _catalog;

    public static UiStringCatalog Catalog {
        get => _catalog ??= UiStringCatalog.Embedded;
        set => _catalog = value;
    }

    public static string Get(string key) => Catalog.Get(key);
}
