namespace GameData.Resources.Palette;

/// <summary>
/// A 256-entry index→index lookup, as shipped in <c>CS&lt;n&gt;.DAT</c>.
/// </summary>
/// <remarks>
/// <b>This is a remap of palette INDICES, not a palette.</b> The original applies it to a loaded
/// creature's image data and then colours the result with the ordinary zone palette
/// (<c>combat_actor_bnames_load_cached</c>, CACTOR.C:508-533) — so the same artwork serves several
/// creatures in different colours. Ten of the sixty-four BNAMES entries carry one, and they come in
/// pairs sharing a bitmap: mordel at CS2 and CS1, gnome at CS4/5/6, ogre at CS7 twice, wyvern at CS0
/// and CS9.
///
/// <para>All ten shipped tables are mostly the identity — between 230 and 250 of the 256 entries map
/// to themselves — and every one of them has <c>Lut[0] == 0</c>, which is what lets index 0 keep
/// meaning "transparent" through the remap.</para>
/// </remarks>
public class ColorRemapTable(string id) : IResource {
    /// <summary>Entries in the table. Fixed by the format.</summary>
    public const int Entries = 256;

    public ResourceType Type => ResourceType.DAT;
    public string Id { get; } = id;

    /// <summary>The lookup itself: a source index in, the index to draw instead out.</summary>
    public byte[] Lut { get; set; } = new byte[Entries];
}
