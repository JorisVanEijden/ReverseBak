namespace BetrayalAtKrondor;

using BetrayalAtKrondor.Overrides.Libraries;

using Serilog.Events;

using Spice86.Core.CLI;
using Spice86.Core.Emulator;
using Spice86.Core.Emulator.Function;
using Spice86.Core.Emulator.ReverseEngineer;
using Spice86.Core.Emulator.VM;
using Spice86.Core.Emulator.VM.Breakpoint;
using Spice86.Shared.Emulator.Memory;
using Spice86.Shared.Interfaces;
using Spice86.Shared.Utils;

public class BakOverrides : CSharpOverrideHelper {
    private readonly IGameEngine _gameEngine;
    private readonly IGlobalSettings _globalSettings;
    private readonly StdIO _stdIo;

    public BakOverrides(Dictionary<SegmentedAddress, FunctionInformation> functionsInformation, Machine machine, ILoggerService loggerService, Configuration configuration)
        : base(functionsInformation, machine, loggerService, configuration) {
        _globalSettings = new GlobalSettings(machine.Memory);
        _gameEngine = new GameEngine(machine.MouseDriver);
        _gameEngine.DataPath = configuration.Exe is null
            ? Directory.GetCurrentDirectory()
            : Path.GetDirectoryName(configuration.Exe);
        _stdIo = new StdIO(functionsInformation, machine, loggerService.WithLogLevel(LogEventLevel.Debug), configuration);
        DefineFunctions();
        DefineBreakpoints();
    }

    private void DefineBreakpoint(ushort segment, ushort offset, Action action) {
        long address = MemoryUtils.ToPhysicalAddress(segment, offset);
        var breakpoint = new AddressBreakPoint(BreakPointType.EXECUTION, address, _ => action.Invoke(), false);
        Machine.MachineBreakpoints.ToggleBreakPoint(breakpoint, true);
    }

    private void LogDialogBuildCall() {
        _loggerService.Information("dialog_Build?Show? called: dialogIdOrOffset: {Arg0}, arg_4: {Arg4}",
            Stack.Peek32(4), Stack.Peek16(8));
    }

    private void DefineBreakpoints() {
        DoOnTopOfInstruction(0x387F, 0x0020, () => Machine.MachineBreakpoints.PauseHandler.RequestPauseAndWait());
        // DoOnTopOfInstruction("3887:0034", LogField1KeyWordCall);
    }

    private void LogField1KeyWordCall() {
        _loggerService.Information("getKeyWordTableOffsetForDialogField1 called: value: {Arg0}",
            Stack.Peek16(4));
        Machine.MachineBreakpoints.PauseHandler.RequestPauseAndWait();
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
        DefineFunction(0x1834, 0x48EC, SetMouseCursorRange, true, nameof(SetMouseCursorRange));
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