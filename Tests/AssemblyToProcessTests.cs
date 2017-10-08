namespace Tests
{
    using System;
    using System.ComponentModel.Composition.Hosting;

    using JetBrains.Annotations;

    using NUnit.Framework;

    public class AssemblyToProcessTests
    {
        [NotNull]
        private readonly WeaverHelper _weaverHelper = WeaverHelper.Create();

        [Test]
        public void ExternalTest()
        {
            var catalog = new AggregateCatalog();
            var container = new CompositionContainer(catalog);

            catalog.Catalogs.Add(new AssemblyCatalog(_weaverHelper.Assembly));

            var actions = container.GetExportedValues<Action>();

            foreach (var action in actions)
            {
                var method = action.Method;

                TestContext.Out.WriteLine($"Run {method.DeclaringType.Name}.{method.Name}");
                action();
            }
        }
    }
}
