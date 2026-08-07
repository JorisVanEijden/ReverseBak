namespace BetrayalAtKrondor.Tests.Text;

using Xunit;

/// <summary>
/// Serialises every test class that reads or replaces the ambient <c>UiStrings.Catalog</c>.
///
/// <para><c>UiStrings.Catalog</c> is process-wide mutable static state — that was the deliberate
/// trade the design took (§8) to keep <c>MoneyFormatter</c> and <c>DialogSlotPopulator</c> pure and
/// terse. xUnit runs different test COLLECTIONS in parallel by default, so
/// <c>UiStringsTests.AmbientCatalogIsReplaceable</c> — which swaps the catalog for
/// <c>{"k":"v"}</c> and restores it in a finally — could be in flight while a money or dialog test
/// in another class formats a string, making that test read the two-entry stub and fail. The
/// failure would be timing-dependent and would look like a bug in the code under test.</para>
///
/// <para>Membership rule: any test class whose assertions depend on the ambient catalog's contents,
/// or that assigns to it, joins this collection. Classes that only touch
/// <c>UiStringCatalog.Embedded</c> or a locally-constructed catalog do not need to.</para>
/// </summary>
[CollectionDefinition(Name)]
public sealed class UiStringsCollection {
    public const string Name = "UiStrings";
}
