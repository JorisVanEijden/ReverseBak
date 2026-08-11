namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// Action type 7. Not a recursive dialog dispatcher — invokes one of 17
/// hardcoded game-state effects via <c>PerformSubAction</c> at 0x40f19. The
/// effect is selected by <see cref="SubActionType"/>; Field2/4/6 are
/// subtype-specific parameters. See <see cref="SubActionType"/> for the
/// per-subtype semantics.
/// </summary>
public class SubAction : DialogActionBase {
    public SubActionType SubActionType { get; set; }
    public ushort Field2 { get; set; }
    public ushort Field4 { get; set; }
    public ushort Field6 { get; set; }
}