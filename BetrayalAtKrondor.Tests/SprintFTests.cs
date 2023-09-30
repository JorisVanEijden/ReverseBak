namespace BetrayalAtKrondor.Tests;

using Avalonia.Input;

using BetrayalAtKrondor.Overrides.Libraries;

using Moq;

using Spice86.Core.CLI;
using Spice86.Core.Emulator;
using Spice86.Core.Emulator.CPU;
using Spice86.Core.Emulator.ReverseEngineer.DataStructure.Array;
using Spice86.Core.Emulator.VM;
using Spice86.Logging;
using Spice86.Shared.Interfaces;

using System.Diagnostics;

using Xunit;
using Xunit.Abstractions;

public class SprintFTests {
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly CFunctions _cFunctions;
    private readonly Cpu _cpu;

    public SprintFTests(ITestOutputHelper testOutputHelper) {
        _testOutputHelper = testOutputHelper;
        var configuration = new Configuration {
            InitializeDOS = false,
            Exe = "BetrayalAtKrondor.Tests.dll",
        };

        ILoggerService loggerService = new Mock<LoggerService>(new LoggerPropertyBag()).Object;
        var programExecutor = new ProgramExecutor(configuration, loggerService, null);
        Machine machine = programExecutor.Machine;
        _cpu = machine.Cpu;
        _cFunctions = new CFunctions(_cpu, machine.Memory);
    }

    [Fact]
    public void SprintfNegativeInt16() {
        //Arrange
        _cpu.Stack.Push16(0xFFFB);
        _cpu.Stack.Push32(0x89ABCDEF); // dummy vars
        _cpu.Stack.Push32(0x01234567); // dummy return address

        // Act
        string result = _cFunctions._sprintf("%d");

        // Assert
        Assert.Equal("-5", result);
    }

    [Fact]
    public void TestPerf() {
        var sw = new Stopwatch();
        const int iterations = int.MaxValue;

        var fastest = new Tester();
        ISlowThingsDown slow = fastest;
        var fast = slow as MyAbstractBaseClass;

        sw.Start();
        for (int i = 0; i < iterations; i++) {
            fastest.Property = i;
        }
        Console.WriteLine(sw.ElapsedMilliseconds.ToString());
        sw.Restart();
        for (int i = 0; i < iterations; i++) {
            fast!.Property = i;
        }
        Console.WriteLine(sw.ElapsedMilliseconds.ToString());
        sw.Restart();
        for (int i = 0; i < iterations; i++) {
            slow.Property = i;
        }
        Console.WriteLine(sw.ElapsedMilliseconds.ToString());
        sw.Stop();
    }
}

public interface IAmBaseInterface {
    public long Property { set; }
}
public interface ISlowThingsDown : IAmBaseInterface {
}

public class Tester : MyAbstractBaseClass, ISlowThingsDown {
}

public abstract class MyAbstractBaseClass : IAmBaseInterface {
    public long Property { get; set; }
}