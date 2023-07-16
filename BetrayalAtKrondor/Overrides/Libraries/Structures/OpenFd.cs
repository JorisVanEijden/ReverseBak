namespace BetrayalAtKrondor.Overrides.TurboC.Structures;

using Spice86.Core.Emulator.Memory;
using Spice86.Core.Emulator.ReverseEngineer;

/// <summary>
/// Handler of access modes for currently open file/devices
/// </summary>
public class OpenFd : MemoryBasedDataStructureWithBaseAddress {
    public OpenFd(Memory memory, uint baseAddress) : base(memory, baseAddress) {
    }

    public void UnsetFlag(int fileHandle, int flag) {
        this[fileHandle] = (ushort)(this[fileHandle] & ~flag);
    }

    public ushort this[int i] {
        get => GetUint16(i * 2);
        set => SetUint16(i * 2, value);
    }
}