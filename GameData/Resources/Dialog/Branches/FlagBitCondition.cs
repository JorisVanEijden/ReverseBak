namespace GameData.Resources.Dialog.Branches;

/// <summary>
/// One bit condition within a <see cref="FlagMaskBranch"/>: the
/// <see cref="Bit"/> (0-7) of the flag byte must be set (<see cref="Set"/> =
/// <c>true</c>) or clear (<c>false</c>) for the condition to hold.
/// </summary>
public class FlagBitCondition {
    public int Bit { get; set; }
    public bool Set { get; set; }
}
