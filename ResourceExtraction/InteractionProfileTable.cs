namespace ResourceExtraction;

using GameData.Resources.Data;
using GameData.Resources.World;
using System.Collections.Generic;

/// <summary>
/// RE'd map from an entity's interactable-type byte (TableDatInfo.EntityType, DOS
/// HandleEnvironmentInteraction @0x76573 switch) to its semantic behavior key + data-driven
/// <see cref="InteractionProfile"/>. This is code knowledge (baked in the EXE switch), surfaced
/// as engine-independent data. This slice ships only the corpse entry (byte 16); adding a type
/// later is a new row here.
/// </summary>
public static class InteractionProfileTable {
    private static readonly Dictionary<WorldEntityType, (string Behavior, InteractionProfile Profile)> Map = new() {
        // byte 16 = corpse (handle_Corpse @0x76a0a).
        [WorldEntityType.Corpse] = ("container", new InteractionProfile {
            Range = new InteractionRange(7000, 2500),
            ActionableContainerTypes = new[] { SaveGameContainerType.Corpse, SaveGameContainerType.ScriptedLoot },
            ExamineDialogId = 94,
            ActionDialogId = 78,
            NotActionableDialogId = 154,
            OpensLoot = true,
            HasLock = false,
        }),
    };

    public static bool TryGet(WorldEntityType entityType, out string behavior, out InteractionProfile profile) {
        if (Map.TryGetValue(entityType, out var e)) {
            behavior = e.Behavior;
            profile = e.Profile;
            return true;
        }
        behavior = null!;
        profile = null!;
        return false;
    }
}
