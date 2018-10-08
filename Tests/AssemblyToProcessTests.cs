namespace Tests
{
    using System;
    using System.ComponentModel.Composition.Hosting;

    using JetBrains.Annotations;

    using Xunit;
    using Xunit.Abstractions;

    public class AssemblyToProcessTests
    {
        [NotNull]
        private readonly WeaverHelper _weaverHelper = WeaverHelper.Create();
        [NotNull]
        private readonly ITestOutputHelper _testOutputHelper;


        public AssemblyToProcessTests([NotNull] ITestOutputHelper testOutputHelper)
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
