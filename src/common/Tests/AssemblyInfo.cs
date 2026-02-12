using System.Runtime.CompilerServices;

namespace Common.Tests
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

