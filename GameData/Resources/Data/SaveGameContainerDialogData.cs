namespace GameData.Resources.Data;

public class SaveGameContainerDialogData {
    public SaveGameContainerDialogData(
        byte examineMessageIndex,
        byte flags,
        uint dialogId
    ) {
        ExamineMessageIndex = examineMessageIndex;
        Flags = flags;
        DialogId = dialogId;
    }

    // 0x00. On EXAMINE (non-primary click) of a building, handle_Building (0x76eab) sets
    // global_30000 to this value and shows dialog_Show(96) (the "examining building" message
    // set) — selects which examine message is shown. (IDA: containerData_dialog.examineMessageIndex.)
    public byte ExamineMessageIndex { get; }

    // 0x01. Flags bitfield read by handle_Building / handle_Grave:
    //   0x01 = state-eval branch (combined with GetGlobalValue(30010));
    //   0x02 = run container action sub_stub157_43(container);
    //   0x20 = suppress/defer the entry dialog_Show(dialogId).
    // (IDA: containerData_dialog.flags, ex flag_field_1.)
    public byte Flags { get; }
    public uint DialogId { get; }
}
