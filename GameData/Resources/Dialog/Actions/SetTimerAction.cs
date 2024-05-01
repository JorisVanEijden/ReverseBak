namespace GameData.Resources.Dialog.Actions;

public class SetTimerAction : DialogActionBase {
    public TimerType Type { get; set; }
    public TimerFlag Flag { get; set; }
    public int Key { get; set; }
    public uint Time { get; set; }
}