namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

/// <summary>
/// Action 21's four words. <b>The first one is SIGNED.</b>
/// </summary>
/// <remarks>
/// <c>nResult</c> is a 16-bit <c>int</c> in the original, and the values the callers test for are
/// negative: <c>dialog_play_record(...) == -1</c> cancels a building interaction outright
/// (WCURSOR.C:355), and <c>GdsSceneRules.OutcomeFor</c> maps -1, -2, -3, -4 and -5 onto scene
/// actions. Reading the word unsigned turns every one of them into 65531..65535, which matches
/// nothing — <b>84 of the corpus's 97 SetReturnValue actions are negative</b> (-1 x67, -2 x9,
/// -4 x8, across 13 dialog files), so the unsigned reading silently disabled the lot.
///
/// <para>Found 2026-09-13 by driving both games: the armourer's refusal answers -1, the original
/// cancels on it, and the port walked into the shop it had just been turned away from. See
/// TASK-467.</para>
/// </remarks>
internal class SetReturnValueActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        short value = resourceReader.ReadInt16();
        ushort field2 = resourceReader.ReadUInt16();
        ushort field4 = resourceReader.ReadUInt16();
        ushort field6 = resourceReader.ReadUInt16();
        return new SetReturnValueAction {
            Value = value,
            Field2 = field2,
            Field4 = field4,
            Field6 = field6
        };
    }
}
