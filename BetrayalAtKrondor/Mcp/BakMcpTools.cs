namespace BetrayalAtKrondor.Mcp;

using System.ComponentModel;
using ModelContextProtocol.Server;
using Spice86.Core.Emulator.Mcp;
using Spice86.Core.Emulator.VM.Breakpoint;
using Spice86.Shared.Emulator.VM.Breakpoint;

/// <summary>
/// MCP tools for BaK reverse engineering that translate between IDA and Spice86 address spaces.
/// Discovered automatically by the SDK via <c>WithToolsFromAssembly</c>.
/// </summary>
[McpServerToolType]
public sealed class BakMcpTools {
    private readonly OverlayAddressTranslator _translator;
    private readonly EmulatorMcpServices _emulator;
    private readonly Lock _lock = new();

    public BakMcpTools(OverlayAddressTranslator translator, EmulatorMcpServices emulator) {
        _translator = translator;
        _emulator = emulator;
    }

    [McpServerTool(Name = "bak_get_current_ida_location")]
    [Description("Get current execution location as an IDA linear address. " + "Translates runtime CS:IP through the overlay map. " +
        "The returned ida_address can be passed directly to IDA MCP tools.")]
    public object GetCurrentIdaLocation() {
        lock (_lock) {
            ushort cs = _emulator.State.CS;
            ushort ip = _emulator.State.IP;
            uint? idaAddr = _translator.PhysicalToIda(cs, ip);
            uint physAddr = (uint)(cs << 4) + ip;

            if (idaAddr == null) {
                return new {
                    physical_address = $"0x{physAddr:X}",
                    runtime_cs = $"0x{cs:X4}",
                    runtime_ip = $"0x{ip:X4}",
                    error = "Unknown segment - not in overlay map"
                };
            }

            return new {
                ida_address = $"0x{idaAddr.Value:X}",
                physical_address = $"0x{physAddr:X}",
                runtime_cs = $"0x{cs:X4}",
                runtime_ip = $"0x{ip:X4}",
                is_overlay = _translator.GetCurrentIdaToRuntimeMap().ContainsValue(cs)
            };
        }
    }

    [McpServerTool(Name = "bak_get_overlay_map")]
    [Description("Get current overlay mapping: which IDA segments are loaded " +
        "at which runtime segments. Shows linear base addresses for both. " + "Only shows currently-loaded overlays.")]
    public object GetOverlayMap() {
        lock (_lock) {
            var map = _translator.GetCurrentIdaToRuntimeMap();

            return new {
                loaded_overlays = map.Select(kvp => new {
                    ida_segment = $"0x{kvp.Key:X4}",
                    ida_base = $"0x{(kvp.Key << 4):X}",
                    runtime_segment = $"0x{kvp.Value:X4}",
                    runtime_base = $"0x{(kvp.Value << 4):X}"
                }).ToArray(),
                count = map.Count,
                relocation_delta = $"0x{OverlayAddressTranslator.RelocationDelta:X}"
            };
        }
    }

    [McpServerTool(Name = "bak_read_memory_ida")]
    [Description("Read memory using an IDA linear address (same format as IDA MCP returns). " +
        "Automatically translates to the correct physical address: " +
        "applies relocation delta for resident segments, overlay map for overlays. " +
        "For overlay code, the overlay must be currently loaded.")]
    public object ReadMemoryIda([Description("IDA linear address (decimal integer, e.g. 0x3D448 = 250952)")] uint idaAddress,
        [Description("Number of bytes to read (1-4096)")] int length) {
        lock (_lock) {
            if (length is <= 0 or > 4096) {
                return new {
                    error = "Length must be between 1 and 4096"
                };
            }

            uint? physical = _translator.IdaToPhysical(idaAddress);
            if (physical == null) {
                return new {
                    error = "Overlay segment not currently loaded in memory",
                    ida_address = $"0x{idaAddress:X}"
                };
            }

            byte[] data = _emulator.Memory.ReadRam((uint)length, physical.Value);

            return new {
                ida_address = $"0x{idaAddress:X}",
                physical_address = $"0x{physical.Value:X}",
                length,
                data = Convert.ToHexString(data)
            };
        }
    }

    [McpServerTool(Name = "bak_set_breakpoint_ida")]
    [Description("Set an execution breakpoint using an IDA linear address. " +
        "For resident segments, the breakpoint is set immediately. " + "For overlay segments, the overlay must be currently loaded.")]
    public object SetBreakpointIda([Description("IDA linear address (decimal integer)")] uint idaAddress) {
        lock (_lock) {
            uint? physical = _translator.IdaToPhysical(idaAddress);

            if (physical != null) {
                var bp = new AddressBreakPoint(BreakPointType.CPU_EXECUTION_ADDRESS, physical.Value,
                    _ => _emulator.PauseHandler.RequestPause($"BaK breakpoint hit at IDA 0x{idaAddress:X}"), false);
                _emulator.BreakpointsManager.ToggleBreakPoint(bp, true);

                return new {
                    status = "active",
                    ida_address = $"0x{idaAddress:X}",
                    physical_address = $"0x{physical.Value:X}"
                };
            }

            return new {
                status = "error",
                ida_address = $"0x{idaAddress:X}",
                message = "Overlay not currently loaded. Cannot set breakpoint."
            };
        }
    }

    [McpServerTool(Name = "bak_translate_address")]
    [Description("Translate between IDA linear and Spice86 physical address spaces. " +
        "Provide ida_address to get physical, or runtime_cs and ip to get IDA.")]
    public object TranslateAddress([Description("IDA linear address to translate to physical (optional)")] uint? idaAddress = null,
        [Description("Runtime CS register value (optional, use with ip)")] int? runtimeCs = null,
        [Description("Runtime IP register value (optional, use with runtime_cs)")] int? ip = null) {
        lock (_lock) {
            if (idaAddress != null) {
                uint? physical = _translator.IdaToPhysical(idaAddress.Value);

                return new {
                    ida_address = $"0x{idaAddress:X}",
                    physical_address = physical != null ? $"0x{physical.Value:X}" : null,
                    is_loaded = physical != null
                };
            }
            if (runtimeCs != null && ip != null) {
                uint? ida = _translator.PhysicalToIda((ushort)runtimeCs.Value, (ushort)ip.Value);
                uint phys = (uint)(runtimeCs.Value << 4) + (uint)ip.Value;

                return new {
                    physical_address = $"0x{phys:X}",
                    ida_address = ida != null ? $"0x{ida.Value:X}" : null,
                    known = ida != null
                };
            }

            return new {
                error = "Provide ida_address, or both runtime_cs and ip"
            };
        }
    }
}