namespace BetrayalAtKrondor.Mcp;

/// <summary>
/// Translates between IDA linear addresses and Spice86 physical addresses.
/// Handles two types of address differences:
///
/// 1. RELOCATION DELTA (all resident segments):
///    IDA places the EXE at a virtual base (seg001 at 0x1000), while Spice86 loads
///    at a runtime base (seg001 at 0x017D). The delta is constant:
///      physical = ida_linear - relocationDelta
///      ida      = physical + relocationDelta
///
/// 2. OVERLAY MAPPING (overlay segments only):
///    Borland's overlay manager loads segments at varying runtime addresses.
///    The mapping is tracked dynamically via RecordOvrChange hook.
///    Translation uses per-segment base address substitution.
/// </summary>
public sealed class OverlayAddressTranslator {
    private readonly Lock _lock = new();

    // Fixed delta: IDA linear - physical linear (for resident segments).
    // IDA entry point is at linear 0x10000 (seg001:0000).
    // Spice86 loads KRONDOR.EXE at physical linear 0x17D0 (segment 0x017D).
    // Delta = 0x10000 - 0x17D0 = 0xE830.
    private const uint RelocationDeltaConst = 0xE830;

    // Bidirectional overlay segment mapping (updated at runtime)
    private readonly Dictionary<ushort, ushort> _runtimeToIda = new();
    private readonly Dictionary<ushort, ushort> _idaToRuntime = new();

    // IDA overlay segment bases sorted by linear address for FindIdaSegment lookup
    private readonly SortedList<uint, ushort> _idaSegmentBases = new();

    // IDA linear address where overlay segments begin (ovr121 start)
    private const uint OverlayStartLinear = 0x3FF70;

    private const uint OverlayEndLinearExclusive = 0x7B000;

    public OverlayAddressTranslator() {
        // Pre-populate IDA overlay segment bases from StubSegments
        foreach (ushort idaSeg in StubSegments.IdaCodeToIdaStub.Keys) {
            _idaSegmentBases[(uint)(idaSeg << 4)] = idaSeg;
        }
    }

    /// <summary>Called by RecordOvrChange hook when overlay manager loads a segment.</summary>
    public void RecordMapping(ushort runtimeSegment, ushort idaSegment) {
        lock (_lock) {
            // Clean up old mappings in both directions
            if (_idaToRuntime.TryGetValue(idaSegment, out ushort oldRuntime)) {
                _runtimeToIda.Remove(oldRuntime);
            }
            if (_runtimeToIda.TryGetValue(runtimeSegment, out ushort oldIda)) {
                _idaToRuntime.Remove(oldIda);
            }

            _runtimeToIda[runtimeSegment] = idaSegment;
            _idaToRuntime[idaSegment] = runtimeSegment;
        }
    }

    /// <summary>
    /// Translate IDA linear address to Spice86 physical address.
    /// Returns null if the address is in an overlay segment that isn't currently loaded.
    /// </summary>
    public uint? IdaToPhysical(uint idaLinear) {
        if (idaLinear < OverlayStartLinear) {
            // Resident segment: apply fixed relocation delta
            return idaLinear - RelocationDeltaConst;
        }

        if (idaLinear >= OverlayEndLinearExclusive) {
            return null;
        }

        // Overlay segment: find which IDA segment, then use overlay map
        ushort? idaSeg = FindIdaSegment(idaLinear);
        if (idaSeg == null) {
            return null;
        }

        lock (_lock) {
            if (_idaToRuntime.TryGetValue(idaSeg.Value, out ushort runtimeSeg)) {
                uint idaBase = (uint)(idaSeg.Value << 4);
                uint runtimeBase = (uint)(runtimeSeg << 4);
                return idaLinear - idaBase + runtimeBase;
            }
            return null; // Overlay not currently loaded
        }
    }

    /// <summary>
    /// Translate Spice86 physical address (as CS:IP) to IDA linear address.
    /// Returns null if the runtime segment is an unknown overlay.
    /// </summary>
    public uint? PhysicalToIda(ushort runtimeCs, ushort ip) {
        uint physLinear = (uint)(runtimeCs << 4) + ip;

        // First check overlay map: overlays can be loaded at any runtime address
        lock (_lock) {
            if (_runtimeToIda.TryGetValue(runtimeCs, out ushort idaSeg)) {
                return (uint)(idaSeg << 4) + ip;
            }
        }

        // Not in overlay map: treat as resident segment, apply delta
        uint idaLinear = physLinear + RelocationDeltaConst;
        return idaLinear < OverlayStartLinear ? idaLinear : null;
    }

    /// <summary>Returns the relocation delta (IDA - physical) in bytes.</summary>
    public static uint RelocationDelta {
        get => RelocationDeltaConst;
    }

    /// <summary>Returns snapshot of current overlay map for diagnostics.</summary>
    public Dictionary<ushort, ushort> GetCurrentIdaToRuntimeMap() {
        lock (_lock) {
            return new Dictionary<ushort, ushort>(_idaToRuntime);
        }
    }

    /// <summary>Find which IDA overlay segment a linear address belongs to.</summary>
    private ushort? FindIdaSegment(uint idaLinear) {
        ushort? result = null;
        foreach (KeyValuePair<uint, ushort> kvp in _idaSegmentBases) {
            if (kvp.Key > idaLinear) {
                break;
            }
            result = kvp.Value;
        }
        return result;
    }
}
