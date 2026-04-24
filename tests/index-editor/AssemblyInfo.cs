using System.Runtime.CompilerServices;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace IndexEditor.Tests
{
    // Module initializer to set up DI before any tests run
    internal static class TestInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            // Initialize DI for all tests
            TestDIHelper.EnsureInitialized();
        }
    }
}
