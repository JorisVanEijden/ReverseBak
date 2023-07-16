namespace BetrayalAtKrondor;

internal interface IGlobalSettings {
    SoundDriverType SoundDriverType { get; set; }
    bool KnockKnock { get; set; }
    int Cycles { get; set; }
    char TempDrive { get; set; }
    bool BookmarkVerify { get; set; }
    bool NonRotatingMap { get; set; }
    char DriveLetter { get; set; }
    char CdRomDriveLetter { get; set; }
}