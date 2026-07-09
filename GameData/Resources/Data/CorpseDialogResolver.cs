namespace GameData.Resources.Data;

/// <summary>
/// Faithful port of handle_Corpse's dialog selection (KRONDOR.EXE 0x76a0a). Given the container
/// resolved at the corpse's location (or null) and which mouse button was used, returns the DDX
/// dialog id to show. DDX 94 = "It's a body"; 78 = "corpse looting messages"; 154 = "not important".
/// Lootable container types are 5 and 9.
/// </summary>
public static class CorpseDialogResolver {
    public const int ExamineBodyDialogId = 94;
    public const int LootMessagesDialogId = 78;
    public const int NotImportantDialogId = 154;

    public static int Resolve(SaveGameContainerData? container, bool isPrimary) {
        // Right-click (examine) is always "It's a body".
        if (!isPrimary) {
            return ExamineBodyDialogId;
        }
        // Left-click (loot).
        if (container == null) {
            return ExamineBodyDialogId; // no container at this spot -> "It's a body"
        }
        int type = (int)container.ContainerType;
        if (type == 5 || type == 9) {
            uint? dialogId = container.DialogData?.DialogId;
            return dialogId is > 0 ? (int)dialogId.Value : LootMessagesDialogId;
        }
        return NotImportantDialogId;
    }
}
