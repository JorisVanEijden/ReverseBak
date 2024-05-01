namespace GameData.Resources.Object;

[Flags]
public enum ObjectFlags {
    B0001 = 0x0001,
    NotEquipable = 0x0002,
    B0004 = 0x0004,
    B0008 = 0x0008,
    B0010 = 0x0010,
    B0020 = 0x0020,
    OnlyUsableInCombat = 0x0040,
    B0080 = 0x0080,
    NotUsableInCombat = 0x0100,
    ArchersOnly = 0x0200,
    B0400 = 0x0400,
    Stackable = 0x0800,
    B1000 = 0x1000,
    LimitedUses = 0x2000,
    B4000 = 0x4000,
    B8000 = 0x8000
}