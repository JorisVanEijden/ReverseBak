namespace BetrayalAtKrondor;

using System.Reflection;

using BetrayalAtKrondor.Mcp;

using Spice86.Core.CLI;
using Spice86.Core.Emulator.Function;
using Spice86.Core.Emulator.VM;
using Spice86.Shared.Emulator.Memory;
using Spice86.Shared.Interfaces;

/// <summary>
///     Provides functions overrides for the DOS program.
/// </summary>
public class BakOverrideSupplier : IOverrideSupplier, IMcpToolSupplier {
    // Created during GenerateFunctionInformations, registered in DI via GetMcpServices
    private OverlayAddressTranslator? _translator;

    public IDictionary<SegmentedAddress, FunctionInformation> GenerateFunctionInformations(ILoggerService loggerService, Configuration configuration, ushort programStartAddress, Machine machine) {
        Dictionary<SegmentedAddress, FunctionInformation> functionsInformation = new();

        _translator = new OverlayAddressTranslator();
        var bakOverrides = new BakOverrides(functionsInformation, machine, loggerService, configuration, _translator);

        return functionsInformation;
    }

    public IEnumerable<Assembly> GetMcpToolAssemblies() {
        yield return typeof(BakMcpTools).Assembly;
    }

    public IEnumerable<object> GetMcpServices() {
        if (_translator != null) {
            yield return _translator;
        }
    }
}
