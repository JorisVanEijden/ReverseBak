namespace GameData.Resources.Dialog.Actions;

using GameData.Resources.GameState;

/// <summary>
/// A dialog action that mutates global game state. The uniform carrier for
/// <c>SetGlobalValue</c> (#4) and <c>SetTemporaryFlag</c> (#14); the specific
/// mutation lives in the shared <see cref="Effect"/>, decoded via GlobalRef.
/// </summary>
public class GlobalEffectAction : DialogActionBase {
    public Effect Effect { get; set; } = null!;
}
