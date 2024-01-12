namespace ResourceExtractor.Resources.Object;

using GameData;

using ResourceExtractor.Resources;

using System.Text;

public class ObjectInfo : IResource {
    public ResourceType Type { get => ResourceType.DAT; }
    public string Name { get; set; }
    public int Field1E { get; set; }
    public ObjectFlags Flags { get; set; }
    public int WordWrap { get; set; }
    public int ChapterNumber { get; set; }
    public int Price { get; set; }
    public int SwingBaseDamage { get; set; }
    public int ThrustBaseDamage { get; set; }
    public int SwingAccuracy_ArmorMod_BowAccuracy { get; set; }
    public int ThrustAccuracy { get; set; }
    public int Icon { get; set; }
    public int InventorySlots { get; set; }
    public int Field34 { get; set; }
    public int Field35 { get; set; }
    public int MaxAmount { get; set; }
    public Race Race { get; set; }
    public int ShopType { get; set; }
    public ObjectType ObjectType { get; set; }
    public ActorAttributeFlag Attributes { get; set; }
    public int Field40 { get; set; }
    public int Field42 { get; set; }
    public int Book1Potion8 { get; set; }
    public int CanEffect { get; set; }
    public int Field48 { get; set; }
    public int Field4A { get; set; }
    public int Field4C { get; set; }
    public int Field4E { get; set; }
    public int Number { get; set; }
    public int Field37 { get; set; }

    public string ToCsv() {
        return
            $"{Number},{Name},{Field1E},{ToBooleans(Flags)},{WordWrap},{ChapterNumber},{Price},{SwingBaseDamage},{ThrustBaseDamage},{SwingAccuracy_ArmorMod_BowAccuracy},{ThrustAccuracy},{Icon},{InventorySlots},{Field34},{MaxAmount},{Field37},{Race},{ShopType:X4},{ObjectType},\"{Attributes}\",{Field40},{Field42},{Book1Potion8},{CanEffect:X4},{Field48:X4},{Field4A},{Field4C},{Field4E}";
    }

    private static string ToBooleans(ObjectFlags flags) {
        char[] bits = new char[16];
        for (int i = 15; i >= 0; i--) {
            if (((int)flags & 1 << i) != 0) {
                bits[i] = '#';
            } else {
                bits[i] = '.';
            }
        }
        return string.Join(',', bits);
    }
}

[Flags]
public enum ObjectFlags {
    B0001 = 0x0001,
    B0002 = 0x0002,
    B0004 = 0x0004,
    B0008 = 0x0008,
    B0010 = 0x0010,
    B0020 = 0x0020,
    CombatUsable = 0x0040,
    B0080 = 0x0080,
    B0100 = 0x0100,
    B0200 = 0x0200,
    B0400 = 0x0400,
    Stackable = 0x0800,
    B1000 = 0x1000,
    LimitedUses = 0x2000,
    B4000 = 0x4000,
    B8000 = 0x8000
}