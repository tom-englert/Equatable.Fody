namespace NetStandardSmokeTest
{
    using System;

    using Equatable;

    [ImplementsEquatable]
    public class Class1
    {
        [Equals(StringComparison.CurrentCulture)]
        private string x;
    }
}
