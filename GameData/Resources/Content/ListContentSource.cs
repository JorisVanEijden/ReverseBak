namespace GameData.Resources.Content;

using System.Collections.Generic;

/// <summary>A concrete <see cref="IContentSource{T}"/> backed by an already-materialized list of
/// entries. The Unity bindings (folder / shipped-JSON mod partials) load async, then wrap the
/// loaded entries in one of these to hand to the pure merge rule.</summary>
/// <remarks>
/// <b>AWAITING ITS FEATURE (TASK-259).</b> The additive content registry has no provider
/// sources wired on the Unity side, so nothing ever adds a source and the one implementation
/// of <c>IContentSource</c> is never constructed.
///
/// <para>Read by <c>scripts/audit-unconsumed-models.py</c>: it separates a rule ported ahead
/// of its feature from an orphan nobody owns, so the audit stays a signal instead of a list
/// to re-triage every run.</para>
/// </remarks>
public sealed class ListContentSource<T> : IContentSource<T> {
    public ListContentSource(string sourceName, IReadOnlyList<ContentEntry<T>> entries) {
        SourceName = sourceName;
        Entries = entries;
    }

    public string SourceName { get; }
    public IReadOnlyList<ContentEntry<T>> Entries { get; }
}
