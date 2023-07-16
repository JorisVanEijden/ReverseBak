namespace BetrayalAtKrondor;

using Spice86.Core.Emulator.Function;
using Spice86.Core.Emulator.VM;
using Spice86.Logging;
using Spice86.Shared.Emulator.Memory;
using Spice86.Shared.Interfaces;

/// <summary>
///     Provides functions overrides for the DOS program.
/// </summary>
public class BakOverrideSupplier : IOverrideSupplier {

    public Dictionary<SegmentedAddress, FunctionInformation> GenerateFunctionInformations(int programStartAddress, Machine machine, ILoggerService loggerService) {
        Dictionary<SegmentedAddress, FunctionInformation> functionsInformation = new();
        // You can extend / replace GeneratedOverrides with your own overrides as well.
        
        var bakOverrides = new BakOverrides(functionsInformation, machine, loggerService);

        return functionsInformation;
    }
}