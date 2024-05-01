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

public class BakOverrides : CSharpOverrideHelper {
    private readonly IGameEngine _gameEngine;
    private readonly IGlobalSettings _globalSettings;

    public BakOverrides(Dictionary<SegmentedAddress, FunctionInformation> functionsInformation, Machine machine, ILoggerService loggerService, Configuration configuration)
        : base(functionsInformation, machine, loggerService.WithLogLevel(LogEventLevel.Debug), configuration) {
        _globalSettings = new GlobalSettings(machine.Memory);
        _gameEngine = new GameEngine(machine.MouseDriver);
        _gameEngine.DataPath = configuration.Exe is null
            ? Directory.GetCurrentDirectory()
            : Path.GetDirectoryName(configuration.Exe);
        new StdIO(functionsInformation, machine, loggerService.WithLogLevel(LogEventLevel.Information), configuration);
        DefineFunctions();
        DefineBreakpoints();
    }

    private void LogDialogBuildCall() {
        _loggerService.Information("dialog_Build?Show? called: dialogIdOrOffset: {Arg0}, arg_4: {Arg4}",
            Stack.Peek32(4), Stack.Peek16(8));
    }

    private void DefineBreakpoints() {
        DoOnTopOfInstruction("387F:0020", LogDialogBuildCall);
        DoOnTopOfInstruction("3887:0025", Sub_4B54C);
        DoOnTopOfInstruction("3887:0034", LogField1KeyWordCall);
        DoOnTopOfInstruction("3840:0025", LogGetValueFromActor);
        DoOnTopOfInstruction("3839:0020", LogGetGlobalValue);
        DoOnTopOfInstruction("3839:0025", LogSetGlobalValue);
        DoOnTopOfInstruction("3991:0020", Logsub_ovr185_0);
        DoOnTopOfInstruction("3991:002F", LogLoadTzzxxyy_WLD);
        DoOnTopOfInstruction("3991:0034", Logsub_ovr185_33F);
        DoOnTopOfInstruction("3991:004D", Logsub_ovr185_53F);
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
        var parts = address.Split(':');
        ushort segment = (ushort)ParseHex(parts[0]);
        ushort offset = (ushort)ParseHex(parts[1]);
        DoOnTopOfInstruction(segment, offset, action);
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
        sb.AppendLine($"Field_0: {Field_0}, Field_1: {Field_1}, Field_3: {Field_3}, BranchCount: {BranchCount}, ActionCount: {ActionCount}, StringLength: {StringLength}, Text: {Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(Text, true)}\n");
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