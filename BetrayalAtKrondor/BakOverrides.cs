namespace BetrayalAtKrondor;

using Microsoft.Extensions.Logging;
using BetrayalAtKrondor.Mcp;
using BetrayalAtKrondor.Overrides.Libraries;
using GameData;
using SkiaSharp;
using Spice86.Core.CLI;
using Spice86.Core.Emulator.Function;
using Spice86.Core.Emulator.Memory.ReaderWriter;
using Spice86.Core.Emulator.ReverseEngineer;
using Spice86.Core.Emulator.ReverseEngineer.DataStructure;
using Spice86.Core.Emulator.VM;
using Spice86.Core.Emulator.VM.Breakpoint;
using Spice86.Shared.Emulator.Memory;
using Spice86.Shared.Emulator.VM.Breakpoint;
using Spice86.Shared.Interfaces;
using Spice86.Shared.Utils;
using System.Text;
using static System.String;
using ArgumentFetcher = Spice86.Core.Emulator.ReverseEngineer.ArgumentFetcher;

public class BakOverrides : CSharpOverrideHelper {
    private readonly IGameEngine _gameEngine;
    private readonly IGlobalSettings _globalSettings;
    private readonly List<OvrBreakpoint> _ovrBreakpoints = [];
    private readonly Dictionary<ushort, ushort> _ovrSegmentMapping = [];
    private readonly OverlayAddressTranslator _translator;
    private readonly ArgumentFetcher _args;
    private readonly IPauseHandler _pauseHandler;
    private Dictionary<uint, byte> _wordLowByteWrites = [];
    private ushort _capturedSoundDriverSegment = 0;
    private ushort _capturedExtraSoundDriverSegment = 0;
    private List<OvrBreakpoint> _dynamicLoadedCodeBreakpoints = [];

    private const int SoundDriverSegment = 0x8000;
    private const int ExtraSoundDriverSegment = 0x8300;

    public BakOverrides(Dictionary<SegmentedAddress, FunctionInformation> functionsInformation, Machine machine,
        ILogger loggerService, Configuration configuration, OverlayAddressTranslator translator) : base(functionsInformation,
        machine, loggerService, configuration) {
        _translator = translator;
        _globalSettings = new GlobalSettings(machine.Memory);
        _gameEngine = new GameEngine(machine.MouseDriver);
        _gameEngine.DataPath = Path.GetDirectoryName(configuration.Exe);
        _ = new StdIO(functionsInformation, machine, loggerService, configuration);
        _args = new ArgumentFetcher(machine.Stack, machine.CpuState, machine.Memory);
        _pauseHandler = machine.PauseHandler;
        DefineFunctions();
        DefineBreakpoints();
    }

    private void LogDialogBuildCall() {
        _loggerService.LogInformation("dialog_Build?Show? called: dialogIdOrOffset: {Arg0}, arg_4: {Arg4}", Stack.Peek32(4), Stack.Peek16(8));
    }

    private void DefineBreakpoints() {
        // DoOnTopOfInstruction("35C1:0040", () => {
        //     foreach (var breakpoint in _dynamicLoadedCodeBreakpoints.Where(ovrBreakpoint => ovrBreakpoint.Segment == ExtraSoundDriverSegment)) {
        //         DoOnTopOfInstruction(State.DX, breakpoint.Offset, breakpoint.Action);
        //     }
        // });
        // DoOnTopOfInstruction("35C1:00D5", () => {
        //     foreach (var breakpoint in _dynamicLoadedCodeBreakpoints.Where(ovrBreakpoint => ovrBreakpoint.Segment == SoundDriverSegment)) {
        //         DoOnTopOfInstruction(State.DX, breakpoint.Offset, breakpoint.Action);
        //     }
        // });
        DoOnTopOfInstruction("36BC:069A", RecordOvrChange);
        DoOnTopOfInstruction("1834:2AA5", RecordVmCodeSegment);

        // DoOnTopOfInstruction("1834:22CC", LogAllocateMemory);

        // DoOnTopOfInstruction("5278:0565", LogFrameCommand);

        // PauseAt("5040:1817", "check animbigstruct at ES:BX");
        // PauseAt("5040:174A", "compare ax with arg0");

        // PauseAt("16C5:15CC", "flag 4 set");
        // PauseAt("16C5:166D", "flag 8 set");
        // PauseAt("16C5:16FC", "word_dseg_2DE != 0");

        // DoOnTopOfInstruction("16C5:16FC", () => {
        //     _loggerService.LogInformation("word_dseg_2DE = 0x{Value:X4}", State.BX);
        //     _pauseHandler.RequestPause();
        // });
        //
        // PauseAt("1834:483E", "xywidthheight?");
        // PauseAt("1834:3F7B", "drawrotated?");
        // PauseAt("5278:10D2", "call what?");

        // DoOnTopOfInstruction("327D:0000", () => {
        //     _args.Get(out ushort stream, out uint nrOfBytes, out uint pBuffer);
        //     _loggerService.LogInformation("[{Caller}] sub_seg045_0(stream: {Stream}, nrOfBytes: {NrOfBytes}, pBuffer: {PBuffer:X8})", CallerAddress(), stream, nrOfBytes, pBuffer);
        // });
        // DoOnTopOfInstruction("3239:000C", () => {
        //     _args.Get(out ushort stream, out byte arg2);
        //     _loggerService.LogInformation("[{Caller}] resourceLoadSound(stream: {Stream}, arg2: {Arg2})", CallerAddress(), stream, arg2);
        // });
        // DoOnTopOfInstruction("3556:000F", () => {
        //     _args.Get(out ushort stream, out ushort soundId);
        //     _loggerService.LogInformation("[{Caller}] resourceLoadSx(stream: {Stream}, soundId: {SoundId})", CallerAddress(), stream, soundId);
        //     // _pauseHandler.RequestPause("step from here");
        // });

        // DoOnTopOfInstruction("1834:7320", () => {
        //     _args.Get(out ushort index, out ushort offset, out ushort segment, out ushort size);
        //     _loggerService.LogInformation("[{Caller}] resourceLoadCurrentFile(index: {Index}, pBuffer: {Segment:X4}:{Offset:X4}, size: {Size} (0x{SizeHex:X4}))", CallerAddress(), index, segment, offset, size, size);
        // });

        // DoOnTopOfInstruction("158B:0121", () => {
        //     _args.Get(out uint size, out ushort flag);
        //     _loggerService.LogInformation("[{Caller}] audio_allocate_memory(flag: {Flag}, size: {Size} (0x{SizeHex:X8}))", CallerAddress(), flag, size, size);
        // });
        // DoOnTopOfInstruction("158B:01AF", () => {
        //     _loggerService.LogInformation("[{Caller}] allocated audio memory at: {Segment:X4}:{Offset:X4}", CallerAddress(), State.DX, State.AX);
        // });

        // DoOnTopOfInstruction("1834:649F", () => {
        //     string dump = Stack.PeekWindow(6);
        //     _loggerService.LogInformation("[{Caller}] {Dump}", CallerAddress(), dump);
        //     // _pauseHandler.RequestPause("check 39DD:5FA9");
        // });

        // DoOnTopOfInstruction("327D:0208", () => {
        //     _args.Get(out ushort index, out byte soundFormat);
        //     _loggerService.LogInformation("[{Caller}] sub_seg045_208(index: {Index}, soundFormat: {Arg2:X2})", CallerAddress(), index, soundFormat);
        //     if (soundFormat != 0x0) {
        //         _loggerService.LogWarning("soundFormat = {SoundFormat:X2}", soundFormat);
        //     }
        //     // _pauseHandler.RequestPause("step from here");
        // });

        // DoOnTopOfInstruction("1834:7441", () => {
        //     _args.Get(out ushort index, out uint size, out ushort compressedMaybe);
        //     _loggerService.LogInformation("[{Caller}] resourceReadFileData(index: {Index}, size: {Size} (0x{SizeHex:X8})), compressedMaybe: {CompressedMaybe})", CallerAddress(), index, size, size, compressedMaybe);
        // });

        // DoOnMemoryWrite(0x39DD, 0x3698, () => _pauseHandler.RequestPause("word_dseg_3698 was written to"));

// PauseAt("3239:0140", "step from here");
// PauseAt("35C1:0061", "check 3656:0002");

        // PauseAt("35D5:000D", "playing a sound");
        // PauseAt("3638:0071", "playing a sound continued");
        //
        // DoOnTopOfInstruction("32E5:000B", () => {
        //     _args.Get(out string path, out string tag, out ushort arg4);
        //     _loggerService.LogInformation("[{Caller}] LoadSoundCodeOverlay(path: {Path}, tag: {Tag}, arg4: {Arg4})", CallerAddress(), path,
        //         tag, arg4);
        // });
        //
        // DoOnTopOfInstruction("8300:0000", () => {
        //     if (State.AX != 1) {
        //         _loggerService.LogInformation("[{Caller}] Extra AudioDriver Dispatcher function {FunctionId:X2}", CallerAddress(), State.AX);
        //     }
        // });
        // DoOnTopOfInstruction("8000:0000", () => {
        //     if (State.BP != 3) {
        //         _loggerService.LogInformation("[{Caller}] AudioDriver Dispatcher function {FunctionId:X2}", CallerAddress(), State.BP);
        //     }
        // });
    }

