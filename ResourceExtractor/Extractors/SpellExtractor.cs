namespace ResourceExtractor.Extractors;

using ResourceExtractor.Resources.Spells;

internal class SpellExtractor : ExtractorBase {
    public List<Spell> ExtractSpells(string filePath) {
        var spellList = ExtractSpellsFile(Path.Join(filePath, "SPELLS.DAT"));
        
        
        return spellList;
    }

    private List<Spell> ExtractSpellsFile(string join) {
        throw new NotImplementedException();
    }
}