namespace ResourceExtraction.Tests;

using ResourceExtraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

/// <summary>
/// A written extractor is not a reachable one.
///
/// <para>This exists because the same omission has now bitten twice: <c>DefCombExtractor</c> and
/// <c>DefTrapExtractor</c> were complete but absent from <see cref="ExtractorFactory.ExtractorMap"/>,
/// so every ambush in the game silently never fired; and <c>GdsSceneExtractor</c> was likewise
/// unreachable, so all 118 location scenes resolved to null. Both looked finished. The Unity side's
/// <c>LoadOrNull</c> swallows the "resource not found" into a null, which is what makes the failure
/// silent rather than loud.</para>
///
/// <para>So the invariant is checked mechanically rather than by remembering: every concrete
/// <c>ExtractorBase&lt;T&gt;</c> in the assembly must be registered for its own T.</para>
/// </summary>
public class ExtractorFactoryCoverageTests {
    /// <summary>
    /// Extractors that are deliberately not in the map, each with the reason. Adding a name here is
    /// a decision to be justified, not a way to silence the test.
    /// </summary>
    private static readonly Dictionary<string, string> DeliberatelyUnregistered = new() {
        // The shipped game never opens these two: they are authoring/build-time sources with no
        // Load_* xref in the executable in any casing. The runtime party comes from STARTUP.GAM and
        // the runtime object table from OBJINFO.DAT. Archive membership is not use.
        ["PartyExtractor"] = "PARTY.DAT is an authoring-time source, not loaded by the game",
        ["OnamesExtractor"] = "ONAMES.DAT is an authoring-time source, not loaded by the game",
        // CursorManager sizes the pointer from the loaded sprite itself and deliberately never
        // reads the POINTER metadata as a resource.
        ["CursorExtractor"] = "CursorManager needs no CursorSet load; it uses the sprite directly",

    };

    private static IEnumerable<(Type Extractor, Type Resource)> ConcreteExtractors() {
        foreach (Type type in typeof(ExtractorFactory).Assembly.GetTypes()) {
            if (type.IsAbstract || type.IsGenericTypeDefinition || !type.IsClass) {
                continue;
            }
            for (Type t = type.BaseType; t != null; t = t.BaseType) {
                if (t.IsGenericType && t.GetGenericTypeDefinition().Name.StartsWith("ExtractorBase")) {
                    yield return (type, t.GetGenericArguments()[0]);
                    break;
                }
            }
        }
    }

    [Fact]
    public void EveryExtractorIsReachableThroughTheFactory() {
        var missing = new List<string>();
        foreach ((Type extractor, Type resource) in ConcreteExtractors()) {
            if (DeliberatelyUnregistered.ContainsKey(extractor.Name)) {
                continue;
            }
            if (!ExtractorFactory.ExtractorMap.TryGetValue(resource, out Type registered)) {
                missing.Add($"{extractor.Name} produces {resource.Name}, which nothing maps to");
            } else if (registered != extractor) {
                missing.Add($"{resource.Name} maps to {registered.Name}, not {extractor.Name}");
            }
        }

        Assert.True(missing.Count == 0,
            "These extractors cannot be loaded at runtime — Unity's LoadOrNull will turn every "
            + "request into a silent null:\n  " + string.Join("\n  ", missing));
    }

    [Fact]
    public void TheGdsSceneExtractorIsRegistered() {
        // The specific regression this test file was written for: 118 location scenes, extracted
        // and committed, that nothing could load.
        Assert.True(ExtractorFactory.ExtractorMap.ContainsKey(
            typeof(GameData.Resources.Scene.GdsScene)));
    }

    [Fact]
    public void NothingIsMappedToAnExtractorThatDoesNotProduceIt() {
        foreach (KeyValuePair<Type, Type> pair in ExtractorFactory.ExtractorMap) {
            Type produced = null;
            for (Type t = pair.Value.BaseType; t != null; t = t.BaseType) {
                if (t.IsGenericType && t.GetGenericTypeDefinition().Name.StartsWith("ExtractorBase")) {
                    produced = t.GetGenericArguments()[0];
                    break;
                }
            }
            Assert.True(produced == null || produced == pair.Key,
                $"{pair.Value.Name} is mapped to {pair.Key.Name} but produces {produced?.Name}");
        }
    }
}
