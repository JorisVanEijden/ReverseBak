namespace ResourceExtraction.Extractors.Dialog.ActionBuilders;

using GameData.Resources.Dialog.Actions;

using System.IO;

internal class SetTimerActionBuilder : IDialogActionBuilder {
    public DialogActionBase Build(BinaryReader resourceReader) {
        return new SetTimerAction {
            Type = (TimerType)resourceReader.ReadByte(),
            Flag = (TimerFlag)resourceReader.ReadByte(),
            Key = resourceReader.ReadUInt16(),
            Time = resourceReader.ReadUInt32()
        };
    }
}
