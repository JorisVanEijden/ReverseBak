namespace GameData.Resources.Dialog;

[Flags]
public enum DialogEntryFlags {
    None = 0,
    F1 = 1,
    F2 = 2,
    F4 = 4,
    F8 = 8,
    F10 = 16,
    F20 = 32,
    F40 = 64,
    F80 = 128,
    F100 = 256,
    F200 = 512,
    F400 = 1024,
    F800 = 2048,
    F1000 = 4096,
    F2000 = 8192,
    F4000 = 16384,
    F8000 = 32768
}