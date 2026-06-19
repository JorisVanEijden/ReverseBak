namespace ResourceExtraction.Extractors;

using GameData.Resources.Config;
using System.IO;

/// <summary>
/// Parses MOVEMENT.DAT — three arrays of three little-endian u16 (one entry per
/// movement/turn speed preset). See <see cref="MovementData"/> for the full
/// semantics and the IDA references (Load_movement.dat @ ovr184:0x71790).
///
/// The "turn size 2" array is the per-step game-time advance; the original loader
/// scales it by 30 (into 2-second clock ticks) before use, so this extractor
/// exposes it as real seconds (raw * 60) — the engine-independent quantity.
/// </summary>
public class MovementExtractor : ExtractorBase<MovementData> {
    // Each raw "turn size 2" unit = 30 game-clock ticks of 2 seconds = 60 seconds.
    private const int SecondsPerRawTimeUnit = 60;

    public override MovementData Extract(string id, Stream resourceStream) {
        using var reader = new BinaryReader(resourceStream);
        var data = new MovementData(id);

        for (int i = 0; i < MovementData.PresetCount; i++) {
            data.StepDistances[i] = reader.ReadUInt16();
        }
        for (int i = 0; i < MovementData.PresetCount; i++) {
            data.TurnAngles[i] = reader.ReadUInt16();
        }
        for (int i = 0; i < MovementData.PresetCount; i++) {
            data.SecondsPerStep[i] = reader.ReadUInt16() * SecondsPerRawTimeUnit;
        }

        return data;
    }
}
