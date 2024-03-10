namespace ResourceExtractor.Extractors.Dialog.ActionBuilders;

using ResourceExtractor.Resources.Dialog.Actions;

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