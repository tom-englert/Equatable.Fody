using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using JetBrains.Annotations;

using Xunit;

using Tests;

using Xunit.Abstractions;

public class WeaverTests
{
    [NotNull]
    private readonly WeaverHelper _weaverHelper = WeaverHelper.Create();
    [NotNull]
    private readonly ITestOutputHelper _testOutputHelper;

    public WeaverTests([NotNull] ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void OutputWeaverErrors()
    {
        foreach (var message in _weaverHelper.Errors)
        {
            _testOutputHelper.WriteLine(message);
        }

        Assert.Equal(6, _weaverHelper.Errors.Count());
    }

    [Fact]
    public void OutputWeaverMessages()
    {
        foreach (var message in _weaverHelper.Messages)
        {
            _testOutputHelper.WriteLine(message);
        }
    }
}