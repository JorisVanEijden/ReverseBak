namespace BetrayalAtKrondor;

using BetrayalAtKrondor.Overrides.Libraries;
using GameData;
using Serilog.Events;
using Spice86.Core.CLI;
using Spice86.Core.Emulator.Function;
using Spice86.Core.Emulator.Memory.ReaderWriter;
using Spice86.Core.Emulator.ReverseEngineer;
using Spice86.Core.Emulator.ReverseEngineer.DataStructure;
using Spice86.Core.Emulator.VM;
using Spice86.Shared.Emulator.Memory;
using Spice86.Shared.Interfaces;
using Spice86.Shared.Utils;
using System.Text;
using ArgumentFetcher = Spice86.Core.Emulator.ReverseEngineer.ArgumentFetcher;

public class BakOverrides : CSharpOverrideHelper {
    private readonly IGameEngine _gameEngine;
    private readonly IGlobalSettings _globalSettings;
    private readonly List<OvrBreakpoint> _ovrBreakpoints = [];
    private Dictionary<ushort, ushort> _stubSegments;
    private readonly Dictionary<ushort, ushort> _ovrSegmentMapping = [];
    private readonly ArgumentFetcher _args;
    private readonly IPauseHandler _pauseHandler;
    private Dictionary<uint, byte> _wordLowByteWrites = [];

    public BakOverrides(Dictionary<SegmentedAddress, FunctionInformation> functionsInformation, Machine machine, ILoggerService loggerService, Configuration configuration)
        : base(functionsInformation, machine, loggerService.WithLogLevel(LogEventLevel.Debug), configuration) {
        _globalSettings = new GlobalSettings(machine.Memory);
        _gameEngine = new GameEngine(machine.MouseDriver);
        _gameEngine.DataPath = configuration.Exe is null
            ? Directory.GetCurrentDirectory()
            : Path.GetDirectoryName(configuration.Exe);
        _ = new StdIO(functionsInformation, machine, loggerService.WithLogLevel(LogEventLevel.Information), configuration);
        _args = new ArgumentFetcher(machine.Cpu, machine.Memory);
        _pauseHandler = machine.PauseHandler;
        DefineStubMapping();
        DefineFunctions();
        DefineBreakpoints();
    }

    // This is the mapping between the OVR segments and the stub segments. ovr151 maps to stub151, etc.
    private void DefineStubMapping() {
        _stubSegments = new Dictionary<ushort, ushort> {
            [0x3FF7] = 0x3817,
            [0x4028] = 0x381A,
            [0x4040] = 0x381E,
            [0x4041] = 0x3820,
            [0x4042] = 0x3822,
            [0x4043] = 0x3824,
            [0x4052] = 0x3827,
            [0x40A6] = 0x382B,
            [0x4162] = 0x382F,
            [0x41C0] = 0x3836,
            [0x4225] = 0x3839,
            [0x42EA] = 0x3840,
            [0x438F] = 0x3846,
            [0x43A4] = 0x3849,
            [0x43CB] = 0x384C,
            [0x43F6] = 0x384F,
            [0x4513] = 0x3859,
            [0x45E8] = 0x385F,
            [0x464B] = 0x3862,
            [0x469F] = 0x3868,
            [0x476E] = 0x3873,
            [0x478C] = 0x3877,
            [0x480E] = 0x387F,
            [0x4A8B] = 0x3887,
            [0x4B6D] = 0x388C,
            [0x4BB3] = 0x388F,
            [0x4C5C] = 0x3893,
            [0x4CB8] = 0x3897,
            [0x4D7D] = 0x389A,
            [0x4EBA] = 0x389E,
            [0x5040] = 0x38A2,
            [0x51FD] = 0x38AD,
            [0x5278] = 0x38B4,
            [0x53DD] = 0x38BA,
            [0x53DF] = 0x38BD,
            [0x540F] = 0x38C0,
            [0x5421] = 0x38C3,
            [0x5605] = 0x38CA,
            [0x577A] = 0x38D0,
            [0x57BF] = 0x38D6,
            [0x58B9] = 0x38D9,
            [0x59D6] = 0x38DE,
            [0x5AAB] = 0x38E1,
            [0x5B3E] = 0x38E6,
            [0x5BBA] = 0x38EA,
            [0x5BCA] = 0x38ED,
            [0x5C16] = 0x38F1,
            [0x5F2C] = 0x3907,
            [0x6300] = 0x3913,
            [0x64A9] = 0x3923,
            [0x6571] = 0x392A,
            [0x65F7] = 0x3931,
            [0x6670] = 0x3938,
            [0x6A36] = 0x3947,
            [0x6A70] = 0x394A,
            [0x6B0F] = 0x3950,
            [0x6B41] = 0x3953,
            [0x6C1A] = 0x395C,
            [0x6CA3] = 0x3961,
            [0x6D38] = 0x396A,
            [0x6DDF] = 0x396F,
            [0x703D] = 0x397B,
            [0x70F6] = 0x3981,
            [0x7179] = 0x3985,
            [0x72A0] = 0x3991,
            [0x7307] = 0x3998,
            [0x7395] = 0x39A0,
            [0x7517] = 0x39B1,
            [0x7650] = 0x39BB,
            [0x7651] = 0x39BD,
            [0x78C6] = 0x39C9,
            [0x7981] = 0x39CE,
            [0x799E] = 0x39D1,
            [0x79A7] = 0x39D4,
            [0x79AE] = 0x39D7,
            [0x7A16] = 0x39DA,
        };
    }

