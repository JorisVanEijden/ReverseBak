namespace BetrayalAtKrondor;

using Spice86.Core.Emulator.Function;
using Spice86.Core.Emulator.VM;
using Spice86.Shared.Emulator.Memory;

/// <summary>
///     Provides functions overrides for the DOS program.
/// </summary>
public class BakOverrideSupplier : IOverrideSupplier {
    public Dictionary<SegmentedAddress, FunctionInformation> GenerateFunctionInformations(int programStartAddress, Machine machine) {
        Dictionary<SegmentedAddress, FunctionInformation> functionsInformation = new();
        // You can extend / replace GeneratedOverrides with your own overrides as well.
        var bakOverrides = new BakOverrides(functionsInformation, machine);

        return functionsInformation;
    }
}