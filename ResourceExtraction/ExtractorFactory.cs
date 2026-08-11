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
using GameData.Resources.Image;
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
            typeof(SaveGame), typeof(SaveGameExtractor)
        }, {
            typeof(ChapterStartData), typeof(ChapterDataExtractor)
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
            typeof(TileEventTile), typeof(TileEventExtractor)
        }, {
            typeof(CreatureNames), typeof(MnamesExtractor)
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