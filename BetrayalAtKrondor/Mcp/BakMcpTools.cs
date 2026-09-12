namespace BetrayalAtKrondor.Mcp;

using System.Collections.Generic;
using System.ComponentModel;
using ModelContextProtocol.Server;
using Spice86.Core.Emulator.CPU;
using Spice86.Core.Emulator.CPU.CfgCpu.Ast.Instruction;
using Spice86.Core.Emulator.CPU.CfgCpu.InstructionRenderer;
using Spice86.Core.Emulator.CPU.CfgCpu.Parser;
using Spice86.Core.Emulator.CPU.CfgCpu.ParsedInstruction;
using Spice86.Core.Emulator.Mcp;
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
    private readonly SequentialIdAllocator _idAllocator = new();

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

            InstructionParser parser = new(_emulator.Memory, _emulator.State, _idAllocator);
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

                InstructionNode ast = instruction.DisplayAst;
                string assembly = ast.Accept(renderer);
                byte[] bytes = _emulator.Memory.ReadRam(instruction.Length, physAddr);
                lines.Add(new {
                    address = $"{current.Segment:X4}:{current.Offset:X4}",
                    bytes = Convert.ToHexString(bytes),
                    assembly
                });
                current = instruction.NextInMemoryAddress32.ToSegmentedAddress();
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

    [McpServerTool(Name = "bak_find_function")]
    [Description("Find a routine in memory by its opening bytes and return the seg:off " +
        "bak_call_function needs. Give it a signature taken from IDA at the function's address and " +
        "the function's OFFSET WITHIN ITS IDA SEGMENT (ida - idaapi.get_segm_base(seg)). Requires " +
        "exactly one match: several means the signature is too short, none means the overlay is not " +
        "resident right now. Use this INSTEAD of the overlay map, whose is_loaded goes stale.")]
    public object FindFunction(
        [Description("Opening bytes as hex, e.g. '558BEC83EC02568B7606'. 16+ bytes keeps it unique.")]
        string signature,
        [Description("The function's offset within its IDA segment — ida minus the segment base.")]
        int segmentOffset) {
        string hex = signature.Replace(" ", "").Replace("0x", "");
        if (hex.Length < 8 || hex.Length % 2 != 0) {
            return new { error = "signature must be an even number of hex digits, at least 4 bytes" };
        }

        var pattern = new byte[hex.Length / 2];
        for (var i = 0; i < pattern.Length; i++) {
            if (!byte.TryParse(hex.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber,
                    null, out pattern[i])) {
                return new { error = $"'{hex}' is not hex" };
            }
        }

        var hits = new List<uint>();
        const uint conventional = 0x100000;
        for (uint addr = 0; addr + (uint)pattern.Length <= conventional && hits.Count <= 8; addr++) {
            if (_emulator.Memory.UInt8[addr] != pattern[0]) {
                continue;
            }
            var match = true;
            for (var i = 1; i < pattern.Length; i++) {
                if (_emulator.Memory.UInt8[addr + (uint)i] != pattern[i]) {
                    match = false;
                    break;
                }
            }
            if (match) {
                hits.Add(addr);
            }
        }

        if (hits.Count == 0) {
            return new { found = false,
                error = "not in memory — the overlay holding it is not resident. Put the game in a "
                    + "state that uses it and look again." };
        }
        if (hits.Count > 1) {
            return new { found = false, matches = hits.Count,
                error = "signature is not unique; take more bytes from IDA." };
        }

        uint physical = hits[0];
        if (physical < (uint)segmentOffset || (physical - (uint)segmentOffset) % 16 != 0) {
            return new { found = false,
                error = $"0x{physical:X} minus offset 0x{segmentOffset:X} is not paragraph-aligned — "
                    + "the segment offset does not belong to this signature." };
        }

        ushort segment = (ushort)((physical - (uint)segmentOffset) >> 4);
        return new {
            found = true,
            address = $"{segment:X4}:{segmentOffset:X4}",
            segment = $"0x{segment:X4}",
            offset = $"0x{segmentOffset:X4}",
            physical = $"0x{physical:X}"
        };
    }

    [McpServerTool(Name = "bak_call_function")]
    [Description("SPIKE — TREAT A CALL AS THE LAST THING YOU DO BEFORE RESTARTING THE EMULATOR. " +
        "Only the FIRST call of a session reaches the routine (the CfgCpu honours a CS:IP change on " +
        "its cold path only), and the cleanup afterwards is not reliable: the CPU can be left parked " +
        "in the call stub, where a later resume kills the process. Call it, read the answer, restart. " +
        "Call a function INSIDE the running game and return what it answers. Pushes the " +
        "given 16-bit words right-to-left (Borland cdecl), pushes the CURRENT CS:IP as the far " +
        "return address, jumps to the target, runs until it returns, then restores every register. " +
        "Address MUST be seg:off (e.g. '3BEB:0430') — a far routine uses near jumps inside its own " +
        "segment, so a made-up segment executes the right bytes and then jumps to the wrong place. " +
        "ONLY CALL PURE FUNCTIONS: registers are restored, memory the routine wrote is not.")]
    public object CallFunction(
        [Description("Target as seg:off, e.g. '3BEB:0430'. Get one from bak_translate_address.")]
        string address,
        [Description("Comma-separated 16-bit arguments, in SOURCE order; they are pushed right-to-left.")]
        string args = "",
        [Description("Data segment for the call, hex or decimal. Empty leaves DS as it is.")]
        string ds = "",
        [Description("Timeout in milliseconds (default 5000, max 30000)")] int timeoutMs = 5000,
        [Description("Skip the 'push bp' prologue check. Only for a deliberate non-function target.")]
        bool force = false) {
        timeoutMs = Math.Clamp(timeoutMs, 100, 30000);

        SegmentedAddress? target = AddressAndValueParser.ParseSegmentedAddress(address, _emulator.State);
        if (target == null) {
            return new { error = $"'{address}' is not seg:off. A far routine needs its real segment." };
        }

        var words = new List<ushort>();
        foreach (string part in args.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
            string t = part.Trim();
            uint? parsed = t.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? AddressAndValueParser.ParseHex(t)
                : (uint.TryParse(t, out uint d) ? d : (int.TryParse(t, out int sd) ? (uint)(ushort)sd : null));
            if (parsed == null) {
                return new { error = $"cannot parse argument '{t}'" };
            }
            words.Add((ushort)parsed.Value);
        }

        // *** CHECK YOU ARE POINTING AT A FUNCTION. *** Every Borland far routine in this binary
        // opens `push bp` (0x55). An OVERLAY moves, and the translator's own `is_loaded` can be
        // STALE — measured 2026-09-12: bak_translate_address answered a physical address for
        // ovr177, and the bytes there were somebody else's code, because the overlay had since been
        // swapped out. Without this guard that call ran whatever happened to be resident, returned
        // a plausible-looking 95 twice for two different inputs, and the second one wandered until
        // it timed out. One byte of checking is cheaper than that.
        uint targetPhysical = (uint)(target.Value.Segment << 4) + target.Value.Offset;
        byte first = _emulator.Memory.UInt8[targetPhysical];
        if (first != 0x55 && !force) {
            return new {
                error = $"{target.Value.Segment:X4}:{target.Value.Offset:X4} starts with 0x{first:X2}, "
                    + "not 0x55 (push bp) — that is not a Borland far function. An overlay has "
                    + "probably moved: find it by signature search while it is resident, or pass "
                    + "force=true if you really mean this address."
            };
        }

        State st = _emulator.State;
        // *** SNAPSHOT EVERYTHING BEFORE TOUCHING ANY OF IT. *** The game is mid-frame; the whole
        // safety of this tool is that it puts the CPU back exactly as it found it.
        (ushort ax, ushort bx, ushort cx, ushort dx) = (st.AX, st.BX, st.CX, st.DX);
        (ushort si, ushort di, ushort bp, ushort sp) = (st.SI, st.DI, st.BP, st.SP);
        (ushort dsSaved, ushort es, ushort ss, ushort cs, ushort ip) = (st.DS, st.ES, st.SS, st.CS, st.IP);

        void Push(ushort value) {
            st.SP -= 2;
            _emulator.Memory.UInt16[(uint)(st.SS << 4) + st.SP] = value;
        }

        // *** A SECOND BREAKPOINT ON THE TARGET, BECAUSE THE FIRST VERSION LIED. *** Without it the
        // tool reported `returned: true` for an address that cannot possibly return — writing
        // State.CS/IP does not redirect Spice86's CfgCpu, so the CPU stayed where it was, and the
        // breakpoint on the RETURN address (which is where it still was) fired at once. The smoke
        // test passed for that reason and meant nothing; what caught it was a target with a known
        // answer returning the wrong one.
        bool enteredTarget = false;
        var entryBp = new AddressBreakPoint(
            BreakPointType.CPU_EXECUTION_ADDRESS,
            (uint)(target.Value.Segment << 4) + target.Value.Offset,
            _ => enteredTarget = true,
            false);
        _emulator.BreakpointsManager.ToggleBreakPoint(entryBp, true);

        // *** THE GUEST MAKES THE CALL, NOT US. *** Jumping straight at the routine and letting it
        // RETF means Spice86's FunctionHandler sees a return it never saw a call for. Five bytes of
        // `CALL FAR target` in the BIOS inter-application scratch at 0040:00F0 — sixteen bytes DOS
        // reserves and never touches — and a jump THERE instead: the guest executes a real far call
        // and the RETF balances it.
        //
        // *** AND THE BYTE AFTER THE CALL IS `JMP $`, NOT `HLT`. *** The first version parked an
        // 0xF4 there on the reasoning that it is never reached. It is: the breakpoint only REQUESTS
        // a pause, the CPU can run one more instruction before that lands, and Spice86 treats HLT as
        // the machine exiting — "Exiting machine entry point but current function does not seem to
        // be entry point" — and the emulator shuts down mid-session. `EB FE` costs a few harmless
        // cycles instead of the process.
        const uint stubSegment = 0x0040;
        const uint stubOffset = 0x00F0;
        uint stubPhysical = (stubSegment << 4) + stubOffset;
        _emulator.Memory.UInt8[stubPhysical] = 0x9A;
        _emulator.Memory.UInt16[stubPhysical + 1] = target.Value.Offset;
        _emulator.Memory.UInt16[stubPhysical + 3] = target.Value.Segment;
        _emulator.Memory.UInt8[stubPhysical + 5] = 0xEB;
        _emulator.Memory.UInt8[stubPhysical + 6] = 0xFE;

        bool returned = false;
        uint returnPhysical = stubPhysical + 5;
        var bp2 = new AddressBreakPoint(
            BreakPointType.CPU_EXECUTION_ADDRESS, returnPhysical,
            _ => {
                returned = true;
                _emulator.PauseHandler.RequestPause("bak_call_function returned");
            },
            false);
        _emulator.BreakpointsManager.ToggleBreakPoint(bp2, true);

        try {
            for (int i = words.Count - 1; i >= 0; i--) {
                Push(words[i]);
            }
            // No return address is pushed here: the stub's own CALL FAR pushes it.

            if (!string.IsNullOrWhiteSpace(ds)) {
                uint? dsValue = ds.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? AddressAndValueParser.ParseHex(ds)
                    : (uint.TryParse(ds, out uint dv) ? dv : null);
                if (dsValue == null) {
                    return new { error = $"cannot parse ds '{ds}'" };
                }
                st.DS = (ushort)dsValue.Value;
            }

            st.CS = (ushort)stubSegment;
            st.IP = (ushort)stubOffset;

            _emulator.PauseHandler.Resume();
            int elapsed = 0;
            while (!returned && elapsed < timeoutMs) {
                Thread.Sleep(20);
                elapsed += 20;
            }

            // *** WAIT FOR THE PAUSE TO LAND BEFORE TOUCHING ANYTHING. *** The breakpoint callback
            // only REQUESTS a pause; the CPU thread is still running when this one wakes up. Reading
            // AX there is a race, and restoring SP there is worse — it rewrites the stack pointer
            // under a running instruction, which is how the second call in a row ended up doing a
            // RETF into segment zero. bak_run_to_ida settles the same way after its own request.
            if (!returned) {
                _emulator.PauseHandler.RequestPause("bak_call_function timed out");
            }
            Thread.Sleep(PauseSettleMs);

            ushort resultAx = st.AX;
            ushort resultDx = st.DX;

            // *** ONE CALL PER SESSION IS ALL THIS GETS, AND THE FLAGS SAY WHICH ONE. *** Writing
            // State.CS/IP only redirects on the CfgCpu's COLD path — ExecuteOneNode compares the
            // node's address against the registers and drops NodeToExecuteNextAccordingToGraph when
            // they differ, and that check is skipped once the surrounding block is discovered and
            // live. Measured: the first call answers correctly, every later one answers a constant
            // with entered_target false. TASK-449 has the way out —
            // ExecutionContextManager.SignalNewExecutionContext, the path an interrupt takes.
            return new {
                returned,
                entered_target = enteredTarget,
                trustworthy = enteredTarget && returned,
                restart_advised = true,
                ax = resultAx,
                dx = resultDx,
                signed_ax = (short)resultAx,
                // A Borland `long` comes back in DX:AX.
                dx_ax = ((uint)resultDx << 16) | resultAx,
                called = $"{target.Value.Segment:X4}:{target.Value.Offset:X4}",
                args = words.Count,
                timeout_ms = returned ? (int?)null : timeoutMs
            };
        } finally {
            _emulator.BreakpointsManager.ToggleBreakPoint(entryBp, false);
            _emulator.BreakpointsManager.ToggleBreakPoint(bp2, false);
            // Restore in a fixed order; SS and SP together, so a half-restored stack can never be
            // left behind even if the call timed out.
            st.AX = ax; st.BX = bx; st.CX = cx; st.DX = dx;
            st.SI = si; st.DI = di; st.BP = bp;
            st.DS = dsSaved; st.ES = es;
            st.SS = ss; st.SP = sp;
            st.CS = cs; st.IP = ip;
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
        // *** CLAMP FIRST -- an out-of-bounds cursor is not merely ignored, it corrupts the screen. ***
        // This tool writes the game's mouse globals DIRECTLY, bypassing the mouse driver that would
        // normally clamp them, so it can produce a position real hardware never reports.
        //
        // At y == ScreenHeight, DrawMouseCursor (IDA 0x2AECF) -- which clamps NEGATIVE coordinates
        // only, having no reason to defend against an impossible one -- computes a clipped cursor
        // height of `scrHeight - y` = 0 and passes it to vga_paste_rect. That row loop is a do-while
        // (`dec [bp+height]; jz`), so 0 wraps to 0xFFFF and it paints 65536 rows through the 64 KB
        // VGA aperture, striding 80 bytes each. 65536 mod 80 = 16 bytes = 64 pixels, so the painted
        // column walks 64 px right on every wrap, leaving five 16-px garbage bands across the screen.
        //
        // That was filed as TASK-315 ("screen transitions leave the background as animating VGA
        // garbage"), labelled a blocker, and cost several sessions chasing VGA timing, Chain4, the
        // blit, the plane mask and the RNG -- all of which were fine. The repro simply clicked
        // (320, 200), one pixel past the bottom-right of a 320x200 screen.
        (x, y) = ClampToScreen(x, y);

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

        // *** MOVE FIRST, AND LET THE GAME SEE THE CURSOR THERE WITH THE BUTTON UP. ***
        // Writing the position and the button in the same instant tells the game a press with no
        // preceding hover, and some widgets never act on it. Measured 2026-09-12 on the inventory's
        // Use button (REQ_INV action 22, `bActive_flag = 1` and `wEnable_gate = 0` for a party
        // member's own pack, so it is live): six same-instant clicks cleared the selection and did
        // nothing, twice, while move-wait-press-wait-release used the item first time, twice.
        // The separate move is the difference; it costs ~120 ms and it is why this is not one write.
        //
        // BaK mouse globals use 4x screen coordinates (LoadMousePosition divides by 4).
        _emulator.Memory.UInt16[physX.Value] = (ushort)(x * 4);
        _emulator.Memory.UInt16[physY.Value] = (ushort)(y * 4);
        _emulator.PauseHandler.Resume();
        Thread.Sleep(MoveSettleMs);

        _emulator.Memory.WriteRam(new[] { buttonBit }, physButton.Value);
        Thread.Sleep(PressHoldMs);

        // Button up
        _emulator.Memory.WriteRam(new byte[] { 0 }, physButton.Value);

        // *** AND LET IT RUN AGAIN, WITH THE BUTTON UP. ***
        // menupage_input_poll (canassa UI/MENUPAGE.C:418-523) fires an action on the RELEASE, not on
        // the press: a poll with the button down only records `g_pMenuPressAnchor`, and the action id
        // comes from the LATER poll where `input == 0` and the cursor is still over that same anchor
        // (the `input_zero` arm). So a click that stops at the button-up WRITE has told the game half
        // a click, and the widget does nothing at all.
        //
        // Measured 2026-09-12 on the inventory's Use button (REQ_INV action 22): six plain clicks
        // cleared the selection and produced nothing, while the same click driven as a held press —
        // move, wait, press, wait, release, wait — used the item first time. The sleep is what turns
        // the second into the first.
        Thread.Sleep(ReleaseRunMs);

        return new {
            success = true,
            x,
            y,
            button,
            message = $"Mouse {button}-clicked at ({x}, {y})"
        };
    }

    /// <summary>
    /// How long to wait after a pause is REQUESTED before believing the CPU has stopped.
    /// </summary>
    private const int PauseSettleMs = 200;

    /// <summary>How long the cursor sits at the new position, button up, before it is pressed.</summary>
    private const int MoveSettleMs = 120;

    /// <summary>How long the button is held down before it is released.</summary>
    private const int PressHoldMs = 150;

    /// <summary>
    /// How long the emulator runs after the release, so the poll that actually fires the action gets
    /// to happen inside this call rather than whenever something else next resumes.
    /// </summary>
    private const int ReleaseRunMs = 150;

    /// <summary>The game's display is mode 13h/mode X: 320x200, so the last addressable pixel is
    /// (319, 199).</summary>
    internal const int ScreenWidth = 320;
    internal const int ScreenHeight = 200;

    /// <summary>Clamps a requested cursor position into the visible screen. See the remarks in
    /// <see cref="MouseClick"/> for why an off-screen position is destructive rather than
    /// harmless.</summary>
    internal static (int X, int Y) ClampToScreen(int x, int y) => (
        x < 0 ? 0 : x > ScreenWidth - 1 ? ScreenWidth - 1 : x,
        y < 0 ? 0 : y > ScreenHeight - 1 ? ScreenHeight - 1 : y);

}
