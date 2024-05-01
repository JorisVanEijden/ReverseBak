namespace GameData;

public enum DialogActionType {
    NullAction = 0,
    SetTextVariable = 1,
    GiveItem = 2,
    RemoveItem = 3,
    SetGlobalValue = 4,
    ChangeFace = 5,
    ResizeDialog = 6,
    SubAction = 7,
    ApplyCondition = 8,
    ChangeAttribute = 9,
    GetPartyAttribute = 10,
    PlaySound = 11,
    PlayAudio = 12,
    AdvanceTime = 13,
    SetTemporaryFlag = 14,
    ChangeParty = 17,
    Heal = 18,
    LearnSpell = 19,
    Teleport = 20,
    SetTimer = 22,
    UseItem = 23
}