    private void LogDialogBuildCall() {
        _loggerService.Information("dialog_Build?Show? called: dialogIdOrOffset: {Arg0}, arg_4: {Arg4}",
            Stack.Peek32(4), Stack.Peek16(8));
    }

    private void DefineBreakpoints() {
        DoOnTopOfInstruction("36BC:069A", RecordOvrChange);
        // DoOnTopOfInstruction("1834:22CC", LogAllocateMemory);

        // DoOnTopOfInstruction("3839:0020", LogGetGlobalValue);
        // DoOnTopOfInstruction("3839:0025", LogSetGlobalValue);

        AddWordWriteMemoryMonitor("39DD:4F70", "currentAnimFunctionId");

        // DoOnTopOfInstruction("5278:0540", LogAx("framenumber"));

        // AddWordReadMemoryMonitor("39DD:20D0", "BUFFER_C");
        // AddWordReadMemoryMonitor("39DD:20D2", "BUFFER_B");
        // AddWordReadMemoryMonitor("39DD:20D4", "BUFFER_A");
        // AddWordReadMemoryMonitor("39DD:20D6", "BUFFER_1");
        // AddWordReadMemoryMonitor("39DD:20D8", "BUFFER_2");
        // AddWordWriteMemoryMonitor("39DD:20D0", "VGA_videoBuffer_C");
        // AddWordWriteMemoryMonitor("39DD:20D2", "VGA_videoBuffer_B");
        // AddWordWriteMemoryMonitor("39DD:20D4", "VGA_videoBuffer_A");
        // AddWordWriteMemoryMonitor("39DD:20D6", "VGA_videoBuffer_1");
        // AddWordWriteMemoryMonitor("39DD:20D8", "VGA_videoBuffer_2");

        // PauseAt("5278:0543", "anim_executeFrameFunctions");
    }

    private void PauseAt(string address, string message) {
        DoOnTopOfInstruction(address, () => {
            _pauseHandler.RequestPause(message);
        });
    }

    private void LogAllocateMemory() {
        _args.Get(out uint sizeInBytes, out uint boolClear);
        _loggerService.Information("AllocateMemory({BoolClear}, {SizeInBytes})",
            boolClear, sizeInBytes);
    }

