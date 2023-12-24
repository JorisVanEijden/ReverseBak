namespace GameData;

[Flags]
public enum ActorAttributeFlag {
    Health = 1 << ActorAttribute.Health,
    Stamina = 1 << ActorAttribute.Stamina,
    Speed = 1 << ActorAttribute.Speed,
    Strength = 1 << ActorAttribute.Strength,
    Defense = 1 << ActorAttribute.Defense,
    AccuracyCrossbow = 1 << ActorAttribute.AccuracyCrossbow,
    AccuracyMelee = 1 << ActorAttribute.AccuracyMelee,
    AccuracyCasting = 1 << ActorAttribute.AccuracyCasting,
    Assessment = 1 << ActorAttribute.Assessment,
    ArmorCraft = 1 << ActorAttribute.ArmorCraft,
    WeaponCraft = 1 << ActorAttribute.WeaponCraft,
    Barding = 1 << ActorAttribute.Barding,
    Haggling = 1 << ActorAttribute.Haggling,
    LockPicking = 1 << ActorAttribute.LockPicking,
    Scouting = 1 << ActorAttribute.Scouting,
    Stealth = 1 << ActorAttribute.Stealth
}