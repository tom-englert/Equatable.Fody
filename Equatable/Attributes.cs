namespace Equatable
{
    using System;

    /// <summary>
    /// Apply this attribute to a property or field that must be equal for two objects.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class Equals : Attribute
    {
        public Equals(StringComparison stringComparison = StringComparison.Ordinal)
        {
        }

        private bool Sequence { get; set; }
    }
}
