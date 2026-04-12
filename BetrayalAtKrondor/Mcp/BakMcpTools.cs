namespace BetrayalAtKrondor.Mcp;

using System.ComponentModel;
using System.Runtime.InteropServices;
using ModelContextProtocol.Server;
using SkiaSharp;
using Spice86.Core.Emulator.CPU;
using Spice86.Core.Emulator.CPU.CfgCpu.Ast.Builder;
using Spice86.Core.Emulator.CPU.CfgCpu.Ast.Instruction;
using Spice86.Core.Emulator.CPU.CfgCpu.InstructionRenderer;
using Spice86.Core.Emulator.CPU.CfgCpu.Parser;
using Spice86.Core.Emulator.CPU.CfgCpu.ParsedInstruction;
using Spice86.Core.Emulator.Devices.Input.Keyboard;
using Spice86.Core.Emulator.Mcp;
using Spice86.Core.Emulator.VM;
using Spice86.Core.Emulator.VM.Breakpoint;
using Spice86.Shared.Emulator.Memory;
using Spice86.Shared.Emulator.VM.Breakpoint;
using Spice86.Shared.Utils;
using Spice86.ViewModels.Services;

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

    /// <summary>
    /// Resolved address with both physical and (optional) IDA representations.
    /// </summary>
    private record ResolvedAddress(uint PhysicalAddress, uint? IdaAddress) {
        public string IdaDisplay => IdaAddress.HasValue ? $"0x{IdaAddress.Value:X}" : "N/A";
        public string PhysDisplay => $"0x{PhysicalAddress:X}";
    }

    /// <summary>
    /// Parse a flexible address string into a resolved physical + IDA address pair.
    /// Delegates seg:off and register parsing to Spice86's <see cref="AddressAndValueParser"/>.
    /// Supported formats:
    ///   "0x3fdf6"   — IDA linear hex (translated via overlay map)
    ///   "249846"    — IDA linear decimal
    ///   "d5e3:86d2" — seg:off → physical directly
    ///   "DS:123c"   — register-relative (DS, CS, ES, SS, or any register pair)
    /// </summary>
    private (ResolvedAddress? Address, string? Error) ResolveAddress(string address) {
        address = address.Trim();

        // Try seg:off first (contains ':') — resolves to physical address directly
        SegmentedAddress? segOff = AddressAndValueParser.ParseSegmentedAddress(address, _emulator.State);
        if (segOff != null) {
            uint physical = segOff.Value.Linear;
            uint? ida = _translator.PhysicalToIda(segOff.Value.Segment, segOff.Value.Offset);
            return (new ResolvedAddress(physical, ida), null);
        }

        // Not seg:off — treat as IDA linear address (hex or decimal)
        uint? hexValue = AddressAndValueParser.ParseHex(address);
        if (hexValue != null) {
            uint? phys = _translator.IdaToPhysical(hexValue.Value);
            if (phys == null) {
                return (null, $"Overlay not loaded for IDA 0x{hexValue.Value:X}");
            }
            return (new ResolvedAddress(phys.Value, hexValue.Value), null);
        }

        // Try plain decimal
        if (uint.TryParse(address, out uint decValue)) {
            uint? phys = _translator.IdaToPhysical(decValue);
            if (phys == null) {
                return (null, $"Overlay not loaded for IDA 0x{decValue:X}");
            }
            return (new ResolvedAddress(phys.Value, decValue), null);
        }

        return (null, $"Cannot parse '{address}'. Use hex (0x3fdf6), decimal, or seg:off (DS:1234).");
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
    [Description("Read memory at an address. Accepts IDA hex (\"0x3fdf6\"), decimal, " +
        "seg:off (\"d5e3:86d2\"), or register-relative (\"dseg:123c\", \"cs:0505\").")]
    public object ReadMemoryIda(
        [Description("Address: IDA hex/dec, seg:off, or dseg:offset")] string address,
        [Description("Number of bytes to read (1-4096)")] int length) {
        lock (_lock) {
            if (length is <= 0 or > 4096) {
                return new { error = "Length must be between 1 and 4096" };
            }

            var (resolved, error) = ResolveAddress(address);
            if (resolved == null) {
                return new { error };
            }

            byte[] data = _emulator.Memory.ReadRam((uint)length, resolved.PhysicalAddress);

            return new {
                ida_address = resolved.IdaDisplay,
                physical_address = resolved.PhysDisplay,
                length,
                data = Convert.ToHexString(data)
            };
        }
    }

    [McpServerTool(Name = "bak_write_memory_ida")]
    [Description("Write hex-encoded bytes at an address. Accepts IDA hex (\"0x3fdf6\"), " +
        "decimal, seg:off (\"d5e3:86d2\"), or register-relative (\"DS:123c\"). Max 4096 bytes.")]
    public object WriteMemoryIda(
        [Description("Address: IDA hex/dec, seg:off, or DS:offset")] string address,
        [Description("Hex-encoded bytes to write (e.g. 'B80200909090')")] string data) {
        lock (_lock) {
            var (resolved, error) = ResolveAddress(address);
            if (resolved == null) {
                return new { error };
            }

            byte[] bytes = Convert.FromHexString(data);
            if (bytes.Length is 0 or > 4096) {
                return new { error = "Data length must be between 1 and 4096 bytes" };
            }

            _emulator.Memory.WriteRam(bytes, resolved.PhysicalAddress);

            return new {
                ida_address = resolved.IdaDisplay,
                physical_address = resolved.PhysDisplay,
                length = bytes.Length,
                success = true
            };
        }
    }

    [McpServerTool(Name = "bak_disasm_ida")]
    [Description("Disassemble x86 real-mode instructions at an address. " +
        "Accepts IDA hex (\"0x3fdf6\"), decimal, seg:off, or register-relative (\"CS:IP\").")]
    public object DisasmIda(
        [Description("Address: IDA hex/dec, seg:off, or DS:offset")] string address,
        [Description("Number of instructions to disassemble (1-500)")] int instructionCount) {
        lock (_lock) {
            if (instructionCount is <= 0 or > 500) {
                return new { error = "instructionCount must be between 1 and 500" };
            }

            var (resolved, error) = ResolveAddress(address);
            if (resolved == null) {
                return new { error };
            }

            ushort segment = MemoryUtils.ToSegment(resolved.PhysicalAddress);
            ushort offset = (ushort)(resolved.PhysicalAddress & 0xF);
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
                ida_address = resolved.IdaDisplay,
                physical_address = resolved.PhysDisplay,
                instructions = lines
            };
        }
    }

    [McpServerTool(Name = "bak_set_breakpoint_ida")]
    [Description("Set an execution breakpoint at an address. " +
        "Accepts IDA hex (\"0x3fdf6\"), decimal, seg:off, or register-relative.")]
    public object SetBreakpointIda(
        [Description("Address: IDA hex/dec, seg:off, or DS:offset")] string address) {
        lock (_lock) {
            var (resolved, error) = ResolveAddress(address);
            if (resolved == null) {
                return new { status = "error", message = error };
            }

            string id;
            lock (_emulator.McpBreakpointsLock) {
                id = _emulator.GetNextBreakpointId().ToString();
            }

            var bp = new AddressBreakPoint(BreakPointType.CPU_EXECUTION_ADDRESS, resolved.PhysicalAddress,
                _ => _emulator.PauseHandler.RequestPause($"BaK breakpoint {id} hit at {resolved.IdaDisplay}"), false);
            _emulator.BreakpointsManager.ToggleBreakPoint(bp, true);

            lock (_emulator.McpBreakpointsLock) {
                _emulator.McpBreakpoints[id] = bp;
            }

            return new {
                status = "active",
                id,
                ida_address = resolved.IdaDisplay,
                physical_address = resolved.PhysDisplay
            };
        }
    }

    [McpServerTool(Name = "bak_translate_address")]
    [Description("Translate any address format to IDA + physical. " +
        "Accepts IDA hex (\"0x3fdf6\"), decimal, seg:off (\"d5e3:86d2\"), or register-relative (\"DS:1234\", \"CS:IP\").")]
    public object TranslateAddress(
        [Description("Address in any supported format")] string address) {
        lock (_lock) {
            var (resolved, error) = ResolveAddress(address);
            if (resolved == null) {
                return new { error };
            }

            return new {
                ida_address = resolved.IdaDisplay,
                physical_address = resolved.PhysDisplay,
                is_loaded = true
            };
        }
    }

    [McpServerTool(Name = "bak_press_key")]
    [Description("Press and release a keyboard key in one call. " +
        "Posts key-down then key-up through the emulator input queue, resumes execution " +
        "long enough for the game's INT 9 ISR to run, then returns. " +
        "Use PcKeyboardKey enum names: Escape, Enter, A-Z, Up, Down, Left, Right, F1-F12, " +
        "Space, Tab, Backspace, Delete, Home, End, PageUp, PageDown.")]
    public object PressKey(
        [Description("Key name from PcKeyboardKey enum (e.g. 'Enter', 'Escape', 'Down')")] string key) {
        Intel8042Controller? controller = _emulator.Intel8042Controller;
        if (controller == null) {
            return new { error = "PS/2 controller is not available" };
        }

        InputEventHub? hub = _emulator.InputEventHub;
        if (hub == null) {
            return new { error = "InputEventHub is not wired" };
        }

        if (!Enum.TryParse(key, true, out PcKeyboardKey parsedKey)) {
            return new { error = $"Invalid key name: '{key}'. Use PcKeyboardKey enum names." };
        }

        hub.PostToEmulatorThread(() => controller.KeyboardDevice.EnqueueKeyEvent(parsedKey, true));
        hub.PostToEmulatorThread(() => controller.KeyboardDevice.EnqueueKeyEvent(parsedKey, false));

        _emulator.PauseHandler.Resume();
        Thread.Sleep(150);

        return new {
            success = true,
            key = parsedKey.ToString(),
            message = $"Key pressed and released: {parsedKey}"
        };
    }

    [McpServerTool(Name = "bak_run_to_ida")]
    [Description("Resume emulator and run until an address is hit, or timeout. " +
        "Sets a temporary breakpoint, resumes execution, waits for hit, removes breakpoint. " +
        "Accepts IDA hex (\"0x3fdf6\"), decimal, seg:off, or register-relative. " +
        "The emulator will be paused when this tool returns.")]
    public object RunToIda(
        [Description("Address: IDA hex/dec, seg:off, or DS:offset")] string address,
        [Description("Timeout in milliseconds (default 5000, max 30000)")] int timeoutMs = 5000) {
        timeoutMs = Math.Clamp(timeoutMs, 100, 30000);

        var (resolved, error) = ResolveAddress(address);
        if (resolved == null) {
            return new { hit = false, error };
        }

        // Create temporary breakpoint
        bool breakpointHit = false;
        var bp = new AddressBreakPoint(
            BreakPointType.CPU_EXECUTION_ADDRESS,
            resolved.PhysicalAddress,
            _ => {
                breakpointHit = true;
                _emulator.PauseHandler.RequestPause($"bak_run_to_ida hit at {resolved.IdaDisplay}");
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
                    ida_address = resolved.IdaDisplay,
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
                ida_address = resolved.IdaDisplay,
                physical_address = resolved.PhysDisplay,
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

    [McpServerTool(Name = "bak_mouse_click")]
    [Description("Click the mouse at a screen position. Writes x, y to the game's mouse position " +
        "globals (MouseHorizontal, MouseVertical), sets MouseButtonState, lets the emulator run " +
        "for ~100 ms so the game processes the click, then clears the button state. " +
        "Coordinates are screen pixels (320x200).")]
    public object MouseClick(
        [Description("Screen X coordinate (0-319)")] int x,
        [Description("Screen Y coordinate (0-199)")] int y,
        [Description("Mouse button: 'left' or 'right' (default: 'left')")] string button = "left") {
        // IDA addresses for BaK mouse globals (dseg, always resident)
        const uint idaMouseX = 0x3CE0C;      // MouseHorizontal (word)
        const uint idaMouseY = 0x3CE0E;      // MouseVertical   (word)
        const uint idaMouseButton = 0x3CFB7;  // MouseButtonState (byte)

        uint? physX = _translator.IdaToPhysical(idaMouseX);
        uint? physY = _translator.IdaToPhysical(idaMouseY);
        uint? physButton = _translator.IdaToPhysical(idaMouseButton);

        if (physX == null || physY == null || physButton == null) {
            return new { error = "Could not translate mouse global addresses" };
        }

        byte buttonBit = button.ToLowerInvariant() == "right" ? (byte)0x02 : (byte)0x01;

        // BaK mouse globals use 4x screen coordinates (LoadMousePosition divides by 4)
        _emulator.Memory.UInt16[physX.Value] = (ushort)(x * 4);
        _emulator.Memory.UInt16[physY.Value] = (ushort)(y * 4);
        _emulator.Memory.WriteRam(new[] { buttonBit }, physButton.Value);

        // Let the emulator run so the game sees the click
        _emulator.PauseHandler.Resume();
        Thread.Sleep(100);

        // Button up
        _emulator.Memory.WriteRam(new byte[] { 0 }, physButton.Value);

        return new {
            success = true,
            x,
            y,
            button,
            message = $"Mouse {button}-clicked at ({x}, {y})"
        };
    }

    private const string ScreenshotPath = "/tmp/spice86-screenshot.png";

    [McpServerTool(Name = "bak_screenshot")]
    [Description("Take a screenshot and save to /tmp/spice86-screenshot.png. " +
        "Returns the file path — use Read tool on it to view the image. " +
        "Much simpler than the base screenshot tool for CLI workflows.")]
    public object BakScreenshot() {
        int width = _emulator.VgaRenderer.Width;
        int height = _emulator.VgaRenderer.Height;
        if (width <= 0 || height <= 0) {
            return new { error = "No frame available yet" };
        }

        uint[] buffer = new uint[width * height];
        _emulator.VgaRenderer.CopyLastFrame(buffer);

        byte[] bytes = new byte[buffer.Length * 4];
        Buffer.BlockCopy(buffer, 0, bytes, 0, bytes.Length);

        SKImageInfo imageInfo = new(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        using SKBitmap bitmap = new(imageInfo);
        Marshal.Copy(bytes, 0, bitmap.GetPixels(), bytes.Length);

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData pngData = image.Encode(SKEncodedImageFormat.Png, 100);
        if (pngData == null) {
            return new { error = "Failed to encode PNG" };
        }

        using FileStream fs = File.Open(ScreenshotPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        pngData.SaveTo(fs);

        return new {
            path = ScreenshotPath,
            width,
            height
        };
    }

}
