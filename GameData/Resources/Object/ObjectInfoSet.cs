namespace GameData.Resources.Object;

using System.Collections.Generic;

/// <summary>An IResource wrapper over the 138 OBJINFO.DAT item definitions, indexable by object id.</summary>
public class ObjectInfoSet : IResource {
    private readonly Dictionary<int, ObjectInfo> _byId;
    public ObjectInfoSet(string id, IReadOnlyList<ObjectInfo> items,
                         IReadOnlyList<int> spellPrices = null) {
        Id = id; Items = items;
        SpellPrices = spellPrices ?? System.Array.Empty<int>();
        _byId = new Dictionary<int, ObjectInfo>();
        foreach (ObjectInfo o in items) { _byId[o.Number] = o; }
    }
    public IReadOnlyList<ObjectInfo> Items { get; }

    /// <summary>
    /// What each spell costs on a scroll, indexed by spell number — the short array OBJINFO.DAT
    /// carries after its 138 item records.
    /// </summary>
    /// <remarks>
    /// A Magical Scroll's own <see cref="ObjectInfo.Price"/> is 0, so it cannot be priced the
    /// ordinary way. <c>itemtbl_compute_value</c> (ITEMTBL.C:66) special-cases it:
    /// <c>value = ((int far *)(g_pItemDefTable + 0x2b20))[slot->condition]</c> — and a scroll's
    /// condition byte is the SPELL NUMBER, the same field <c>SpellBook.Learn</c> reads.
    /// See docs/shop-pricing.md line 60.
    /// </remarks>
    public IReadOnlyList<int> SpellPrices { get; }

    /// <summary>The scroll price for a spell, or 0 when the table does not reach it.</summary>
    public int SpellPriceFor(int spellNumber) =>
        spellNumber >= 0 && spellNumber < SpellPrices.Count ? SpellPrices[spellNumber] : 0;
    public ObjectInfo GetById(int objectId) => _byId.TryGetValue(objectId, out ObjectInfo o) ? o : null;
    public string Id { get; }
    public ResourceType Type => ResourceType.DAT;
}
