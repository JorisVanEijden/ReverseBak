namespace ResourceExtraction.Extractors;

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

        stats.Health = ReadStatRange(reader);
        stats.Stamina = ReadStatRange(reader);
        stats.Speed = ReadStatRange(reader);
        stats.Strength = ReadStatRange(reader);
        stats.AccuracyCrossbow = ReadStatRange(reader);
        stats.AccuracyMelee = ReadStatRange(reader);
        stats.AccuracyCasting = ReadStatRange(reader);
        stats.Defense = ReadStatRange(reader);
        stats.CombatFieldF = ReadStatRange(reader);
        stats.CombatField10 = ReadStatRange(reader);
        stats.CombatField11 = ReadStatRange(reader);
        stats.CombatFieldE = ReadStatRange(reader);

        return stats;
    }

    private static StatRange ReadStatRange(BinaryReader reader)
    {
        // IMPORTANT: File stores max first, then min (verified against disasm at 0x6a3c3)
        ushort max = reader.ReadUInt16();
        ushort min = reader.ReadUInt16();
        return new StatRange { Max = max, Min = min };
    }
}
