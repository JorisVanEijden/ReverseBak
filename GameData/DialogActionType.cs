namespace GameData;

public enum DialogActionType {
    Unknown = 0,
    SetTextVariable = 1,
    GiveItem = 2,
    RemoveItem = 3,
    SetGlobalValue = 4,
    ResizeDialog = 6,
    ApplyCondition = 8,
    ChangeAttribute = 9,
    GetPartyAttribute = 10,
    PlayAudio = 12,
    AdvanceTime = 13
}