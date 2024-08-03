namespace ResourceExtraction;

using GameData.Resources;
using GameData.Resources.Animation;
using GameData.Resources.Book;
using GameData.Resources.Data;
using GameData.Resources.Image;
using GameData.Resources.Label;
using GameData.Resources.Menu;
using GameData.Resources.Spells;
using ResourceExtraction.Extractors;
using ResourceExtraction.Extractors.Animation;
using System;
using System.Collections.Generic;

public static class ExtractorFactory {
    public static readonly Dictionary<Type, Type> ExtractorMap = new() {
        {
            typeof(AnimationResource), typeof(AnimationExtractor)
        }, {
            typeof(AnimatorScene), typeof(TtmExtractor)
        }, {
            typeof(UserInterface), typeof(UserInterfaceExtractor)
        }, {
            typeof(ImageSet), typeof(BitmapExtractor)
        }, {
            typeof(BookResource), typeof(BokExtractor)
        }, {
            typeof(KeywordList), typeof(KeywordExtractor)
        }, {
            typeof(LabelSet), typeof(LabelExtractor)
        }, {
            typeof(Screen), typeof(ScreenExtractor)
        }, {
            typeof(SpellList), typeof(SpellExtractor)
        }, {
            typeof(SpellInfoList), typeof(SpellInfoExtractor)
        }
    };

    public static ExtractorBase<T> GetExtractor<T>() where T : IResource {
        if (ExtractorMap.TryGetValue(typeof(T), out var extractorType)) {
            return (ExtractorBase<T>)Activator.CreateInstance(extractorType);
        }

        throw new InvalidOperationException($"No extractor found for type {typeof(T).Name}");
    }
}