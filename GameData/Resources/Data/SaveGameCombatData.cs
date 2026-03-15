namespace GameData.Resources.Data;

public class SaveGameCombatData {
    public SaveGameCombatData(
        ushort targetActorPointer,
        short creatureType,
        byte xOnGrid,
        byte yOnGrid,
        byte targetXOnGrid,
        byte targetYOnGrid,
        byte combatStatus,
        byte animEffectType,
        short activeSpellEffectSlot,
        byte unusedPadding,
        byte animDurationTimer,
        byte monsterSpellAbility,
        byte meleeAttackType,
        byte rangedAttackType,
        byte movementAiType,
        sbyte preferredArrowType,
        byte lastSpellSymbolFile,
        byte floatingDamageValue,
        sbyte floatingDamageTimer
    ) {
        TargetActorPointer = targetActorPointer;
        CreatureType = creatureType;
        XOnGrid = xOnGrid;
        YOnGrid = yOnGrid;
        TargetXOnGrid = targetXOnGrid;
        TargetYOnGrid = targetYOnGrid;
        CombatStatus = combatStatus;
        AnimEffectType = animEffectType;
        ActiveSpellEffectSlot = activeSpellEffectSlot;
        UnusedPadding = unusedPadding;
        AnimDurationTimer = animDurationTimer;
        MonsterSpellAbility = monsterSpellAbility;
        MeleeAttackType = meleeAttackType;
        RangedAttackType = rangedAttackType;
        MovementAiType = movementAiType;
        PreferredArrowType = preferredArrowType;
        LastSpellSymbolFile = lastSpellSymbolFile;
        FloatingDamageValue = floatingDamageValue;
        FloatingDamageTimer = floatingDamageTimer;
    }

    public ushort TargetActorPointer { get; }
    public short CreatureType { get; }
    public byte XOnGrid { get; }
    public byte YOnGrid { get; }
    public byte TargetXOnGrid { get; }
    public byte TargetYOnGrid { get; }
    public byte CombatStatus { get; }
    public byte AnimEffectType { get; }
    public short ActiveSpellEffectSlot { get; }
    public byte UnusedPadding { get; }
    public byte AnimDurationTimer { get; }
    public byte MonsterSpellAbility { get; }
    public byte MeleeAttackType { get; }
    public byte RangedAttackType { get; }
    public byte MovementAiType { get; }
    public sbyte PreferredArrowType { get; }
    public byte LastSpellSymbolFile { get; }
    public byte FloatingDamageValue { get; }
    public sbyte FloatingDamageTimer { get; }
}
