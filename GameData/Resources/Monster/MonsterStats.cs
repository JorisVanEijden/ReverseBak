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
    public StatRange AttackPattern { get; set; } = new();
    public StatRange DefensePattern { get; set; } = new();
    public StatRange MovementPattern { get; set; } = new();
    public StatRange FleeThreshold { get; set; } = new();
    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
