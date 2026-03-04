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
    public StatRange CombatFieldF { get; set; } = new();
    public StatRange CombatField10 { get; set; } = new();
    public StatRange CombatField11 { get; set; } = new();
    public StatRange CombatFieldE { get; set; } = new();
    public ResourceType Type => ResourceType.DAT;
    public string Id { get; }
}