    private Action LogBx(string message) {
        return () => {
            _loggerService.LogInformation("{Message} = 0x{Value:X4}", message, State.BX);
        };
    }

    private string CallerAddress() {
        ushort segment = Stack.Peek16(2);
        var adjust = 5;
        if (segment == 0) {
            segment = State.CS;
            adjust -= 2;
        }

        ushort offset = (ushort)(Stack.Peek16(0) - adjust);

        return $"{segment:X4}:{offset:X4}";
    }

    private void RecordVmCodeSegment() {
        _ovrSegmentMapping[State.DX] = 0x7B00;
    }

    private void LogFrameCommand() {
        var segment = State.ES;
        var offset = State.BX;
        var address = MemoryUtils.ToPhysicalAddress(segment, offset);
        var bytes = Memory.ReadRam(16, address);
        var reader = new BinaryReader(new MemoryStream(bytes));
        ushort type = reader.ReadUInt16();
        if (type == 0x0FF0) {
            _loggerService.LogInformation("FrameCommand: [{Type:X4}] (End of frame)", type);

            return;
        }
        var command = ResourceExtraction.Extractors.Animation.TtmExtractor.GetFrameCommand(type, reader);
        _loggerService.LogInformation("FrameCommand: [{Type:X4}] {Command}", type, command);
    }

    private void LogAt(string address, string message) {
        DoOnTopOfInstruction(address, () => {
            _loggerService.LogInformation("{Message}", message);
        });
    }

    private void LogColorCycle() {
        _args.Get(out ushort start, out ushort length, out ushort color, out ushort blendAmount);
        _loggerService.LogInformation(
            "[{Segment:X4}:{Offset:X4}] ColorCycle(start: {Length:X4}, end: {Start:X4}, color: {Color:X4}, blendAmount: {BlendAmount:X4})",
            State.CS, State.IP, length, start, color, blendAmount);
    }

    private void PauseAt(string address, string message) {
        DoOnTopOfInstruction(address, () => {
            _pauseHandler.RequestPause(message);
        });
    }

    private void LogAllocateMemory() {
        _args.Get(out uint sizeInBytes, out uint boolClear);
        _loggerService.LogInformation("AllocateMemory({BoolClear}, {SizeInBytes})", boolClear, sizeInBytes);
    }

