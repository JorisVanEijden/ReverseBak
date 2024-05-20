namespace GameData.Resources.Dialog;

[Flags]
public enum DialogEntryFlags {
    None = 0x0,
    F1 = 0x1,
    F2 = 0x2,
    F4 = 0x4,
    F8 = 0x8,
    F10 = 0x10,
    F20 = 0x20,
    F40 = 0x40,
    F80 = 0x80,
    F100 = 0x100,
    F200 = 0x200,
    F400 = 0x400,
    TakeRandomBranch = 0x800,
    F1000 = 0x1000,
    F2000 = 0x2000,
    F4000 = 0x4000,
    F8000 = 0x8000
}