using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using JetBrains.Annotations;

using NUnit.Framework;

using Tests;

[TestFixture]
public class WeaverTests
{
    [NotNull]
    private readonly WeaverHelper _weaverHelper = WeaverHelper.Create();

    [Test]
    public void OutputWeaverErrors()
    {
        foreach (var message in _weaverHelper.Errors)
        {
            TestContext.Out.WriteLine(message);
        }

        Assert.AreEqual(0, _weaverHelper.Errors.Count);
    }

    [Test]
    public void OutputWeaverMessages()
    {
        foreach (var message in _weaverHelper.Messages)
        {
            TestContext.Out.WriteLine(message);
        }
    }

#if (DEBUG)
    [Test]
    public void PeVerify()
    {
        Verifier.Verify(_weaverHelper.OriginalAssemblyPath, _weaverHelper.NewAssemblyPath);
    }
#endif
}