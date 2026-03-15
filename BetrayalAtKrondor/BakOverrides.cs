namespace BetrayalAtKrondor;

using BetrayalAtKrondor.Mcp;
using BetrayalAtKrondor.Overrides.Libraries;
using GameData;
using Serilog.Events;
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
        ILoggerService loggerService, Configuration configuration, OverlayAddressTranslator translator) : base(functionsInformation,
        machine, loggerService, configuration) {
        _translator = translator;
        _globalSettings = new GlobalSettings(machine.Memory);
        _gameEngine = new GameEngine(machine.MouseDriver);
        _gameEngine.DataPath = configuration.Exe is null ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(configuration.Exe);
        _ = new StdIO(functionsInformation, machine, loggerService.WithLogLevel(LogEventLevel.Information), configuration);
        _args = new ArgumentFetcher(machine.Stack, machine.CpuState, machine.Memory);
        _pauseHandler = machine.PauseHandler;
        DefineFunctions();
        DefineBreakpoints();
    }

    private void LogDialogBuildCall() {
        _loggerService.Information("dialog_Build?Show? called: dialogIdOrOffset: {Arg0}, arg_4: {Arg4}", Stack.Peek32(4), Stack.Peek16(8));
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
        //     _loggerService.Information("word_dseg_2DE = 0x{Value:X4}", State.BX);
        //     _pauseHandler.RequestPause();
        // });
        //
        // PauseAt("1834:483E", "xywidthheight?");
        // PauseAt("1834:3F7B", "drawrotated?");
        // PauseAt("5278:10D2", "call what?");

        // DoOnTopOfInstruction("327D:0000", () => {
        //     _args.Get(out ushort stream, out uint nrOfBytes, out uint pBuffer);
        //     _loggerService.Information("[{Caller}] sub_seg045_0(stream: {Stream}, nrOfBytes: {NrOfBytes}, pBuffer: {PBuffer:X8})", CallerAddress(), stream, nrOfBytes, pBuffer);
        // });
        // DoOnTopOfInstruction("3239:000C", () => {
        //     _args.Get(out ushort stream, out byte arg2);
        //     _loggerService.Information("[{Caller}] resourceLoadSound(stream: {Stream}, arg2: {Arg2})", CallerAddress(), stream, arg2);
        // });
        // DoOnTopOfInstruction("3556:000F", () => {
        //     _args.Get(out ushort stream, out ushort soundId);
        //     _loggerService.Information("[{Caller}] resourceLoadSx(stream: {Stream}, soundId: {SoundId})", CallerAddress(), stream, soundId);
        //     // _pauseHandler.RequestPause("step from here");
        // });

        // DoOnTopOfInstruction("1834:7320", () => {
        //     _args.Get(out ushort index, out ushort offset, out ushort segment, out ushort size);
        //     _loggerService.Information("[{Caller}] resourceLoadCurrentFile(index: {Index}, pBuffer: {Segment:X4}:{Offset:X4}, size: {Size} (0x{SizeHex:X4}))", CallerAddress(), index, segment, offset, size, size);
        // });

        // DoOnTopOfInstruction("158B:0121", () => {
        //     _args.Get(out uint size, out ushort flag);
        //     _loggerService.Information("[{Caller}] audio_allocate_memory(flag: {Flag}, size: {Size} (0x{SizeHex:X8}))", CallerAddress(), flag, size, size);
        // });
        // DoOnTopOfInstruction("158B:01AF", () => {
        //     _loggerService.Information("[{Caller}] allocated audio memory at: {Segment:X4}:{Offset:X4}", CallerAddress(), State.DX, State.AX);
        // });

        // DoOnTopOfInstruction("1834:649F", () => {
        //     string dump = Stack.PeekWindow(6);
        //     _loggerService.Information("[{Caller}] {Dump}", CallerAddress(), dump);
        //     // _pauseHandler.RequestPause("check 39DD:5FA9");
        // });

        // DoOnTopOfInstruction("327D:0208", () => {
        //     _args.Get(out ushort index, out byte soundFormat);
        //     _loggerService.Information("[{Caller}] sub_seg045_208(index: {Index}, soundFormat: {Arg2:X2})", CallerAddress(), index, soundFormat);
        //     if (soundFormat != 0x0) {
        //         _loggerService.Warning("soundFormat = {SoundFormat:X2}", soundFormat);
        //     }
        //     // _pauseHandler.RequestPause("step from here");
        // });

        // DoOnTopOfInstruction("1834:7441", () => {
        //     _args.Get(out ushort index, out uint size, out ushort compressedMaybe);
        //     _loggerService.Information("[{Caller}] resourceReadFileData(index: {Index}, size: {Size} (0x{SizeHex:X8})), compressedMaybe: {CompressedMaybe})", CallerAddress(), index, size, size, compressedMaybe);
        // });

        // DoOnMemoryWrite(0x39DD, 0x3698, () => _pauseHandler.RequestPause("word_dseg_3698 was written to"));

// PauseAt("3239:0140", "step from here");
// PauseAt("35C1:0061", "check 3656:0002");

        // PauseAt("35D5:000D", "playing a sound");
        // PauseAt("3638:0071", "playing a sound continued");
        //
        // DoOnTopOfInstruction("32E5:000B", () => {
        //     _args.Get(out string path, out string tag, out ushort arg4);
        //     _loggerService.Information("[{Caller}] LoadSoundCodeOverlay(path: {Path}, tag: {Tag}, arg4: {Arg4})", CallerAddress(), path,
        //         tag, arg4);
        // });
        //
        // DoOnTopOfInstruction("8300:0000", () => {
        //     if (State.AX != 1) {
        //         _loggerService.Information("[{Caller}] Extra AudioDriver Dispatcher function {FunctionId:X2}", CallerAddress(), State.AX);
        //     }
        // });
        // DoOnTopOfInstruction("8000:0000", () => {
        //     if (State.BP != 3) {
        //         _loggerService.Information("[{Caller}] AudioDriver Dispatcher function {FunctionId:X2}", CallerAddress(), State.BP);
        //     }
        // });
    }

    private Action LogBx(string message) {
        return () => {
            _loggerService.Information("{Message} = 0x{Value:X4}", message, State.BX);
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
            _loggerService.Information("FrameCommand: [{Type:X4}] (End of frame)", type);

            return;
        }
        var command = ResourceExtraction.Extractors.Animation.TtmExtractor.GetFrameCommand(type, reader);
        _loggerService.Information("FrameCommand: [{Type:X4}] {Command}", type, command);
    }

    private void LogAt(string address, string message) {
        DoOnTopOfInstruction(address, () => {
            _loggerService.Information("{Message}", message);
        });
    }

    private void LogColorCycle() {
        _args.Get(out ushort start, out ushort length, out ushort color, out ushort blendAmount);
        _loggerService.Information(
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
        _loggerService.Information("AllocateMemory({BoolClear}, {SizeInBytes})", boolClear, sizeInBytes);
    }

    private void AddDWordMemoryMonitor(string address, string name) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);
        DoOnMemoryWrite(segment, (ushort)(offset + 3), () => {
            _ovrSegmentMapping.TryGetValue(State.CS, out ushort idaSegment);
            _loggerService.Information(
                "[{IdaSegment:X4}:{IdaOffset:X4}] {Name} Memory write at {Segment:X4}:{Offset:X4}: {ValueSegment:X4}:{ValueOffset:X4}",
                idaSegment, State.IP, name, segment, offset, Memory.UInt16[segment, (ushort)(offset + 2)], Memory.UInt16[segment, offset]);
        });
    }

    private Action LogStringAt(string address) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);

        return () => {
            var stringAddress = MemoryUtils.ToPhysicalAddress(segment, offset);
            _loggerService.Information("{Segment:X4}:{Offset:X4} = {Value}", segment, offset,
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
                _loggerService.Information("[{IdaSegment:X4}:{IdaOffset:X4}] {Name} Memory write at {Segment:X4}:{Offset:X4}: 0x{Value:X4}",
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
            _loggerService.Information("[{IdaSegment:X4}:{IdaOffset:X4}] {Name} Memory write at {Segment:X4}:{Offset:X4}: 0x{Value:X2}",
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
            _loggerService.Information("[{IdaSegment:X4}:{IdaOffset:X4}] {Name} Memory read at {Segment:X4}:{Offset:X4}: 0x{Value:X4}",
                idaSegment, State.IP, name, segment, offset, readValue);
        });
    }

    private void AddByteMemoryMonitor(string address, string? name = null) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);
        DoOnMemoryWrite(segment, offset, () => {
            _ovrSegmentMapping.TryGetValue(State.CS, out ushort idaSegment);
            _loggerService.Information("[{IdaSegment:X4}:{IdaOffset:X4}] {Name} Memory write at {Segment:X4}:{Offset:X4}: 0x{Value:X2}",
                idaSegment, State.IP, name, segment, offset, Memory.UInt8[segment, offset]);
        });
    }

    private Action LogAx(string message) {
        return () => {
            _loggerService.Information("{Message} = 0x{Value:X4}", message, State.AX);
        };
    }

    private Action LogDs(string message) {
        return () => {
            _loggerService.Information("{Message} = 0x{Value:X4}", message, State.DS);
        };
    }

    private Action LogMemoryAtAxDx(string? message = null) {
        return () => {
            if (message != null) {
                _loggerService.Information("{Message}:", message);
            }
            var address = MemoryUtils.ToPhysicalAddress(State.AX, State.DX);
            for (int i = -2; i < 20; i += 2) {
                _loggerService.Information("{Segment:X4}:{Offset:X4} = {Value:X4}", State.AX, State.DX + i, Memory.UInt16[address + i]);
            }
        };
    }

    private void LogEax() {
        _loggerService.Information("EAX: {Eax:X4}", State.EAX);
    }

    private void RecordOvrChange() {
        var runtimeStubSegment = State.ES;
        var runtimeSegment = State.BX;

        // Convert runtime stub segment to IDA stub segment by adding the segment delta (0xE830 >> 4 = 0xE83)
        ushort idaStubSegment = (ushort)(runtimeStubSegment + (OverlayAddressTranslator.RelocationDelta >> 4));

        if (StubSegments.IdaStubToIdaCode.TryGetValue(idaStubSegment, out ushort idaSegment)) {
            _ovrSegmentMapping[runtimeSegment] = idaSegment;
            _translator.RecordMapping(runtimeSegment, idaSegment);
            _loggerService.Debug("OVR Mapping real segment {SourceSegment:X4} to ida segment {DestinationSegment:X4}", runtimeSegment,
                idaSegment);
        }

        var ovrBreakpoint = _ovrBreakpoints.FirstOrDefault(breakpoint => StubSegments.IdaCodeToIdaStub[breakpoint.IdaSegment] == idaStubSegment);
        if (ovrBreakpoint is not null) {
            DoOnTopOfInstruction(runtimeSegment, ovrBreakpoint.Offset, ovrBreakpoint.Action);
        }
    }

    private void Logsub_ovr185_0() {
        _loggerService.Information("{MethodName} called", nameof(Logsub_ovr185_0));
    }

    private void Logsub_ovr185_33F() {
        _loggerService.Information("{MethodName} called", nameof(Logsub_ovr185_33F));
    }

    private void Logsub_ovr185_53F() {
        _loggerService.Information("{MethodName} called", nameof(Logsub_ovr185_53F));
    }

    private void LogLoadTzzxxyy_WLD() {
        var zoneNumber = Stack.Peek16(6);
        var xCoordinate = Stack.Peek16(8);
        var yCoordinate = Stack.Peek16(10);
        var arg6 = Stack.Peek16(12);

        _loggerService.Information(
            "{MethodName} called. zoneNumber: {ZoneNumber}, xCoordinate: {XCoordinate}, yCoordinate: {YCoordinate}, arg_6: {Arg6}",
            nameof(LogLoadTzzxxyy_WLD), zoneNumber, xCoordinate, yCoordinate, arg6);
    }

    private void LogGetGlobalValue() {
        _loggerService.Information("GetGlobalValue(key: {Arg0})", Stack.Peek16(4));
    }

    private void LogSetGlobalValue() {
        _loggerService.Information("SetGlobalValue(key: {Arg0}, value: {Arg2:X4})", Stack.Peek16(4), Stack.Peek16(6));
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

        _loggerService.Debug(
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
        _loggerService.Debug("{MethodName} called: dialogEntry {DialogEntry}", nameof(Sub_4B54C), dialogEntry);
    }

    private void LogField1KeyWordCall() {
        _loggerService.Information("getKeyWordTableOffsetForDialogField1 called: value: {Arg0}", Stack.Peek16(4));
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
        uint idaLinear = MemoryUtils.ToPhysicalAddress(idaSegment, offset);
        uint runtimeLinear = idaLinear - OverlayAddressTranslator.RelocationDelta;
        ushort runtimeSegment = (ushort)(runtimeLinear >> 4);
        ushort runtimeOffset = (ushort)(runtimeLinear & 0xF);
        DefineFunction(runtimeSegment, runtimeOffset, overrideFunc, failOnExisting, name);
    }

    private void DefineFunctions() {
        DefineFunctionIda(0x3849, 0x0020, LoadConfig, true, nameof(LoadConfig));
    }

    private Action LoadConfig(int _) {
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