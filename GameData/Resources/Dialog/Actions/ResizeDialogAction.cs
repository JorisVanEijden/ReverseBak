namespace GameData.Resources.Dialog.Actions;

/// <summary>
/// Per-entry override of the dialog panel rectangle, expressed in canonical
/// 1600×1200 pixels. Converted from the original VGA (320×200) payload in
/// <c>ResizeDialogActionBuilder</c> via <c>CanonicalSpace.Apply(Dialog)</c>;
/// downstream consumers see only canonical-space coordinates — see <see cref="DialogArea"/>.
/// </summary>
public class ResizeDialogAction : DialogActionBase {
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