    private void AddDWordMemoryMonitor(string address, string name) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);
        DoOnMemoryWrite(segment, (ushort)(offset + 3), () => {
            _ovrSegmentMapping.TryGetValue(State.CS, out ushort idaSegment);
            _loggerService.LogInformation(
                "[{IdaSegment:X4}:{IdaOffset:X4}] {Name} Memory write at {Segment:X4}:{Offset:X4}: {ValueSegment:X4}:{ValueOffset:X4}",
                idaSegment, State.IP, name, segment, offset, Memory.UInt16[segment, (ushort)(offset + 2)], Memory.UInt16[segment, offset]);
        });
    }

    private Action LogStringAt(string address) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);

        return () => {
            var stringAddress = MemoryUtils.ToPhysicalAddress(segment, offset);
            _loggerService.LogInformation("{Segment:X4}:{Offset:X4} = {Value}", segment, offset,
                Memory.GetZeroTerminatedString(stringAddress, 100));
        };
    }

    private void AddWordWriteMemoryMonitor(string address, string? name = null) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);
        DoOnMemoryWrite(segment, offset, () => {
            _wordLowByteWrites[MemoryUtils.ToPhysicalAddress(segment, offset)] = Memory.CurrentlyWritingByte;
        });
        DoOnMemoryWrite(segment, (ushort)(offset + 1), () => {
            uint physicalAddress = MemoryUtils.ToPhysicalAddress(segment, offset);
            if (_wordLowByteWrites.TryGetValue(physicalAddress, out byte lowByte)) {
                if (!_ovrSegmentMapping.TryGetValue(State.CS, out ushort idaSegment)) {
                    idaSegment = State.CS;
                }
                int writtenValue = lowByte | Memory.CurrentlyWritingByte << 8;
                _loggerService.LogInformation("[{IdaSegment:X4}:{IdaOffset:X4}] {Name} Memory write at {Segment:X4}:{Offset:X4}: 0x{Value:X4}",
                    idaSegment, State.IP, name, segment, offset, writtenValue);
                _wordLowByteWrites.Remove(physicalAddress);
            }
        });
    }

    private void AddByteWriteMemoryMonitor(string address, string? name = null) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);
        DoOnMemoryWrite(segment, offset, () => {
            if (!_ovrSegmentMapping.TryGetValue(State.CS, out ushort idaSegment)) {
                idaSegment = State.CS;
            }
            int writtenValue = Memory.CurrentlyWritingByte;
            _loggerService.LogInformation("[{IdaSegment:X4}:{IdaOffset:X4}] {Name} Memory write at {Segment:X4}:{Offset:X4}: 0x{Value:X2}",
                idaSegment, State.IP, name, segment, offset, writtenValue);
        });
    }

    private void AddWordReadMemoryMonitor(string address, string? name = null) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);
        DoOnMemoryRead(segment, (ushort)(offset + 1), () => {
            if (!_ovrSegmentMapping.TryGetValue(State.CS, out ushort idaSegment)) {
                idaSegment = State.CS;
            }
            uint physicalAddress = MemoryUtils.ToPhysicalAddress(segment, offset);
            int readValue = Memory.Ram.Read(physicalAddress) | Memory.Ram.Read(physicalAddress + 1) << 8;
            _loggerService.LogInformation("[{IdaSegment:X4}:{IdaOffset:X4}] {Name} Memory read at {Segment:X4}:{Offset:X4}: 0x{Value:X4}",
                idaSegment, State.IP, name, segment, offset, readValue);
        });
    }

    private void AddByteMemoryMonitor(string address, string? name = null) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);
        DoOnMemoryWrite(segment, offset, () => {
            _ovrSegmentMapping.TryGetValue(State.CS, out ushort idaSegment);
            _loggerService.LogInformation("[{IdaSegment:X4}:{IdaOffset:X4}] {Name} Memory write at {Segment:X4}:{Offset:X4}: 0x{Value:X2}",
                idaSegment, State.IP, name, segment, offset, Memory.UInt8[segment, offset]);
        });
    }

    private Action LogAx(string message) {
        return () => {
            _loggerService.LogInformation("{Message} = 0x{Value:X4}", message, State.AX);
        };
    }

    private Action LogDs(string message) {
        return () => {
            _loggerService.LogInformation("{Message} = 0x{Value:X4}", message, State.DS);
        };
    }

    private Action LogMemoryAtAxDx(string? message = null) {
        return () => {
            if (message != null) {
                _loggerService.LogInformation("{Message}:", message);
            }
            var address = MemoryUtils.ToPhysicalAddress(State.AX, State.DX);
            for (int i = -2; i < 20; i += 2) {
                _loggerService.LogInformation("{Segment:X4}:{Offset:X4} = {Value:X4}", State.AX, State.DX + i, Memory.UInt16[address + i]);
            }
        };
    }

    private void LogEax() {
        _loggerService.LogInformation("EAX: {Eax:X4}", State.EAX);
    }

    private void RecordOvrChange() {
        var runtimeStubSegment = State.ES;
        var runtimeSegment = State.BX;

        // Convert runtime stub segment to IDA stub segment by adding the segment delta (0xE830 >> 4 = 0xE83)
        ushort idaStubSegment = (ushort)(runtimeStubSegment + (OverlayAddressTranslator.RelocationDelta >> 4));

        if (StubSegments.IdaStubToIdaCode.TryGetValue(idaStubSegment, out ushort idaSegment)) {
            _ovrSegmentMapping[runtimeSegment] = idaSegment;
            _translator.RecordMapping(runtimeSegment, idaSegment);
            _loggerService.LogDebug("OVR Mapping real segment {SourceSegment:X4} to ida segment {DestinationSegment:X4}", runtimeSegment,
                idaSegment);
        }

        var ovrBreakpoint = _ovrBreakpoints.FirstOrDefault(breakpoint => StubSegments.IdaCodeToIdaStub[breakpoint.IdaSegment] == idaStubSegment);
        if (ovrBreakpoint is not null) {
            DoOnTopOfInstruction(runtimeSegment, ovrBreakpoint.Offset, ovrBreakpoint.Action);
        }
    }

    private void Logsub_ovr185_0() {
        _loggerService.LogInformation("{MethodName} called", nameof(Logsub_ovr185_0));
    }

    private void Logsub_ovr185_33F() {
        _loggerService.LogInformation("{MethodName} called", nameof(Logsub_ovr185_33F));
    }

    private void Logsub_ovr185_53F() {
        _loggerService.LogInformation("{MethodName} called", nameof(Logsub_ovr185_53F));
    }

    private void LogLoadTzzxxyy_WLD() {
        var zoneNumber = Stack.Peek16(6);
        var xCoordinate = Stack.Peek16(8);
        var yCoordinate = Stack.Peek16(10);
        var arg6 = Stack.Peek16(12);

        _loggerService.LogInformation(
            "{MethodName} called. zoneNumber: {ZoneNumber}, xCoordinate: {XCoordinate}, yCoordinate: {YCoordinate}, arg_6: {Arg6}",
            nameof(LogLoadTzzxxyy_WLD), zoneNumber, xCoordinate, yCoordinate, arg6);
    }

    private void LogGetGlobalValue() {
        _loggerService.LogInformation("GetGlobalValue(key: {Arg0})", Stack.Peek16(4));
    }

    private void LogSetGlobalValue() {
        _loggerService.LogInformation("SetGlobalValue(key: {Arg0}, value: {Arg2:X4})", Stack.Peek16(4), Stack.Peek16(6));
    }

    private void LogGetValueFromActor() {
        ushort actorOffset = Stack.Peek16(4);
        ushort arg2 = Stack.Peek16(6);
        ushort arg4 = Stack.Peek16(8);

        uint actorAddress = MemoryUtils.ToPhysicalAddress(DS, actorOffset);

        ushort nameOffset = Memory.UInt16[actorAddress];
        uint nameAddress = MemoryUtils.ToPhysicalAddress(DS, nameOffset);
        string name = Memory.GetZeroTerminatedString(nameAddress, 9);
        var field58 = Memory.UInt8[actorAddress + 0x58];
        name += $" ({field58:X2})";

        uint attributeBase = actorAddress + 8;
        var attributeOffset = 5 * arg2;
        var attribute = (ActorAttribute)arg2;
        long attributeAddress = attributeBase + attributeOffset;

        int attributeValue;
        if (attribute == ActorAttribute.HealthStaminaCombo) {
            var health = GetAttributeValue(arg4, attributeAddress, ActorAttribute.Health, actorAddress);
            var stamina = GetAttributeValue(arg4, attributeAddress, ActorAttribute.Stamina, actorAddress);
            attributeValue = health + stamina;
        } else {
            attributeValue = GetAttributeValue(arg4, attributeAddress, attribute, actorAddress);
        }

        var attributeValue1 = Memory.UInt8[attributeAddress + 0];
        var attributeValue2 = Memory.UInt8[attributeAddress + 1];
        var attributeValue3 = Memory.UInt8[attributeAddress + 2];
        var attributeValue4 = Memory.UInt8[attributeAddress + 3];
        var attributeValue5 = Memory.UInt8[attributeAddress + 4];

        _loggerService.LogDebug(
            "{MethodName}: actor: {Name}, arg_2: {Arg2:X4}, arg_4: {Arg4:X4}, {Attribute}: {AttributeValue} ({Value1} {Value2} {Value3} {Value4} {Value5})",
            nameof(LogGetValueFromActor), name, arg2, arg4, attribute, attributeValue, attributeValue1, attributeValue2, attributeValue3,
            attributeValue4, attributeValue5);
    }

    private int GetAttributeValue(ushort arg4, long attributeAddress, ActorAttribute attribute, uint actorAddress) {
        int attributeValue = arg4 switch {
            1 => Memory.UInt8[attributeAddress],
            3 => Memory.UInt8[attributeAddress + 1],
            _ => CalculateActiveValue(attribute, actorAddress)
        };

        return attributeValue;
    }

    private int CalculateActiveValue(ActorAttribute attribute, uint actorAddress) {
        var current = Memory.UInt8[actorAddress + 8 + 5 * (int)attribute + 1];

        // apply bonuses and penalties
        return current;
    }

    private void Sub_4B54C() {
        ushort offset = Stack.Peek16(4);
        ushort segment = Stack.Peek16(6);
        uint physicalAddress = MemoryUtils.ToPhysicalAddress(segment, offset);
        var dialogEntry = new DialogEntry(Memory, physicalAddress);
        _loggerService.LogDebug("{MethodName} called: dialogEntry {DialogEntry}", nameof(Sub_4B54C), dialogEntry);
    }

    private void LogField1KeyWordCall() {
        _loggerService.LogInformation("getKeyWordTableOffsetForDialogField1 called: value: {Arg0}", Stack.Peek16(4));
    }

    /// <summary>
    /// Set a breakpoint using IDA seg:offset addresses. Applies relocation delta for resident segments.
    /// </summary>
    private void DoOnTopOfInstruction(string address, Action action) {
        (ushort idaSegment, ushort offset) = ToSegmentOffset(address);

        // If segment >= ovr121  (0x3FF7) then it's an overlay
        // Look up segment in ovr table
        if (idaSegment is >= 0x3FF7 and < 0x5ADE) {
            // We add it to the list, and when the OVR gets mapped, the real breakpoint is added.
            _ovrBreakpoints.Add(new OvrBreakpoint(idaSegment, offset, action));

            return;
        }
        if (idaSegment is SoundDriverSegment or ExtraSoundDriverSegment) {
            _dynamicLoadedCodeBreakpoints.Add(new OvrBreakpoint(idaSegment, offset, action));

            return;
        }

        // Resident segment: addresses are IDA seg:offset, convert to runtime physical
        uint idaLinear = MemoryUtils.ToPhysicalAddress(idaSegment, offset);
        uint physical = idaLinear - OverlayAddressTranslator.RelocationDelta;
        AddressBreakPoint breakPoint = new(
            BreakPointType.CPU_EXECUTION_ADDRESS,
            physical,
            _ => action.Invoke(),
            false);
        EmulatorBreakpointsManager.ToggleBreakPoint(breakPoint, true);
    }

    private static (ushort segment, ushort offset) ToSegmentOffset(string address) {
        var parts = address.Split(':');
        ushort segment = (ushort)ParseHex(parts[0]);
        ushort offset = (ushort)ParseHex(parts[1]);

        return (segment, offset);
    }

    private static int ParseHex(string hex) {
        return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
    }

    /// <summary>
    /// DefineFunction wrapper that converts IDA seg:offset to runtime seg:offset
    /// by applying the relocation delta before registering with Spice86.
    /// </summary>
    private void DefineFunctionIda(ushort idaSegment, ushort offset, Func<int, Action> overrideFunc,
        bool failOnExisting = true, string? name = null) {
        DefineFunction(RuntimeSegment(idaSegment), offset, overrideFunc, failOnExisting, name);
    }

    /// <summary>
    /// Relocate an IDA segment to the segment the program actually runs in, keeping the offset.
    /// <para><see cref="SegmentedAddress" /> is a record struct, so overrides are looked up by
    /// (Segment, Offset) equality and NOT by linear address. Normalising to
    /// <c>segment = linear >> 4, offset = linear &amp; 0xF</c> therefore yields a key that no CALL can
    /// ever match, even though it denotes the same physical byte — which is why this must relocate
    /// the segment base and leave the offset alone.</para>
    /// </summary>
    private static ushort RuntimeSegment(ushort idaSegment) =>
        (ushort)((MemoryUtils.ToPhysicalAddress(idaSegment, 0) - OverlayAddressTranslator.RelocationDelta) >> 4);

    /// <summary>
    /// DoOnTopOfInstruction wrapper that converts IDA seg:offset to runtime seg:offset,
    /// the same relocation the <see cref="DefineFunctionIda" /> path applies. Use this to
    /// observe or adjust state at an instruction while leaving the original code running;
    /// DefineFunctionIda replaces a function outright.
    /// </summary>
    private void DoOnTopOfInstructionIda(ushort idaSegment, ushort offset, Action action) {
        DoOnTopOfInstruction(RuntimeSegment(idaSegment), offset, action);
    }

    private void DefineFunctions() {
        // DISABLED 2026-09-02. This override never actually ran until DefineFunctionIda's address
        // bug was fixed today (it registered a segment:offset pair no CALL could match — see
        // RuntimeSegment). With it live for the first time, the game boots to a black screen and
        // never reaches the main menu, so the C# body is NOT a faithful replacement for the asm it
        // masks: it reads resource.cfg and drive.cfg and does nothing else. Re-enable only after
        // diffing it against LoadConfig @0x384B0.
        // DefineFunctionIda(0x3849, 0x0020, LoadConfig, true, nameof(LoadConfig));
        DefineChapterSpike();
        DefineTrapSpike();
        DefineCombatSpike();
        DefineTrapTriggerSpike();
    }

    /// <summary>
    /// Opt-in harness for capturing a chapter's cutscenes without playing to them: set
    /// <c>BAK_SPIKE_CHAPTER=N</c> and "New Game" plays chapter N's scenes instead of chapter 1's.
    /// Off unless the variable is set, so a normal run is untouched.
    ///
    /// <para><c>_main</c> @seg020:0x0E65 starts a new game with a literal
    /// <c>playChapterAnimationsAndBook(chapter: 1, part: 1, scene: 1)</c> (<c>push 10001h; push 1</c>),
    /// which plays CHAPTER1.ADS, then the C11.BOK book, then C11.ADS. The callee builds the scene
    /// name from the digits of <c>"C00."</c> — <c>buffer[1] += chapterNr; buffer[2] += partNr</c> —
    /// so chapter 2 part 1 loads C21.ADS with nothing else changed.</para>
    /// </summary>
    /// <summary>IDA's seg020 (base 0x20830), the resident segment holding _main and the chapter
    /// scene players. Offsets passed alongside it are IDA offsets within that segment.</summary>
    private const ushort Seg020 = 0x2083;

    /// <summary>IDA linear address -> the emulator's physical address (resident segments).</summary>
    private static uint IdaLinear(uint idaLinear) => idaLinear - OverlayAddressTranslator.RelocationDelta;

    /// <summary>IDA's seg033 (base 0x2D5F0), resident, holding the TRAPS.DAT loader.</summary>
    private const ushort Seg033 = 0x2D5F;

    /// <summary>IDA's seg021 (base 0x21710), resident, holding the world/arena render entry.</summary>
    private const ushort Seg021 = 0x2171;

    /// <summary><c>encounterNumber_dseg_50F4</c>, the number every trap load reads.</summary>
    private const uint IdaEncounterNumber = 0x3EEC4;

    /// <summary>The resident def_file_struct array the trigger dispatcher walks (19 bytes each).</summary>
    private const uint IdaDefFileStructs = 0x3F0E6;

    private int _trapSpikeEncounter = -1;
    private string _trapFrameDir;
    private int _trapFramesWanted;
    private int _trapFramesTaken;
    private bool _trapSpikeFired;

    /// <summary>
    /// Opt-in harness for photographing a chosen trap encounter in the ORIGINAL, so a screenshot
    /// comparison can use the SAME encounter on both sides.
    /// </summary>
    /// <remarks>
    /// <b>Why forcing the NUMBER and not the DATA.</b> The obvious route — dropping a modified
    /// <c>traps.dat</c> beside the exe, which works because <c>OpenFile</c> @0x1056C fopens the path
    /// before falling back to KRONDOR.001 — produces an encounter that exists in neither the
    /// original nor the port as shipped, so the comparison would be against a scene we invented.
    /// Forcing the encounter NUMBER instead makes the original open a genuine shipped encounter, and
    /// the port can open the same one by its number with nothing patched anywhere.
    ///
    /// <para>Set <c>BAK_TRAP_ENCOUNTER=347</c> (347 and 348 are the cannon rooms: two cannons, two
    /// red crystals, three enemies and the no-retreat lock) together with the chapter spike's
    /// <c>BAK_SPIKE_LOADSAVE</c> to reach a fight unattended. <c>BAK_TRAP_FRAMEDIR</c> dumps frames.
    /// Off unless the variable is set, so a normal run is untouched.</para>
    ///
    /// <para><b>The hook is on the loader, not on combat entry.</b> <c>Load_traps.dat</c> reads the
    /// global at its +0x23, so writing it at the function's first instruction lands before the read
    /// and after whatever set it — no ordering assumption about the caller.</para>
    ///
    /// <para><b>seg033 and seg021 are RESIDENT</b>, which is why
    /// <see cref="DoOnTopOfInstructionIda" /> is usable here: it applies the relocation delta
    /// unconditionally and would compute nonsense for an overlay. <c>renderCombatGridScene</c> lives
    /// in ovr167 and must NOT be hooked this way — hence capturing from the resident
    /// <c>RenderWorldView</c> instead.</para>
    /// </remarks>
    private void DefineTrapSpike() {
        if (!int.TryParse(Environment.GetEnvironmentVariable("BAK_TRAP_ENCOUNTER"), out int encounter)
            || encounter is < 0 or > 767) {
            return;
        }
        _trapSpikeEncounter = encounter;
        _trapFrameDir = Environment.GetEnvironmentVariable("BAK_TRAP_FRAMEDIR");
        _trapFramesWanted =
            int.TryParse(Environment.GetEnvironmentVariable("BAK_TRAP_FRAMES"), out int n) && n > 0 ? n : 8;

        _loggerService.LogInformation(
            "Trap spike: armed (encounter {Encounter}, frames {Frames} -> {Dir})",
            encounter, _trapFramesWanted, _trapFrameDir ?? "none");

        DoOnTopOfInstructionIda(Seg033, 0x0CDE, () => {
            uint address = IdaLinear(IdaEncounterNumber);
            ushort was = UInt16[address];
            if (was == encounter) {
                return;
            }
            UInt16[address] = (ushort)encounter;
            _trapSpikeFired = true;
            _loggerService.LogInformation("Trap spike: Load_traps.dat encounter {Was} -> {Now}", was, encounter);
        });

        if (IsNullOrEmpty(_trapFrameDir)) {
            return;
        }
        Directory.CreateDirectory(_trapFrameDir);

        // RenderWorldView is the arena's own per-frame entry and is RESIDENT, so it is reachable
        // through the Ida helper where renderCombatGridScene (ovr167) is not. Gated on the loader
        // having fired, so the frames are of the forced encounter rather than of the world the
        // party was standing in beforehand.
        DoOnTopOfInstructionIda(Seg021, 0x0959, () => {
            if (!_trapSpikeFired || _trapFramesTaken >= _trapFramesWanted) {
                return;
            }
            _trapFramesTaken++;
            CaptureFrameTo(_trapFrameDir, $"trap_{encounter}_{_trapFramesTaken:D2}.png");
            _loggerService.LogInformation("Trap spike: captured frame {N}/{Total}",
                _trapFramesTaken, _trapFramesWanted);
        });
    }

    /// <summary>
    /// Opt-in harness that CALLS the combat entry directly: set <c>BAK_COMBAT_ENCOUNTER=N</c> and
    /// the next world frame enters encounter N, with no trap trigger and no walking.
    /// </summary>
    /// <remarks>
    /// <b>Why this exists.</b> Reaching a trap the honest way means walking the party onto a
    /// trigger, and that turned into a pathfinding problem: the party wedges on impassable terrain
    /// 4,000 units from the only usable save, and teleporting past it desyncs the walkability list
    /// so nothing moves at all (TASK-323). But the trigger is only a DELIVERY MECHANISM — what has
    /// to happen is the combat entry running. So call it.
    ///
    /// <para>The signature is taken from the one real call site, <c>trapTrigger_phase2</c> @0x748fd:
    /// <code>
    ///   push [bp+var_6]                          ; flag
    ///   lea ax,[bp+var_4]; push ax               ; near ptr to an out-word
    ///   push [bp+trap_entry.encounterNumber]     ; encounter (word)
    ///   call j_runCombatEncounter                ; @0x3909f, RESIDENT thunk
    ///   add sp, 6
    /// </code>
    /// Three words, C order, caller cleans up. The out-pointer is a NEAR pointer taken from the
    /// caller's own stack frame, which is only sound because this program runs with
    /// <b>DS == SS</b> — confirmed live at a MoveParty breakpoint (both 0x2B5A). That is why the
    /// scratch word below can live on the stack.</para>
    ///
    /// <para><b>This deliberately does not restore SP afterwards.</b> The forged frame abandons the
    /// world loop, so the game is not expected to survive returning from the fight. That is
    /// acceptable for a capture harness whose whole job is to get the arena on screen, and it is
    /// why this is opt-in rather than always armed. Do not reuse it for anything that has to keep
    /// playing afterwards.</para>
    ///
    /// <para><b>STATUS: the call FIRES but the game does not survive it, so this does not yet
    /// produce a capture.</b> The log shows "Combat spike: calling runCombatEncounter(347)" and
    /// then, within 40 ms, "CALL STACK DEPTH EXCEEDED 1000" followed by a storm of "Returning but
    /// no call was done". So the entry mechanics are right — the thunk is reached — but
    /// <c>runCombatEncounter</c> evidently needs context that only its real caller sets up.
    /// <c>trapTrigger_phase2</c> builds a 0x199-byte <c>def_trap</c> on its own stack, sets
    /// <c>creatureType</c> from the first enemy slot, may show a dialog, and relocates the party,
    /// all before it calls this — none of which exists in the forged frame.</para>
    ///
    /// <para><b>The better target is therefore <c>j_trapTrigger_phase2</c> @0x39a75</b> (also
    /// resident), which does that setup itself. It takes <c>(def_file_struct near*, bool near*)</c>,
    /// so it needs a filled-in <c>def_file_struct</c> — <c>defFileStructs</c> @0x3f0e6 is a
    /// resident array of 19-byte entries and is the obvious place to borrow or build one. That
    /// needs the field offsets pinned down first; do that before the next attempt rather than
    /// guessing.</para>
    /// </remarks>
    private void DefineCombatSpike() {
        if (!int.TryParse(Environment.GetEnvironmentVariable("BAK_COMBAT_ENCOUNTER"),
                out int encounter) || encounter <= 0) {
            return;
        }

        _loggerService.LogInformation("Combat spike: armed (encounter {Encounter})", encounter);

        // *** NOT RenderWorldView. *** Seg021:0x0959 (RenderWorldView @0x22069) is never called —
        // an MCP breakpoint on it does not trip while the travel view is plainly rendering, so the
        // world is drawn by some other path. The trap spike's frame capture hangs off that same
        // address and has therefore never run either. menu_resolveHoverAndClick @0x2c97f IS
        // resident and IS called every frame of the travel loop (breakpoint trips within 3s).
        var entered = false;
        DoOnTopOfInstructionIda(0x2C97, 0x000F, () => {
            if (entered) {
                return;
            }
            entered = true;

            // Scratch word for the out-parameter, carved off the top of the stack. Safe as a near
            // pointer only because DS == SS here.
            State.SP -= 2;
            ushort outPtr = State.SP;

            // C order: pushed right to left, so the encounter number ends up as the FIRST argument.
            Stack.Push16(0);                    // flag  (var_6 at the real call site)
            Stack.Push16(outPtr);               // out word
            Stack.Push16((ushort)encounter);    // encounter number

            // Far return address, so the thunk's RETF lands back on this instruction.
            Stack.Push16(State.CS);
            Stack.Push16(State.IP);

            // j_runCombatEncounter is IDA 0x3909F; any (para, offset) pair that resolves to it
            // works, because RuntimeSegment applies one uniform relocation delta to the resident
            // segments. 0x3909:000F is the tidiest split.
            State.CS = RuntimeSegment(0x3909);
            State.IP = 0x000F;

            _loggerService.LogInformation(
                "Combat spike: calling runCombatEncounter({Encounter}) directly", encounter);
        });
    }

    /// <summary>
    /// Opt-in harness that fires a TRAP TRIGGER directly: set <c>BAK_TRAP_TRIGGER=entryNumber</c>
    /// and the next world frame runs <c>trapTrigger_phase2</c> for that DEF_TRAP entry.
    /// </summary>
    /// <remarks>
    /// <b>This is the refinement of <see cref="DefineCombatSpike" />, which does not work.</b>
    /// Calling <c>runCombatEncounter</c> straight blew the emulator's call stack, because its real
    /// caller does a great deal of setup first. <c>trapTrigger_phase2</c> IS that caller, so
    /// calling it instead gets the setup for free: it reads the DEF_TRAP entry, sets
    /// <c>creatureType</c>, runs the stealth roll, shows the dialog and relocates the party before
    /// entering combat.
    ///
    /// <para>Signature from its only in-game call site, <c>dispatchTriggers_phase2</c> @0x73be0:
    /// <code>
    ///   lea ax,[bp+outPbool]; push ax    ; arg2, near ptr to a bool
    ///   push si                          ; arg1, near ptr to a def_file_struct
    ///   call trapTrigger_phase2
    ///   add sp, 4
    /// </code></para>
    ///
    /// <para>The <c>def_file_struct</c> is 19 bytes — <c>type</c> at +0 (7 = Trap),
    /// <c>entryNumber</c> at +6 — and is built in the game's own <c>defFileStructs</c> array
    /// (@0x3f0e6) rather than on the stack, so every pointer convention the callee assumes still
    /// holds. The near offset is derived at runtime from <c>IdaLinear</c> minus <c>DS &lt;&lt; 4</c>
    /// rather than hand-computed, because hand-computing IDA-to-runtime addresses has cost this
    /// project two sessions before.</para>
    ///
    /// <para>Combine with <c>BAK_TRAP_ENCOUNTER</c> to force which encounter the trap opens: any
    /// real entry number will do, since the encounter is rewritten at Load_traps.dat.</para>
    /// </remarks>
    private void DefineTrapTriggerSpike() {
        if (!int.TryParse(Environment.GetEnvironmentVariable("BAK_TRAP_TRIGGER"),
                out int entryNumber) || entryNumber < 0) {
            return;
        }

        _loggerService.LogInformation("Trap-trigger spike: armed (DEF_TRAP entry {Entry})", entryNumber);

        var entered = false;
        DoOnTopOfInstructionIda(0x2C97, 0x000F, () => {
            if (entered) {
                return;
            }
            entered = true;

            uint structLinear = IdaLinear(IdaDefFileStructs);
            var nearStruct = (ushort)(structLinear - ((uint)State.DS << 4));

            // Build a Trap def_file_struct in the game's own array slot 0.
            UInt16[structLinear + 0x00] = 7;                       // type = Trap
            Memory.UInt8[structLinear + 0x02] = 0;                 // start_x
            Memory.UInt8[structLinear + 0x03] = 39;                // end_y
            Memory.UInt8[structLinear + 0x04] = 39;                // end_x
            Memory.UInt8[structLinear + 0x05] = 0;                 // start_y
            Memory.UInt32[structLinear + 0x06] = (uint)entryNumber;// entryNumber
            Memory.UInt8[structLinear + 0x0A] = 0;                 // fireOnce
            UInt16[structLinear + 0x0B] = 0;                       // requiredKey
            UInt16[structLinear + 0x0D] = 0;                       // forbiddenKey
            UInt16[structLinear + 0x0F] = 0;                       // setOnFireKey
            UInt16[structLinear + 0x11] = 0;                       // repeatable

            // Scratch bool for the out-parameter, on the stack. Sound as a near pointer only
            // because DS == SS in this program (both 0x2B5A, confirmed at a breakpoint).
            State.SP -= 2;
            ushort outPtr = State.SP;

            Stack.Push16(outPtr);      // arg2 pushed first
            Stack.Push16(nearStruct);  // arg1
            Stack.Push16(State.CS);    // far return
            Stack.Push16(State.IP);

            State.CS = RuntimeSegment(0x39A7);   // j_trapTrigger_phase2 @0x39A75
            State.IP = 0x0005;

            _loggerService.LogInformation(
                "Trap-trigger spike: calling trapTrigger_phase2(entry {Entry}) at DS:{Ptr:X4}",
                entryNumber, nearStruct);
        });
    }

    private void DefineChapterSpike() {
        // *** EITHER VARIABLE ARMS THE HARNESS, AND THAT IS THE FIX FOR A REAL TRAP. ***
        // This used to return unless BAK_SPIKE_CHAPTER was set, which silently disarmed
        // BAK_SPIKE_LOADSAVE too — every override below, the intro skip and the menu answer
        // included. So `BAK_SPIKE_LOADSAVE=probe:1:0` on its own did NOTHING: the game played its
        // full title sequence, never reached the main menu, and never loaded. It looks exactly like
        // a broken save-load path, and cost two sessions before the gate was read.
        //
        // The chapter spike still needs its own number; the load spike does not need a chapter.
        bool haveChapter =
            int.TryParse(Environment.GetEnvironmentVariable("BAK_SPIKE_CHAPTER"), out int chapter)
            && chapter is >= 1 and <= 9;
        bool haveLoad = !IsNullOrEmpty(Environment.GetEnvironmentVariable("BAK_SPIKE_LOADSAVE"));
        if (!haveChapter && !haveLoad) {
            return;
        }

        _loggerService.LogInformation(
            "Chapter spike: armed (chapter {Chapter}, load spec {Load})",
            haveChapter ? chapter.ToString() : "none",
            Environment.GetEnvironmentVariable("BAK_SPIKE_LOADSAVE") ?? "none");

        // PlayIntro @seg020:0x038C runs the Sierra logo and the animated intro before the menu is
        // ever shown, and skipping it by keyboard is timing-dependent (tried; unreliable). Stub it
        // to a far return — `push cs; call near ptr PlayIntro` at seg020:0x0E4A returns far.
        DefineFunctionIda(Seg020, 0x038C, SkipIntro, true, nameof(SkipIntro));

        // showMainMenu BLOCKS waiting for input, so a hook on `mov si, ax` at seg020:0x0E5E (which
        // is what _main does with the result) never runs — the menu never returns on its own.
        // Replace the menu itself instead: _main calls it at seg020:0x0E58 as `9A 2A 00 6F 39`, a
        // far call to 0x396F:0x002A. Returning mainMenu_newGame (2, from the `cmp si, 2` at
        // seg020:0x0E60) takes the new-game branch with no keyboard timing involved.
        // Once only: _main loops back to the menu when the chapter ends, and it should answer for
        // itself after that rather than restarting the chapter forever.
        DefineFunctionIda(0x396F, 0x002A, ChooseNewGameOnce, true, nameof(ChooseNewGameOnce));

        // playChapterAnimationsAndBook @seg020:0x05AA — past `push bp; mov bp, sp`, so the far-call
        // frame is addressable. IDA lists chapterNrP at frame offset 0x20, but those offsets are
        // relative to the bottom of the local area, not to BP: __saved_registers sits at 0x1A and
        // IS bp+0, so the arguments start at bp+6. Reading bp+0x20 returned garbage (19292).
        DoOnTopOfInstructionIda(Seg020, 0x05AA, () => {
            uint chapterArg = MemoryUtils.ToPhysicalAddress(State.SS, (ushort)(State.BP + 6));
            if (UInt16[chapterArg] == chapter) {
                return;
            }
            _loggerService.LogInformation("Chapter spike: playChapterAnimationsAndBook chapter {Was} -> {Now}",
                UInt16[chapterArg], chapter);
            UInt16[chapterArg] = (ushort)chapter;
        });


        // Frame capture for the chapter's STORY scene (C<chapter>1.ADS), not the title card.
        // playChapterAnimationsAndBook runs two animation loops: the first plays CHAPTER<n>.ADS, the
        // second -- after the book -- plays C<n>1.ADS. seg020:0x06FA is the second loop's
        // `call j_animationStateMachine` (IDA's own label there is loc_seg020_6FA), reached only
        // AFTER that frame's SwapDisplayBuffer and blit have run, so the captured buffer is a
        // finished frame. Set BAK_SPIKE_FRAMEDIR to dump every one of them.
        string frameDir = Environment.GetEnvironmentVariable("BAK_SPIKE_FRAMEDIR");
        if (!IsNullOrEmpty(frameDir)) {
            Directory.CreateDirectory(frameDir);
            _frameDir = frameDir;
            DoOnTopOfInstructionIda(Seg020, 0x06FA, CaptureStorySceneFrame);
        }

        // playChapterBook @seg020:0x04F1 sits between the title animation and the story scene and
        // blocks on page turns. Replace it with a far return (the `push cs; call near ptr` at
        // seg020:0x066C returns far, and the caller does its own `add sp, 4`) so the capture reaches
        // C<chapter>1.ADS unattended. Returns 0 = the book was not aborted.
        DefineFunctionIda(Seg020, 0x04F1, SkipChapterBook, true, nameof(SkipChapterBook));
    }

    private bool _menuForced;

    // saveDirectoryName @dseg:0x3F04E, directoryNumber @0x3F063, saveGameNumber @0x3F061 --
    // StartGameOrLoadSave(3) formats them as "GAMES\\%s.G%02d\\SAVE%02d.GAM" (seg020:0x0095).
    private const uint SaveDirectoryNameIda = 0x3F04E;
    private const uint DirectoryNumberIda = 0x3F063;
    private const uint SaveGameNumberIda = 0x3F061;

    /// <summary>
    /// Point the save-load globals at one of the saves shipped in OriginalGame/GAMES, so
    /// StartGameOrLoadSave(3) restores it. Normally the load DIALOG fills these in; this skips the
    /// dialog entirely. Format is BAK_SPIKE_LOADSAVE=&lt;dirName&gt;:&lt;dirNumber&gt;:&lt;saveNumber&gt;,
    /// e.g. "dir:1:0" for GAMES/dir.G01/SAVE00.GAM.
    /// </summary>
    private bool PointAtSaveGame(string spec) {
        string[] parts = spec.Split(':');
        if (parts.Length != 3 || !ushort.TryParse(parts[1], out ushort dirNumber) || !ushort.TryParse(parts[2], out ushort saveNumber)) {
            _loggerService.LogWarning("Chapter spike: BAK_SPIKE_LOADSAVE must be <dirName>:<dirNumber>:<saveNumber>, got {Spec}", spec);

            return false;
        }
        uint nameAddr = IdaLinear(SaveDirectoryNameIda);
        for (int i = 0; i < parts[0].Length; i++) {
            UInt8[(uint)(nameAddr + i)] = (byte)parts[0][i];
        }
        UInt8[(uint)(nameAddr + parts[0].Length)] = 0;
        UInt16[IdaLinear(DirectoryNumberIda)] = dirNumber;
        UInt16[IdaLinear(SaveGameNumberIda)] = saveNumber;
        _loggerService.LogInformation("Chapter spike: restoring GAMES/{Dir}.G{DirNum:D2}/SAVE{SaveNum:D2}.GAM",
            parts[0], dirNumber, saveNumber);

        return true;
    }

    private Action ChooseNewGameOnce(int _) {
        if (!_menuForced) {
            string loadSpec = Environment.GetEnvironmentVariable("BAK_SPIKE_LOADSAVE");
            if (!IsNullOrEmpty(loadSpec) && PointAtSaveGame(loadSpec)) {
                _menuForced = true;
                // mainMenu_loadGame -- _main passes this straight to StartGameOrLoadSave (seg020:0x0EC9).
                State.AX = 3;

                return FarRet();
            }
        }
        if (_menuForced) {
            // Hand the menu back to the player: re-running the original is not possible from here,
            // so answer 6 ("show the menu again"), the value _main itself starts with.
            State.AX = 6;

            return FarRet();
        }
        _menuForced = true;
        _loggerService.LogInformation("Chapter spike: answering showMainMenu with newGame(2)");
        State.AX = 2;

        return FarRet();
    }

    private string _frameDir;
    private int _storySceneFrame;

    /// <summary>
    /// Writes the emulator's current frame to <c>_frameDir</c> as a PNG, numbered in playback order.
    /// Same encode the MCP screenshot tool uses (BGRA8888 straight out of the VGA renderer), so the
    /// output is comparable with it -- but taken from a known point in the animation loop rather
    /// than whenever an external tool happens to ask, which is what makes "the Nth frame" meaningful.
    /// </summary>
    /// <summary>
    /// Writes the emulator's current frame to <paramref name="fileName"/> under
    /// <paramref name="directory"/>.
    /// </summary>
    /// <remarks>
    /// BGRA8888 straight out of the VGA renderer — the same encode the MCP screenshot tool uses, so
    /// the output is comparable with it. The value of taking it from here rather than through that
    /// tool is WHEN: an override fires at a known instruction, while an external tool captures
    /// whenever it happens to ask, which for anything mid-animation is not a repeatable moment.
    /// </remarks>
    private void CaptureFrameTo(string directory, string fileName) {
        int width = Machine.VgaRenderer.Width;
        int height = Machine.VgaRenderer.Height;
        uint[] buffer = new uint[width * height];
        Machine.VgaRenderer.CopyLastFrame(buffer);

        byte[] bytes = new byte[buffer.Length * 4];
        Buffer.BlockCopy(buffer, 0, bytes, 0, bytes.Length);

        SKImageInfo imageInfo = new(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        using SKBitmap bitmap = new(imageInfo);
        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, bitmap.GetPixels(), bytes.Length);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
        if (png == null) {
            return;
        }
        File.WriteAllBytes(Path.Combine(directory, fileName), png.ToArray());
    }

    private void CaptureStorySceneFrame() {
        _storySceneFrame++;
        CaptureFrameTo(_frameDir, $"frame_{_storySceneFrame:D4}.png");
        if (_storySceneFrame % 25 == 0) {
            _loggerService.LogInformation("Chapter spike: captured story frame {Frame}", _storySceneFrame);
        }
    }

    private Action SkipIntro(int _) {
        _loggerService.LogInformation("Chapter spike: skipping PlayIntro");

        return FarRet();
    }

    private Action SkipChapterBook(int _) {
        _loggerService.LogInformation("Chapter spike: skipping playChapterBook");
        State.AX = 0;

        return FarRet();
    }

    private Action LoadConfig(int _) {
        _loggerService.LogInformation("LoadConfig override entered");
        string resourceConfigFilePath = _gameEngine.DataPath + "/resource.cfg";
        if (File.Exists(resourceConfigFilePath)) {
            LoadResourceConfig(resourceConfigFilePath);
        }
        string driveConfigFilePath = _gameEngine.DataPath + "/drive.cfg";
        if (File.Exists(driveConfigFilePath)) {
            LoadDriveConfig(driveConfigFilePath);
        }

        return FarRet();
    }

    private void LoadDriveConfig(string driveConfigFilePath) {
        string[] lines = File.ReadAllLines(driveConfigFilePath);
        _globalSettings.DriveLetter = lines[0].Trim()[0];
        _globalSettings.CdRomDriveLetter = lines[1].Trim()[0];
    }

    private void LoadResourceConfig(string resourceConfigFilePath) {
        string[] lines = File.ReadAllLines(resourceConfigFilePath);

        foreach (string line in lines) {
            string[] splits = line.ToLower().Split('=', StringSplitOptions.TrimEntries);
            if (splits.Length != 2) {
                continue;
            }
            string key = splits[0];
            string value = splits[1];
            switch (key) {
                case "sounddrv":
                    SoundDriverType soundDriverType = value switch {
                        "adl.drv" => SoundDriverType.AdLib,
                        "mt32.drv" => SoundDriverType.Mt32,
                        "sndblast.drv" => SoundDriverType.SoundBlaster,
                        "std.drv" => SoundDriverType.Standard,
                        "genmidi.drv" => SoundDriverType.GeneralMidi,
                        _ => SoundDriverType.None
                    };
                    _globalSettings.SoundDriverType = soundDriverType;

                    break;
                case "knockknock":
                    if (value.Length == 29) {
                        _globalSettings.KnockKnock = true;
                    }

                    break;
                case "cycle":
                    _globalSettings.Cycles = int.Parse(value);

                    break;
                case "tempdrive":
                    _globalSettings.TempDrive = value.ToUpper()[0];

                    break;
                case "bookmarkverify":
                    _globalSettings.BookmarkVerify = int.Parse(value) != 0;

                    break;
                case "nonrotatingmap":
                    _globalSettings.NonRotatingMap = int.Parse(value) != 0;

                    break;
            }
        }
    }

    private Action SetMouseCursorRange(int _) {
        int minCol = Stack.Peek16(4) * 4;
        int minRow = Stack.Peek16(6) * 4;
        int maxCol = Stack.Peek16(8) * 4;
        int maxRow = Stack.Peek16(10) * 4;

        _gameEngine.SetMouseCursorArea(minCol, minRow, maxCol, maxRow);

        return FarRet();
    }
}

