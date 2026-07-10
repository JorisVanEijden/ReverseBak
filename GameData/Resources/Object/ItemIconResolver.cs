namespace GameData.Resources.Object;

/// <summary>
/// Faithful port of getIconImageData (KRONDOR.EXE 0x56185): an item's INVSHP bitmap.
/// index = Icon != 0 ? Icon : Number;  index &lt; 120 -> INVSHP1.BMX#index,  else INVSHP2.BMX#(index-120).
/// </summary>
public static class ItemIconResolver {
    public static string ResolveBmxSubResource(ObjectInfo obj) {
        if (obj == null) { return null; }
        int index = obj.Icon != 0 ? obj.Icon : obj.Number;
        return index < 120 ? $"INVSHP1.BMX#{index}" : $"INVSHP2.BMX#{index - 120}";
    }
}
