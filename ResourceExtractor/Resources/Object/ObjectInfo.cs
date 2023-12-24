namespace ResourceExtractor.Resources.Object;

using GameData;

using ResourceExtractor.Resources;

public class ObjectInfo : IResource {
    public ResourceType Type { get => ResourceType.DAT; }
    public string Name { get; set; }
    public int Field1E { get; set; }
    public int Field20 { get; set; }
    public int Field22 { get; set; }
    public int Field24 { get; set; }
    public int Price { get; set; }
    public int SwingBaseDamage { get; set; }
    public int ThrustBaseDamage { get; set; }
    public int SwingAccuracy_ArmorMod_BowAccuracy { get; set; }
    public int ThrustAccuracy { get; set; }
    public int Icon { get; set; }
    public int InventorySlots { get; set; }
    public int Field34 { get; set; }
    public int Field36 { get; set; }
    public Race Race { get; set; }
    public int Field3A { get; set; }
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
        return $"{Number},{Name},{Field1E},{Field20:X4},{Field22},{Field24},{Price},{SwingBaseDamage},{ThrustBaseDamage},{SwingAccuracy_ArmorMod_BowAccuracy},{ThrustAccuracy},{Icon},{InventorySlots},{Field34},{Field36},{Field37},{Race},{Field3A:X4},{ObjectType},\"{Attributes}\",{Field40},{Field42},{Book1Potion8},{CanEffect:X4},{Field48:X4},{Field4A},{Field4C},{Field4E}";
    }
}