internal record OvrBreakpoint(ushort IdaSegment, ushort Offset, Action Action);

internal class DialogEntry : MemoryBasedDataStructure {
    public DialogEntry(IByteReaderWriter byteReaderWriter, uint baseAddress) : base(byteReaderWriter, baseAddress) {
        uint address = baseAddress + 9;
        DialogBranchDataArray = new DialogBranchData[BranchCount];
        for (uint i = 0; i < BranchCount; i++) {
            DialogBranchDataArray[i] = new DialogBranchData(byteReaderWriter, address);
            address += 10;
        }
        DialogActionArray = new DialogAction[ActionCount];
        for (uint i = 0; i < ActionCount; i++) {
            DialogActionArray[i] = new DialogAction(byteReaderWriter, address);
            address += 10;
        }
    }

    public byte Field_0 { get => UInt8[0x00]; set => UInt8[0x00] = value; }
    public ushort Field_1 { get => UInt16[0x01]; set => UInt16[0x01] = value; }
    public ushort Field_3 { get => UInt16[0x03]; set => UInt16[0x03] = value; }
    public byte BranchCount { get => UInt8[0x05]; set => UInt8[0x05] = value; }
    public byte ActionCount { get => UInt8[0x06]; set => UInt8[0x06] = value; }
    public ushort StringLength { get => UInt16[0x07]; set => UInt16[0x07] = value; }
    public DialogBranchData[] DialogBranchDataArray { get; set; }
    public DialogAction[] DialogActionArray { get; set; }

