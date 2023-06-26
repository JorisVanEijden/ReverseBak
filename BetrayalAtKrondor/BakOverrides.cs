namespace BetrayalAtKrondor;

using Spice86.Core.Emulator.Function;
using Spice86.Core.Emulator.ReverseEngineer;
using Spice86.Core.Emulator.VM;
using Spice86.Logging;
using Spice86.Shared.Emulator.Memory;

public class BakOverrides : CSharpOverrideHelper {
    private readonly IGameEngine _gameEngine;
    private readonly GlobalSettings _globalSettings;

    public BakOverrides(Dictionary<SegmentedAddress, FunctionInformation> functionsInformation, Machine machine, ushort entrySegment = 0x1000)
        : base(functionsInformation, machine, new LoggerService(new LoggerPropertyBag())) {
        _globalSettings = new GlobalSettings(machine.Memory);
        _gameEngine = new GameEngine(machine.MouseDriver);
        _gameEngine.DataPath = machine.Dos.FileManager.ToHostCaseSensitiveFileName(string.Empty, false) ?? Directory.GetCurrentDirectory();
        DefineFunction(0x3849, 0x0020, LoadConfig, true, nameof(LoadConfig));
        DefineFunction(0x1834, 0x48EC, SetMouseCursorRange, true, nameof(SetMouseCursorRange));
    }

    private Action LoadConfig(int _) {
        string resourceConfigFilePath = _gameEngine.DataPath + "resource.cfg";
        if (File.Exists(resourceConfigFilePath)) {
            LoadResourceConfig(resourceConfigFilePath);
        }
        string driveConfigFilePath = _gameEngine.DataPath + "drive.cfg";
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