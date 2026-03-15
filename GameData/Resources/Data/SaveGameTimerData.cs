namespace GameData.Resources.Data;

using GameData.Resources.Dialog.Actions;

public class SaveGameTimerData {
    public SaveGameTimerData(
        TimerType type,
        TimerFlag flag,
        short key,
        int time
    ) {
        Type = type;
        Flag = flag;
        Key = key;
        Time = time;
    }

    public TimerType Type { get; }
    public TimerFlag Flag { get; }
    public short Key { get; }
    public int Time { get; }
}
