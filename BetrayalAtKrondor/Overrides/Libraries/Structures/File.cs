namespace BetrayalAtKrondor.Overrides.TurboC.Structures;

public struct File {
    public short level;
    public ushort flags;
    public char fd;
    public byte hold;
    public short bsize;
    public byte[] buffer;
    public short curp; // current index into buffer
    public ushort istemp;
    public short token;
}