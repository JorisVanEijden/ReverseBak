using GameData.Resources.Dialog;
using GameData.Resources.World;

namespace ResourceExtraction;

using GameData.Resources;
using GameData.Resources.Animation;
using GameData.Resources.Audio;
using GameData.Resources.Book;
using GameData.Resources.Config;
using GameData.Resources.Creature;
using GameData.Resources.Credits;
using GameData.Resources.Data;
using GameData.Resources.Combat;
using GameData.Resources.Location;
using GameData.Resources.Scene;
using GameData.Resources.Image;
using GameData.Resources.Font;
using GameData.Resources.Label;
using GameData.Resources.Menu;
using GameData.Resources.Monster;
using GameData.Resources.Object;
using GameData.Resources.Palette;
using GameData.Resources.Spells;
using Extractors;
using Extractors.Animation;
using Extractors.Audio;
using Extractors.Dialog;
using Extractors.Object;
using System;
using System.Collections.Generic;

public static class ExtractorFactory {
    public static readonly Dictionary<Type, Type> ExtractorMap = new() {
        {
            typeof(AnimatorResource), typeof(AdsExtractor)
        }, {
            typeof(AnimationResource), typeof(TtmExtractor)
        }, {
            typeof(UserInterface), typeof(UserInterfaceExtractor)
        }, {
            typeof(ImageSet), typeof(BitmapExtractor)
        }, {
            typeof(BookResource), typeof(BokExtractor)
        }, {
            typeof(CreditsData), typeof(CredExtractor)
        }, {
            typeof(KeywordList), typeof(KeywordExtractor)
        }, {
            typeof(MovementData), typeof(MovementExtractor)
        }, {
            typeof(StartData), typeof(StartDataExtractor)
        }, {
            typeof(SaveGame), typeof(SaveGameExtractor)
        }, {
            typeof(ChapterStartData), typeof(ChapterDataExtractor)
        }, {
            typeof(GameData.Resources.Combat.PartyCombatEntries), typeof(PartyCombatEntryExtractor)
        }, {
            typeof(FontResource), typeof(FontExtractor)
        }, {
            typeof(LabelSet), typeof(LabelExtractor)
        }, {
            typeof(InputForm), typeof(InExtractor)
        }, {
            typeof(BackgroundImage), typeof(ScreenExtractor)
        }, {
            typeof(SpellList), typeof(SpellExtractor)
        }, {
            typeof(PaletteResource), typeof(PaletteExtractor)
        }, {
            typeof(AudioResource), typeof(AudioExtractor)
        }, {
            typeof(ZoneTable), typeof(ZoneTableExtractor)
        }, {
            typeof(RemapResource), typeof(RemapExtractor)
        }, {
            typeof(MonsterStats), typeof(MonsterStatsExtractor)
        }, {
            typeof(WorldTile), typeof(WorldItemExtractor)
        }, {
            // GDS##?.DAT — the interactive location scenes. The extractor has existed since the GDS
            // work landed but was never reachable through here, so every Unity load of one resolved
            // to null. Same omission the DEF_COMB/DEF_TRAP extractors had.
            typeof(GdsScene), typeof(GdsSceneExtractor)
        }, {
            typeof(EncampData), typeof(EncampExtractor)
        }, {
            typeof(GridData), typeof(GridExtractor)
        }, {
            typeof(TrapData), typeof(TrapExtractor)
        }, {
            typeof(CreatureBitmaps), typeof(BNamesExtractor)
        }, {
            typeof(GameData.Resources.Palette.ColorRemapTable), typeof(ColorRemapTableExtractor)
        }, {
            typeof(CastRing), typeof(CastRingExtractor)
        }, {
            typeof(ChapterSongMap), typeof(ChapterSongMapExtractor)
        }, {
            typeof(SpellAffinityTable), typeof(SpellAffinityExtractor)
        }, {
            typeof(SpellBookPage), typeof(SpellBookPageExtractor)
        }, {
            typeof(SpellDescriptions), typeof(SpellDocExtractor)
        }, {
            typeof(ZoneAppearance), typeof(ZoneAppearanceExtractor)
        }, {
            typeof(ZoneBounds), typeof(ZoneBoundsExtractor)
        }, {
            typeof(ZoneDefinition), typeof(ZoneDefExtractor)
        }, {
            typeof(ZoneMap), typeof(ZoneMapExtractor)
        }, {
            typeof(ZoneRef), typeof(ZoneRefExtractor)
        }, {
            typeof(ZoneShape), typeof(ZoneShapeExtractor)
        }, {
            typeof(Dialog), typeof(DdxExtractor)
        }, {
            typeof(Preferences), typeof(PreferencesExtractor)
        }, {
            typeof(ObjectInfoSet), typeof(ObjectInfoSetExtractor)
        }, {
            typeof(DetectData), typeof(DetectExtractor)
        }, {
            typeof(FilterData), typeof(FilterExtractor)
        }, {
            // The invisible-wall hotspot parameters (docs/specs/collision-system.md §3.4.1).
            typeof(DefFamilyFile<DefBlocEntry>), typeof(Extractors.Def.DefBlocExtractor)
        }, {
            // Combat/trap hotspot parameters: the avoidable bit the scouting roll is gated on, the
            // encounter index behind the already-fought check, and the pre-fire dialog.
            typeof(DefFamilyFile<DefCombEntry>), typeof(Extractors.Def.DefCombExtractor)
        }, {
            typeof(DefFamilyFile<DefTrapEntry>), typeof(Extractors.Def.DefTrapExtractor)
        }, {
            typeof(DefFamilyFile<DefBkgrEntry>), typeof(Extractors.Def.DefBkgrExtractor)
        }, {
            typeof(DefFamilyFile<DefDialEntry>), typeof(Extractors.Def.DefDialExtractor)
        }, {
            typeof(DefFamilyFile<DefDisaEntry>), typeof(Extractors.Def.DefDisaExtractor)
        }, {
            typeof(DefFamilyFile<DefEnabEntry>), typeof(Extractors.Def.DefEnabExtractor)
        }, {
            typeof(DefFamilyFile<DefTownEntry>), typeof(Extractors.Def.DefTownExtractor)
        }, {
            typeof(DefFamilyFile<DefZoneEntry>), typeof(Extractors.Def.DefZoneExtractor)
        }, {
            typeof(FullMapTowns), typeof(FullMapTownExtractor)
        }, {
            typeof(FullMapPositions), typeof(FullMapPositionExtractor)
        }, {
            typeof(SpellSymbolLayout), typeof(SpellSymbolExtractor)
        }, {
            // Needed at RUNTIME, not just for the JSON dump: a dialog Teleport action names a
            // destination by id, so the table must be loadable through the resource system.
            typeof(TeleportDestinationSet), typeof(TeleportExtractor)
        }, {
            // The second source actorspawn_objfixed reads, after the save's own copy.
            typeof(FixedObjectSet), typeof(ObjFixedExtractor)
        }, {
            typeof(TileEventTile), typeof(TileEventExtractor)
        }, {
            typeof(CreatureNames), typeof(MnamesExtractor)
        }, {
            // Read out of KRONDOR.EXE. Absent until 2026-08-29, so every fight ran with null
            // affinity tables and MonsterTurnResolver never got the shipped flee thresholds.
            typeof(GameData.Resources.Combat.CombatAffinityTables),
            typeof(Extractors.Exe.CombatAffinityExtractor)
        }
    };

    public static ExtractorBase<T> GetExtractor<T>() where T : IResource {
        if (ExtractorMap.TryGetValue(typeof(T), out var extractorType)) {
            return (ExtractorBase<T>)Activator.CreateInstance(extractorType);
        }

        throw new InvalidOperationException($"No extractor found for type {typeof(T).Name}");
    }

    public static object GetExtractor(Type extractorType) {
        return Activator.CreateInstance(extractorType);
    }
}