namespace GameData.Resources.Data;

public class SaveGameActorData {
    public SaveGameActorData(
        ushort namePointer,
        short knownSpells1,
        short knownSpells2,
        short knownSpells3,
        SaveGameAttributeValuesData health,
        SaveGameAttributeValuesData stamina,
        SaveGameAttributeValuesData speed,
        SaveGameAttributeValuesData strength,
        SaveGameAttributeValuesData defense,
        SaveGameAttributeValuesData accuracyCrossbow,
        SaveGameAttributeValuesData accuracyMelee,
        SaveGameAttributeValuesData accuracyCasting,
        SaveGameAttributeValuesData assessment,
        SaveGameAttributeValuesData armorcraft,
        SaveGameAttributeValuesData weaponcraft,
        SaveGameAttributeValuesData barding,
        SaveGameAttributeValuesData haggling,
        SaveGameAttributeValuesData lockpick,
        SaveGameAttributeValuesData scouting,
        SaveGameAttributeValuesData stealth,
        byte actorNumber,
        uint inventoryPointer,
        ushort combatDataPointer
    ) {
        NamePointer = namePointer;
        KnownSpells1 = knownSpells1;
        KnownSpells2 = knownSpells2;
        KnownSpells3 = knownSpells3;
        Health = health;
        Stamina = stamina;
        Speed = speed;
        Strength = strength;
        Defense = defense;
        AccuracyCrossbow = accuracyCrossbow;
        AccuracyMelee = accuracyMelee;
        AccuracyCasting = accuracyCasting;
        Assessment = assessment;
        Armorcraft = armorcraft;
        Weaponcraft = weaponcraft;
        Barding = barding;
        Haggling = haggling;
        Lockpick = lockpick;
        Scouting = scouting;
        Stealth = stealth;
        ActorNumber = actorNumber;
        InventoryPointer = inventoryPointer;
        CombatDataPointer = combatDataPointer;
    }

    public ushort NamePointer { get; }
    public short KnownSpells1 { get; }
    public short KnownSpells2 { get; }
    public short KnownSpells3 { get; }
    public SaveGameAttributeValuesData Health { get; }
    public SaveGameAttributeValuesData Stamina { get; }
    public SaveGameAttributeValuesData Speed { get; }
    public SaveGameAttributeValuesData Strength { get; }
    public SaveGameAttributeValuesData Defense { get; }
    public SaveGameAttributeValuesData AccuracyCrossbow { get; }
    public SaveGameAttributeValuesData AccuracyMelee { get; }
    public SaveGameAttributeValuesData AccuracyCasting { get; }
    public SaveGameAttributeValuesData Assessment { get; }
    public SaveGameAttributeValuesData Armorcraft { get; }
    public SaveGameAttributeValuesData Weaponcraft { get; }
    public SaveGameAttributeValuesData Barding { get; }
    public SaveGameAttributeValuesData Haggling { get; }
    public SaveGameAttributeValuesData Lockpick { get; }
    public SaveGameAttributeValuesData Scouting { get; }
    public SaveGameAttributeValuesData Stealth { get; }
    public byte ActorNumber { get; }
    public uint InventoryPointer { get; }
    public ushort CombatDataPointer { get; }
}
