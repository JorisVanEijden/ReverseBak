namespace GameData.Resources.Dialog;

[Flags]
public enum DialogEntryFlags {
    None = 0x0,
    PreserveKeyword = 0x0001,
    IsolatePalette = 0x0002,
    CenterText = 0x0004,
    ChapterScaledTimer = 0x0008,
    Legacy10 = 0x0010,
    KeepAcceptingKeyboard = 0x0020,
    AutoAdvanceTimer = 0x0040,
    Legacy80 = 0x0080,
    SaveScreenBehindDialog = 0x0100,
    TextWithChoice = 0x0200,
    ChoiceMenu = 0x0400,
    TakeRandomBranch = 0x0800,
    PartyMemberSelection = 0x1000,
    SkipOpenWipe = 0x2000,
    SkipWait = 0x4000,
    FixedStripePattern = 0x8000
}
