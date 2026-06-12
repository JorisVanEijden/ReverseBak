namespace GameData.Resources.Dialog.Actions;

using GameData.Resources.GameState;

public class SetTimerAction : DialogActionBase {
    public TimerType Type { get; set; }
    public TimerFlag Flag { get; set; }
    public uint Time { get; set; }

    /// <summary>For SetFlag/ClearFlag timers: the global-flag write applied on expiry. Null otherwise.</summary>
    public Effect? OnExpiry { get; set; }

    /// <summary>For Light/Spell timers: the raw timer target key (semantics unconfirmed). Null for flag timers.</summary>
    public int? TimerTarget { get; set; }
}
