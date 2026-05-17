namespace GameData.Resources.Dialog;

[Flags]
public enum DialogEntryFlags {
    None = 0x0,
    KeywordSetupGate = 0x0001,
    IsolatePalette = 0x0002,
    JustifyText = 0x0004,
    ChapterScaledTimer = 0x0008,
    Legacy10 = 0x0010,
    MouseOnlyDismiss = 0x0020,
    AutoAdvanceTimer = 0x0040,
    Legacy80 = 0x0080,
    SuspendInputDuringEntry = 0x0100,
    MultiPageText = 0x0200,
    ChoiceMenu = 0x0400,
    TakeRandomBranch = 0x0800,
    KeywordMenuMode = 0x1000,
    SkipFullScreenSetup = 0x2000,
    SkipWait = 0x4000,
    FixedStripePattern = 0x8000
}
