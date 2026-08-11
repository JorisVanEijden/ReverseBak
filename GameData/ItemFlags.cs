namespace GameData;

[Flags]
public enum ItemFlags {
    Lit = 0x1,
    Unknown2 = 0x2,
    Unknown4 = 0x4,
    Unknown8 = 0x8,
    Broken = 0x10,
    Repairable = 0x20,
    Equipped = 0x40,
    Poisoned = 0x80,
    Flaming = 0x100,
    SteelFired = 0x200,
    Frosted = 0x400,
    Enhanced1 = 0x800,
    Enhanced2 = 0x1000,
    Blessed1 = 0x2000,
    Blessed2 = 0x4000,
    Blessed3 = 0x8000,
}