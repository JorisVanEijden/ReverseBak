namespace ResourceExtraction.Extractors.Exe;

using GameData.Resources.Combat;
using ResourceExtraction.Extractors;
using System.IO;

/// <summary>
/// Makes <see cref="CombatAffinityReader"/> reachable through the resource system.
/// </summary>
/// <remarks>
/// <b>The reader was complete and unreachable, and nothing could see that.</b>
/// <c>ExtractorFactoryCoverageTests</c> enumerates concrete <c>ExtractorBase&lt;T&gt;</c> subclasses
/// and asserts each is registered — but the reader is a static class, not an extractor, so it was
/// invisible to the very test written to stop exactly this. The runtime asked for
/// <see cref="CombatAffinityTables"/>, <c>ExtractorFactory</c> threw "No extractor found", and
/// <c>HotspotService.LoadOrNull</c> swallowed it into a null.
///
/// <para>Measured 2026-08-29: seven of those exceptions in one Editor log, one per encounter
/// started that day. So <c>_affinity</c> was null in every fight, and the AI flee thresholds it
/// carries were never supplied to <c>MonsterTurnResolver</c> — monsters have been fleeing on
/// whatever the null path does instead of on the shipped table.</para>
///
/// <para>A thin wrapper by design: the reading, the IDA-address arithmetic and the placement check
/// that refuses to emit a guess all stay in <see cref="CombatAffinityReader"/>, which is also
/// callable directly by the dump tooling.</para>
/// </remarks>
public class CombatAffinityExtractor : ExtractorBase<CombatAffinityTables> {
    public override CombatAffinityTables Extract(string id, Stream resourceStream) {
        using var memory = new MemoryStream();
        resourceStream.CopyTo(memory);
        return CombatAffinityReader.Read(memory.ToArray(), id);
    }
}
