namespace GameData.Resources.Animation.Commands;

/// <summary>
/// This sets global bool 39DD:0EA8 to true.
/// But this is never read.
/// </summary>
public class ObsoleteCommand0500 : FrameCommand {
    public override string ToString() {
        return $"{nameof(ObsoleteCommand0500)}();";
    }

}