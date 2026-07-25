namespace ResourceExtraction.Extractors;

using GameData.Resources.Content;
using GameData.Resources.Monster;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class MonsterStatsExtractor : ExtractorBase<MonsterStats>
{
    public override MonsterStats Extract(string id, Stream resourceStream)
    {
        using var reader = new BinaryReader(resourceStream, Encoding.GetEncoding(DosCodePage));
        var stats = new MonsterStats(id);

        var match = Regex.Match(id, @"(\d+)", RegexOptions.IgnoreCase);
        if (match.Success)
            stats.CreatureId = int.Parse(match.Groups[1].Value);
        stats.CreatureKey = ContentKey.ForBase("mnames", stats.CreatureId); // -> base:mnames:<n> (same space as EnemySlot/affinity)

        stats.Health = ReadStatRange(reader);
        stats.Stamina = ReadStatRange(reader);
        stats.Speed = ReadStatRange(reader);
        stats.Strength = ReadStatRange(reader);
        stats.AccuracyCrossbow = ReadStatRange(reader);
        stats.AccuracyMelee = ReadStatRange(reader);
        stats.AccuracyCasting = ReadStatRange(reader);
        stats.Defense = ReadStatRange(reader);
        stats.SpellcastPattern = ReadStatRange(reader);   // caster AI personality (gated by canCastSpells)
        stats.CrossbowPattern = ReadStatRange(reader);     // ranged AI personality (gated by combat_canShootCrossbow)
        stats.MeleeMovePattern = ReadStatRange(reader);    // default melee/move AI personality
        stats.FleeThreshold = ReadStatRange(reader);

        return stats;
    }

    private static StatRange ReadStatRange(BinaryReader reader)
    {
        // File stores min first, then max (verified against disasm at sub_ovr174_0:
        // range = max - min, result = min + random % range)
        ushort min = reader.ReadUInt16();
        ushort max = reader.ReadUInt16();
        return new StatRange { Min = min, Max = max };
    }
}
