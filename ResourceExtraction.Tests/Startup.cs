using System.Runtime.CompilerServices;
using System.Text;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = false)]

namespace ResourceExtraction.Tests;

public class Startup
{
    [ModuleInitializer]
    internal static void Init()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
