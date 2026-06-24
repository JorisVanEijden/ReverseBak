namespace GameData.Resources.Data;

/// <summary>
/// Lightweight parsed header of a <c>SAVE%02d.GAM</c> slot — the per-slot metadata the
/// original engine's <c>LoadSaveGameHeader</c> (0x6fd66) reads. Use this for cheap
/// save-slot listing without parsing the full ~300 KB save body via
/// <see cref="SaveGame"/>. Layout (100 bytes, little-endian):
/// <list type="table">
/// <item><term>0x00 (90)</term><description><see cref="Name"/>, null-terminated CP437</description></item>
/// <item><term>0x5A (2)</term><description><see cref="ChapterNumber"/> (1..9)</description></item>
/// <item><term>0x5C (2)</term><description><see cref="WorldX"/> (pixel on FULLMAP.SCX)</description></item>
/// <item><term>0x5E (2)</term><description><see cref="WorldY"/></description></item>
/// <item><term>0x60 (2)</term><description><see cref="MapIcon"/></description></item>
/// <item><term>0x62 (2)</term><description><see cref="Version"/> — engine rejects anything
/// other than <see cref="SaveGame.SupportedVersion"/></description></item>
/// </list>
/// </summary>
public readonly struct SaveGameHeader {
    /// <summary>Length in bytes of the null-padded CP437 save name field.</summary>
    public const int NameLength = 90;

    /// <summary>Total size in bytes of the on-disk header.</summary>
    public const int Size = 100;

    public SaveGameHeader(
        string name, short chapterNumber, short worldX, short worldY, short mapIcon, short version) {
        Name = name;
        ChapterNumber = chapterNumber;
        WorldX = worldX;
        WorldY = worldY;
        MapIcon = mapIcon;
        Version = version;
    }

    public string Name { get; }
    public short ChapterNumber { get; }
    public short WorldX { get; }
    public short WorldY { get; }
    public short MapIcon { get; }
    public short Version { get; }

    /// <summary>True when <see cref="Version"/> matches <see cref="SaveGame.SupportedVersion"/>.</summary>
    public bool IsSupportedVersion => Version == SaveGame.SupportedVersion;
}
