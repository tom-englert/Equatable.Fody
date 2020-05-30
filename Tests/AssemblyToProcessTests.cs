namespace Tests
{
    using System;
    using System.ComponentModel.Composition.Hosting;

    using Xunit;
    using Xunit.Abstractions;

    public class AssemblyToProcessTests
    {
        private readonly WeaverHelper _weaverHelper = WeaverHelper.Create();
        private readonly ITestOutputHelper _testOutputHelper;


        public AssemblyToProcessTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ExternalTest()
        {
            var catalog = new AggregateCatalog();
            var container = new CompositionContainer(catalog);

            catalog.Catalogs.Add(new AssemblyCatalog(_weaverHelper.Assembly));

            var actions = container.GetExportedValues<Action>();

            foreach (var action in actions)
            {
                var method = action.Method;

                _testOutputHelper.WriteLine($"Run {method.DeclaringType.Name}.{method.Name}");
                action();
            }
        }
    }
}
