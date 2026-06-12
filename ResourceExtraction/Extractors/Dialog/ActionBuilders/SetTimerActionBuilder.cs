namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;
using GameData.Resources.GameState;

using System.IO;

internal class SetTimerActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        var type = (TimerType)resourceReader.ReadByte();
        var flag = (TimerFlag)resourceReader.ReadByte();
        int key = resourceReader.ReadUInt16();
        uint time = resourceReader.ReadUInt32();

        var action = new SetTimerAction { Type = type, Flag = flag, Time = time };
        if (type is TimerType.SetFlag or TimerType.ClearFlag) {
            action.OnExpiry = new SetFlagEffect { Flag = key, Set = type == TimerType.SetFlag };
        } else {
            action.TimerTarget = key;
        }
        return action;
    }
}
