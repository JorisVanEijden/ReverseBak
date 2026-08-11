namespace BetrayalAtKrondor.Overrides.Libraries;

public abstract class Constants {
    public const ushort O_RDONLY = 1;
    public const ushort O_WRONLY = 2;
    public const ushort O_RDWR = 4;
    public const ushort O_CREAT = 0x0100;
    public const ushort O_TRUNC = 0x0200;
    public const ushort O_EXCL = 0x0400;
    public const ushort _O_RUNFLAGS = 0x0700;
    public const ushort _O_EOF = 0x0200;
    public const ushort O_APPEND = 0x0800;
    public const ushort O_CHANGED = 0x1000;
    public const ushort O_DEVICE = 0x2000;
    public const ushort O_TEXT = 0x4000;
    public const ushort O_BINARY = 0x8000;
    public const ushort O_NOINHERIT = 0x80;
    public const ushort O_DENYALL = 0x10;
    public const ushort O_DENYWRITE = 0x20;
    public const ushort O_DENYREAD = 0x30;
    public const ushort O_DENYNONE = 0x40;

    public const ushort _F_READ = 0x0001;
    public const ushort _F_WRIT = 0x0002;
    public const ushort _F_RDWR = 0x0003;
    public const ushort _F_BUF = 0x0004;
    public const ushort _F_LBUF = 0x0008;
    public const ushort _F_ERR = 0x0010;
    public const ushort _F_EOF = 0x0020;
    public const ushort _F_BIN = 0x0040;
    public const ushort _F_IN = 0x0080;
    public const ushort _F_OUT = 0x0100;
    public const ushort _F_TERM = 0x0200;

    public const ushort S_IREAD = 0x0100;
    public const ushort S_IWRITE = 0x0080;

    public const int EOF = -1;
}

[Flags]
public enum OpenFlags {
    O_RDONLY = 1,
    O_WRONLY = 2,
    O_RDWR = 4,
    O_DENYALL = 0x10,
    O_DENYWRITE = 0x20,
    O_DENYREAD = 0x30,
    O_DENYNONE = 0x40,
    O_NOINHERIT = 0x80,
    O_CREAT = 0x0100,
    O_TRUNC = 0x0200,
    O_EXCL = 0x0400,
    O_APPEND = 0x0800,
    O_CHANGED = 0x1000,
    O_DEVICE = 0x2000,
    O_TEXT = 0x4000,
    O_BINARY = 0x8000
}

[Flags]
public enum FileFlags {
    _F_READ = 0x0001,
    _F_WRIT = 0x0002,
    _F_RDWR = 0x0003,
    _F_BUF = 0x0004,
    _F_LBUF = 0x0008,
    _F_ERR = 0x0010,
    _F_EOF = 0x0020,
    _F_BIN = 0x0040,
    _F_IN = 0x0080,
    _F_OUT = 0x0100,
    _F_TERM = 0x0200
}

[Flags]
public enum StatFlags {
    S_IWRITE = 0x0080,
    S_IREAD = 0x0100
}

[Flags]
public enum FileAttrib {
    FA_NORMAL = 0x00,
    FA_RDONLY = 0x01,
    FA_HIDDEN = 0x02,
    FA_SYSTEM = 0x04,
    FA_LABEL = 0x08,
    FA_DIREC = 0x10,
    FA_ARCH = 0x20
}