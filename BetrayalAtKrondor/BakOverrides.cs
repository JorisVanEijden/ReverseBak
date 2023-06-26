namespace BetrayalAtKrondor;

using Spice86.Core.Emulator.Function;
using Spice86.Core.Emulator.ReverseEngineer;
using Spice86.Core.Emulator.VM;
using Spice86.Logging;
using Spice86.Shared.Emulator.Memory;

public class BakOverrides : CSharpOverrideHelper {
    private readonly IGameEngine _gameEngine;

    public BakOverrides(Dictionary<SegmentedAddress, FunctionInformation> functionsInformation, Machine machine, ushort entrySegment = 0x1000)
        : base(functionsInformation, machine, new LoggerService(new LoggerPropertyBag())) {
        _gameEngine = new GameEngine(machine.MouseDriver);
        DefineFunction(0x1834, 0x48EC, SetMouseCursorRange, failOnExisting: true, nameof(SetMouseCursorRange));
    }

    private Action SetMouseCursorRange(int jumpAddress) {
        int minCol = Stack.Peek16(4) * 4;
        int minRow = Stack.Peek16(6) * 4;
        int maxCol = Stack.Peek16(8) * 4;
        int maxRow = Stack.Peek16(10) * 4;

        _gameEngine.SetMouseCursorArea(minCol, minRow, maxCol, maxRow);

        return FarRet();
    }
}