namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// Per-entry override of the dialog panel rectangle, expressed as percentages
/// of the game viewport (0..100). Converted from the original VGA (320×200)
/// payload in <c>ResizeDialogActionBuilder</c>; downstream consumers see only
/// percentages — see <see cref="DialogArea"/>.
/// </summary>
public class ResizeDialogAction : DialogActionBase {
    public float LeftPct { get; set; }
    public float TopPct { get; set; }
    public float WidthPct { get; set; }
    public float HeightPct { get; set; }
}
