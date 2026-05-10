using GameData.Resources.Dialog;
using GameData.Resources.World;

namespace ResourceExtraction;

using GameData.Resources;
using GameData.Resources.Animation;
using GameData.Resources.Audio;
using GameData.Resources.Book;
using GameData.Resources.Data;
using GameData.Resources.Image;
using GameData.Resources.Label;
using GameData.Resources.Menu;
using GameData.Resources.Monster;
using GameData.Resources.Palette;
using GameData.Resources.Spells;
using Extractors;
using Extractors.Animation;
using Extractors.Audio;
using Extractors.Dialog;
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
            typeof(KeywordList), typeof(KeywordExtractor)
        }, {
            typeof(SaveGame), typeof(SaveGameExtractor)
        }, {
            typeof(LabelSet), typeof(LabelExtractor)
        }, {
            typeof(BackgroundImage), typeof(ScreenExtractor)
        }, {
            typeof(SpellList), typeof(SpellExtractor)
        }, {
            typeof(SpellInfoList), typeof(SpellInfoExtractor)
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