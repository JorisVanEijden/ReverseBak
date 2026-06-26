namespace GameData.Resources.Monster;

using GameData.Resources;

public class MonsterStats : IResource
{
    public MonsterStats(string id) { Id = id; }
    public int CreatureId { get; set; }
    public StatRange Health { get; set; } = new();
    public StatRange Stamina { get; set; } = new();
    public StatRange Speed { get; set; } = new();
    public StatRange Strength { get; set; } = new();
    public StatRange AccuracyCrossbow { get; set; } = new();
    public StatRange AccuracyMelee { get; set; } = new();
    public StatRange AccuracyCasting { get; set; } = new();
    public StatRange Defense { get; set; } = new();
    // The three "pattern" fields are AI-behavior priority-table indices, selected by the
    // monster's combat capability (canCastSpells > canShootCrossbow > melee), NOT an
    // attack/defense split. Verified in IDA via monster_combatTurn (ovr169 0x64501).
    // See docs/FileFormats/MONST.DAT.md.
    public StatRange SpellcastPattern { get; set; } = new();   // file field 8 (was "AttackPattern")  — caster AI, gated by canCastSpells
    public StatRange CrossbowPattern { get; set; } = new();    // file field 9 (was "DefensePattern") — ranged AI, gated by combat_canShootCrossbow
    public StatRange MeleeMovePattern { get; set; } = new();   // file field 10 (was "MovementPattern") — default melee/move AI
    public StatRange FleeThreshold { get; set; } = new();
    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