    public string Text {
        get => GetZeroTerminatedString((uint)(9 + 10 * BranchCount + 10 * ActionCount), StringLength);
        set => SetZeroTerminatedString((uint)(9 + 10 * BranchCount + 10 * ActionCount), value, value.Length);
    }

    public override string ToString() {
        StringBuilder sb = new();
        sb.AppendLine(
            $"Field_0: {Field_0}, Field_1: {Field_1}, Field_3: {Field_3}, BranchCount: {BranchCount}, ActionCount: {ActionCount}, StringLength: {StringLength}, Text: {Text}\n");
        foreach (DialogBranchData branchData in DialogBranchDataArray) {
            sb.AppendLine(branchData.ToString());
        }
        foreach (DialogAction data in DialogActionArray) {
            sb.AppendLine(data.ToString());
        }

        return sb.ToString();
    }
}

internal class DialogAction(IByteReaderWriter byteReaderWriter, uint baseAddress)
    : MemoryBasedDataStructure(byteReaderWriter, baseAddress) {
    public ushort Field_0 { get => UInt16[0x00]; set => UInt16[0x00] = value; }
    public ushort Field_2 { get => UInt16[0x02]; set => UInt16[0x02] = value; }
    public ushort Field_4 { get => UInt16[0x04]; set => UInt16[0x04] = value; }
    public ushort Field_6 { get => UInt16[0x06]; set => UInt16[0x06] = value; }
    public ushort Field_8 { get => UInt16[0x08]; set => UInt16[0x08] = value; }

    public override string ToString() {
        return $"Field_0: {Field_0}, Field_2: {Field_2}, Field_4: {Field_4}, Field_6: {Field_6}, Field_8: {Field_8}";
    }
}

internal class DialogBranchData(IByteReaderWriter byteReaderWriter, uint baseAddress)
    : MemoryBasedDataStructure(byteReaderWriter, baseAddress) {
    public ushort Unknown2 { get => UInt16[0x00]; set => UInt16[0x00] = value; }
    public ushort Unknown3 { get => UInt16[0x02]; set => UInt16[0x02] = value; }
    public ushort Unknown4 { get => UInt16[0x04]; set => UInt16[0x04] = value; }
    public uint Offset { get => UInt32[0x06]; set => UInt32[0x06] = value; }

    public override string ToString() {
        return $"Unknown2: {Unknown2}, Unknown3: {Unknown3}, Unknown4: {Unknown4}, Offset: {Offset:X8}";
    }
}