namespace GameData.Resources.Config;

using System;

/// <summary>
/// Engine-independent player preferences, mirroring the settings the original
/// game keeps in its 5-byte KRONDOR.CFG (and DEFAULT.DAT / CDEFAULT.DAT).
/// Values are expressed as domain enums and booleans; the byte layout and the
/// flag-bit packing are an extractor concern (see PreferencesExtractor) and do
/// not leak into this model.
/// </summary>
[Serializable]
public class Preferences : IResource {
    public Preferences() {
        Id = string.Empty;
    }

    public Preferences(string id) {
        Id = id;
    }

    // Movement
    public StepSize StepSize { get; set; } = StepSize.Medium;
    public TurnSize TurnSize { get; set; } = TurnSize.Medium;

    // Display
    public DetailLevel DetailLevel { get; set; } = DetailLevel.High;
    public TextSpeed TextSpeed { get; set; } = TextSpeed.Slow;

    // Audio / intro toggles
    public bool Sound { get; set; } = true;
    public bool GameMusic { get; set; } = true;
    public bool CombatMusic { get; set; } = true;
    public bool Introduction { get; set; } = true;
    public bool CdMusic { get; set; }

    public ResourceType Type => ResourceType.DAT;

    public string Id { get; set; }

    public Preferences Clone() {
        return new Preferences(Id) {
            StepSize = StepSize,
            TurnSize = TurnSize,
            DetailLevel = DetailLevel,
            TextSpeed = TextSpeed,
            Sound = Sound,
            GameMusic = GameMusic,
            CombatMusic = CombatMusic,
            Introduction = Introduction,
            CdMusic = CdMusic
        };
    }
}

/// <summary>Forward-movement step distance (config offset 0, 3 options).</summary>
public enum StepSize {
    Small = 0,
    Medium = 1,
    Large = 2
}

/// <summary>Turn increment (config offset 1, 3 options).</summary>
public enum TurnSize {
    Small = 0,
    Medium = 1,
    Large = 2
}

/// <summary>World rendering level of detail (config offset 2, 4 options).</summary>
public enum DetailLevel {
    Minimum = 0,
    Low = 1,
    High = 2,
    Maximum = 3
}

/// <summary>Dialogue text auto-advance speed (config offset 3, 3 options).</summary>
public enum TextSpeed {
    Slow = 0,
    Medium = 1,
    Fast = 2
}
