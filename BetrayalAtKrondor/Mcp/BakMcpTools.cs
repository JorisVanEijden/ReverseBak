namespace BetrayalAtKrondor.Mcp;

using System.ComponentModel;
using ModelContextProtocol.Server;
using Spice86.Core.Emulator.CPU;
using Spice86.Core.Emulator.CPU.CfgCpu.Ast.Builder;
using Spice86.Core.Emulator.CPU.CfgCpu.Ast.Instruction;
using Spice86.Core.Emulator.CPU.CfgCpu.InstructionRenderer;
using Spice86.Core.Emulator.CPU.CfgCpu.Parser;
using Spice86.Core.Emulator.CPU.CfgCpu.ParsedInstruction;
using Spice86.Core.Emulator.Devices.Input.Keyboard;
using Spice86.Core.Emulator.Mcp;
using Spice86.Core.Emulator.VM.Breakpoint;
using Spice86.Shared.Emulator.Memory;
using Spice86.Shared.Emulator.VM.Breakpoint;
using Spice86.Shared.Utils;

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

    [McpServerTool(Name = "bak_write_memory_ida")]
    [Description("Write hex-encoded bytes at an IDA linear address. " +
        "Translates to physical address automatically. " +
        "For overlay segments, the overlay must be currently loaded. Max 4096 bytes.")]
    public object WriteMemoryIda(
        [Description("IDA linear address (decimal integer)")] uint idaAddress,
        [Description("Hex-encoded bytes to write (e.g. 'B80200909090')")] string data) {
        lock (_lock) {
            uint? physical = _translator.IdaToPhysical(idaAddress);
            if (physical == null) {
                return new {
                    error = "Overlay segment not currently loaded in memory",
                    ida_address = $"0x{idaAddress:X}"
                };
            }

            byte[] bytes = Convert.FromHexString(data);
            if (bytes.Length is 0 or > 4096) {
                return new { error = "Data length must be between 1 and 4096 bytes" };
            }

            _emulator.Memory.WriteRam(bytes, physical.Value);

            return new {
                ida_address = $"0x{idaAddress:X}",
                physical_address = $"0x{physical.Value:X}",
                length = bytes.Length,
                success = true
            };
        }
    }

    [McpServerTool(Name = "bak_disasm_ida")]
    [Description("Disassemble x86 real-mode instructions at an IDA linear address. " +
        "Translates to physical address automatically. " +
        "Returns array of instructions with address, hex bytes, and assembly text. " +
        "For overlay segments, the overlay must be currently loaded.")]
    public object DisasmIda(
        [Description("IDA linear address (decimal integer)")] uint idaAddress,
        [Description("Number of instructions to disassemble (1-500)")] int instructionCount) {
        lock (_lock) {
            if (instructionCount is <= 0 or > 500) {
                return new { error = "instructionCount must be between 1 and 500" };
            }

            uint? physical = _translator.IdaToPhysical(idaAddress);
            if (physical == null) {
                return new {
                    error = "Overlay segment not currently loaded in memory",
                    ida_address = $"0x{idaAddress:X}"
                };
            }

            ushort segment = MemoryUtils.ToSegment(physical.Value);
            ushort offset = (ushort)(physical.Value & 0xF);
            SegmentedAddress current = new(segment, offset);

            InstructionParser parser = new(_emulator.Memory, _emulator.State);
            AstBuilder astBuilder = new();
            AstInstructionRenderer renderer = new(AsmRenderingConfig.CreateSpice86Style());
            List<object> lines = new();
            uint memoryLength = (uint)_emulator.Memory.Length;

            for (int i = 0; i < instructionCount; i++) {
                uint physAddr = MemoryUtils.ToPhysicalAddress(current.Segment, current.Offset);
                if (physAddr >= memoryLength) {
                    break;
                }

                CfgInstruction instruction = parser.ParseInstructionAt(current);
                uint endAddr = physAddr + instruction.Length;
                if (endAddr > memoryLength) {
                    break;
                }

                InstructionNode ast = instruction.ToInstructionAst(astBuilder);
                string assembly = ast.Accept(renderer);
                byte[] bytes = _emulator.Memory.ReadRam(instruction.Length, physAddr);
                lines.Add(new {
                    address = $"{current.Segment:X4}:{current.Offset:X4}",
                    bytes = Convert.ToHexString(bytes),
                    assembly
                });
                current = instruction.NextInMemoryAddress;
            }

            return new {
                ida_address = $"0x{idaAddress:X}",
                physical_address = $"0x{physical.Value:X}",
                instructions = lines
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
                string id;
                lock (_emulator.McpBreakpointsLock) {
                    id = _emulator.GetNextBreakpointId().ToString();
                }

                var bp = new AddressBreakPoint(BreakPointType.CPU_EXECUTION_ADDRESS, physical.Value,
                    _ => _emulator.PauseHandler.RequestPause($"BaK breakpoint {id} hit at IDA 0x{idaAddress:X}"), false);
                _emulator.BreakpointsManager.ToggleBreakPoint(bp, true);

                lock (_emulator.McpBreakpointsLock) {
                    _emulator.McpBreakpoints[id] = bp;
                }

                return new {
                    status = "active",
                    id,
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

    [McpServerTool(Name = "bak_press_key")]
    [Description("Press and release a keyboard key in one call. " +
        "Simulates key-down, waits briefly, then key-up. " +
        "Use PcKeyboardKey enum names: Escape, Enter, A-Z, Up, Down, Left, Right, F1-F12, " +
        "Space, Tab, Backspace, Delete, Home, End, PageUp, PageDown.")]
    public object PressKey(
        [Description("Key name from PcKeyboardKey enum (e.g. 'Enter', 'Escape', 'Down')")] string key) {
        Intel8042Controller? controller = _emulator.Intel8042Controller;
        if (controller == null) {
            return new { error = "PS/2 controller is not available" };
        }

        if (!Enum.TryParse(key, true, out PcKeyboardKey parsedKey)) {
            return new { error = $"Invalid key name: '{key}'. Use PcKeyboardKey enum names." };
        }

        controller.KeyboardDevice.EnqueueKeyEvent(parsedKey, true);
        Thread.Sleep(100);
        controller.KeyboardDevice.EnqueueKeyEvent(parsedKey, false);

        return new {
            success = true,
            key = parsedKey.ToString(),
            message = $"Key pressed and released: {parsedKey}"
        };
    }

    [McpServerTool(Name = "bak_run_to_ida")]
    [Description("Resume emulator and run until an IDA linear address is hit, or timeout. " +
        "Sets a temporary breakpoint, resumes execution, waits for the emulator to pause, " +
        "then removes the breakpoint. Returns CPU state and IDA location when hit. " +
        "The emulator will be paused when this tool returns.")]
    public object RunToIda(
        [Description("IDA linear address to run to (decimal integer)")] uint idaAddress,
        [Description("Timeout in milliseconds (default 5000, max 30000)")] int timeoutMs = 5000) {
        timeoutMs = Math.Clamp(timeoutMs, 100, 30000);

        uint? physical = _translator.IdaToPhysical(idaAddress);
        if (physical == null) {
            return new {
                hit = false,
                error = "Overlay segment not currently loaded in memory",
                ida_address = $"0x{idaAddress:X}"
            };
        }

        // Create temporary breakpoint
        bool breakpointHit = false;
        var bp = new AddressBreakPoint(
            BreakPointType.CPU_EXECUTION_ADDRESS,
            physical.Value,
            _ => {
                breakpointHit = true;
                _emulator.PauseHandler.RequestPause($"bak_run_to_ida hit at IDA 0x{idaAddress:X}");
            },
            false);

        _emulator.BreakpointsManager.ToggleBreakPoint(bp, true);

        try {
            // Resume execution
            _emulator.PauseHandler.Resume();

            // Wait for breakpoint hit or timeout
            int elapsed = 0;
            while (!breakpointHit && elapsed < timeoutMs) {
                Thread.Sleep(50);
                elapsed += 50;
            }

            if (!breakpointHit) {
                // Timed out — pause the emulator
                _emulator.PauseHandler.RequestPause("bak_run_to_ida timed out");
                Thread.Sleep(200); // Give it a moment to actually pause

                ushort cs = _emulator.State.CS;
                ushort ip = _emulator.State.IP;
                uint? currentIda = _translator.PhysicalToIda(cs, ip);

                return new {
                    hit = false,
                    timeout = true,
                    timeout_ms = timeoutMs,
                    ida_address = $"0x{idaAddress:X}",
                    stopped_at = currentIda != null ? $"0x{currentIda.Value:X}" : null,
                    runtime_cs = $"0x{cs:X4}",
                    runtime_ip = $"0x{ip:X4}"
                };
            }

            // Breakpoint was hit
            ushort hitCs = _emulator.State.CS;
            ushort hitIp = _emulator.State.IP;
            return new {
                hit = true,
                ida_address = $"0x{idaAddress:X}",
                physical_address = $"0x{physical.Value:X}",
                runtime_cs = $"0x{hitCs:X4}",
                runtime_ip = $"0x{hitIp:X4}",
                cpu_state = new {
                    ax = _emulator.State.AX,
                    bx = _emulator.State.BX,
                    cx = _emulator.State.CX,
                    dx = _emulator.State.DX,
                    si = _emulator.State.SI,
                    di = _emulator.State.DI,
                    bp = _emulator.State.BP,
                    sp = _emulator.State.SP,
                    ds = _emulator.State.DS,
                    es = _emulator.State.ES,
                    ss = _emulator.State.SS
                }
            };
        } finally {
            // Always remove the temporary breakpoint
            _emulator.BreakpointsManager.ToggleBreakPoint(bp, false);
        }
    }
}
