namespace BetrayalAtKrondor;

using Spice86.Core.Emulator.Memory;

internal class GlobalSettings : IGlobalSettings {
    private readonly Memory _memory;

    public GlobalSettings(Memory memory) {
        _memory = memory;
        Initialize();
    }

    public SoundDriverType SoundDriverType {
        get => (SoundDriverType)_memory.UInt16[0x39DD, 0x043C];
        set => _memory.UInt16[0x39DD, 0x043C] = (ushort)value;
    }

    public bool KnockKnock {
        get => _memory.UInt16[0x39DD, 0x08CF] != 0;
        set => _memory.UInt16[0x39DD, 0x08CF] = value ? (ushort)1 : (ushort)0;
    }

    public int Cycles {
        get => _memory.UInt16[0x39DD, 0x08D1];
        set => _memory.UInt16[0x39DD, 0x08D1] = (ushort)value;
    }

    public char TempDrive {
        get => (char)_memory.UInt16[0x39DD, 0x08D3];
        set => _memory.UInt16[0x39DD, 0x08D3] = value;
    }

    public bool BookmarkVerify {
        get => _memory.UInt16[0x39DD, 0x08D5] != 0;
        set => _memory.UInt16[0x39DD, 0x08D5] = value ? (ushort)1 : (ushort)0;
    }

    public bool NonRotatingMap {
        get => _memory.UInt16[0x39DD, 0x08D7] != 0;
        set => _memory.UInt16[0x39DD, 0x08D7] = value ? (ushort)1 : (ushort)0;
    }

    public char DriveLetter {
        get => (char)_memory.UInt8[0x39DD, 0x090B];
        set => _memory.UInt8[0x39DD, 0x090B] = (byte)value;
    }

    public char CdRomDriveLetter {
        get => (char)_memory.UInt8[0x39DD, 0x08CE];
        set => _memory.UInt8[0x39DD, 0x08CE] = (byte)value;
    }

    private void Initialize() {
        SoundDriverType = SoundDriverType.None;
        BookmarkVerify = true;
    }
}