    private void AddDWordMemoryMonitor(string address, string name) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);
        DoOnMemoryWrite(segment, (ushort)(offset + 3), () => {
            _ovrSegmentMapping.TryGetValue(State.CS, out ushort idaSegment);
            _loggerService.Information("[{IdaSegment:X4}:{IdaOffset:X4}] {Name} Memory write at {Segment:X4}:{Offset:X4}: {ValueSegment:X4}:{ValueOffset:X4}",
                idaSegment, State.IP, name, segment, offset, Memory.UInt16[segment, (ushort)(offset + 2)], Memory.UInt16[segment, offset]);
        });
    }

    private Action LogStringAt(string address) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);

        return () => {
            var stringAddress = MemoryUtils.ToPhysicalAddress(segment, offset);
            _loggerService.Information("{Segment:X4}:{Offset:X4} = {Value}", segment, offset, Memory.GetZeroTerminatedString(stringAddress, 100));
        };
    }

    private void AddWordWriteMemoryMonitor(string address, string? name = null) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);
        DoOnMemoryWrite(segment, offset, () => {
            _wordLowByteWrites[MemoryUtils.ToPhysicalAddress(segment, offset)] = Memory.CurrentlyWritingByte;
        });
        DoOnMemoryWrite(segment, (ushort)(offset + 1), () => {
            uint physicalAddress = MemoryUtils.ToPhysicalAddress(segment, offset);
            if (_wordLowByteWrites.TryGetValue(physicalAddress, out byte lowByte))
            {
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
        var stubSegment = State.ES;
        var realSegment = State.BX;
        foreach (var ovrBreakpoint in _ovrBreakpoints) {
            if (_stubSegments[ovrBreakpoint.Segment] == stubSegment) {
                _loggerService.Information("OVR Mapping {SourceSegment:X4}:{SourceOffset:X4} to {DestinationSegment:X4}:{DestinationOffset:X4}",
                    ovrBreakpoint.Segment, ovrBreakpoint.Offset, realSegment, ovrBreakpoint.Offset);
                _ovrSegmentMapping[realSegment] = ovrBreakpoint.Segment;
                DoOnTopOfInstruction(realSegment, ovrBreakpoint.Offset, ovrBreakpoint.Action);
            }
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

        _loggerService.Information("{MethodName} called. zoneNumber: {ZoneNumber}, xCoordinate: {XCoordinate}, yCoordinate: {YCoordinate}, arg_6: {Arg6}",
            nameof(LogLoadTzzxxyy_WLD), zoneNumber, xCoordinate, yCoordinate, arg6);
    }

    private void LogGetGlobalValue() {
        _loggerService.Information("GetGlobalValue(key: {Arg0})",
            Stack.Peek16(4));
    }

    private void LogSetGlobalValue() {
        _loggerService.Information("SetGlobalValue(key: {Arg0}, value: {Arg2:X4})",
            Stack.Peek16(4), Stack.Peek16(6));
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

        _loggerService.Debug("{MethodName}: actor: {Name}, arg_2: {Arg2:X4}, arg_4: {Arg4:X4}, {Attribute}: {AttributeValue} ({Value1} {Value2} {Value3} {Value4} {Value5})",
            nameof(LogGetValueFromActor), name, arg2, arg4, attribute, attributeValue, attributeValue1, attributeValue2, attributeValue3, attributeValue4, attributeValue5);
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
        _loggerService.Information("getKeyWordTableOffsetForDialogField1 called: value: {Arg0}",
            Stack.Peek16(4));
    }

    private void DoOnTopOfInstruction(string address, Action action) {
        (ushort segment, ushort offset) = ToSegmentOffset(address);

        // If segment >= ovr121  (0x3FF7) then it's an overlay
        // Look up segment in ovr table
        if (segment is >= 0x3FF7 and < 0x5ADE) {
            // We add it to the list, and when the OVR gets mapped, the real breakpoint is added.
            _ovrBreakpoints.Add(new OvrBreakpoint(segment, offset, action));

            return;
        }

        DoOnTopOfInstruction(segment, offset, action);
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

    private void DefineFunctions() {
        DefineFunction(0x3849, 0x0020, LoadConfig, true, nameof(LoadConfig));
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

internal record OvrBreakpoint(ushort Segment, ushort Offset, Action Action);

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
        sb.AppendLine($"Field_0: {Field_0}, Field_1: {Field_1}, Field_3: {Field_3}, BranchCount: {BranchCount}, ActionCount: {ActionCount}, StringLength: {StringLength}, Text: {Text}\n");
        foreach (DialogBranchData branchData in DialogBranchDataArray) {
            sb.AppendLine(branchData.ToString());
        }
        foreach (DialogAction data in DialogActionArray) {
            sb.AppendLine(data.ToString());
        }

        return sb.ToString();
    }
}

internal class DialogAction(IByteReaderWriter byteReaderWriter, uint baseAddress) : MemoryBasedDataStructure(byteReaderWriter, baseAddress) {
    public ushort Field_0 { get => UInt16[0x00]; set => UInt16[0x00] = value; }
    public ushort Field_2 { get => UInt16[0x02]; set => UInt16[0x02] = value; }
    public ushort Field_4 { get => UInt16[0x04]; set => UInt16[0x04] = value; }
    public ushort Field_6 { get => UInt16[0x06]; set => UInt16[0x06] = value; }
    public ushort Field_8 { get => UInt16[0x08]; set => UInt16[0x08] = value; }

    public override string ToString() {
        return $"Field_0: {Field_0}, Field_2: {Field_2}, Field_4: {Field_4}, Field_6: {Field_6}, Field_8: {Field_8}";
    }
}

internal class DialogBranchData(IByteReaderWriter byteReaderWriter, uint baseAddress) : MemoryBasedDataStructure(byteReaderWriter, baseAddress) {
    public ushort Unknown2 { get => UInt16[0x00]; set => UInt16[0x00] = value; }
    public ushort Unknown3 { get => UInt16[0x02]; set => UInt16[0x02] = value; }
    public ushort Unknown4 { get => UInt16[0x04]; set => UInt16[0x04] = value; }
    public uint Offset { get => UInt32[0x06]; set => UInt32[0x06] = value; }

    public override string ToString() {
        return $"Unknown2: {Unknown2}, Unknown3: {Unknown3}, Unknown4: {Unknown4}, Offset: {Offset:X8}";
    }
}