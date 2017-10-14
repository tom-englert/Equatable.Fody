namespace Equatable
{
    using System;

    /// <summary>
    /// Apply this attribute to a property or field that must be tested for equality.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class EqualsAttribute : Attribute
    {
        public EqualsAttribute(StringComparison stringComparison = StringComparison.Ordinal)
        {
        }

        private bool Sequence { get; set; }
    }

    /// <summary>
    /// Apply this attribute to a method used to provide custom compare functionality.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CustomEqualsAttribute : Attribute
    {
    }

    /// <summary>
    /// Apply this attribute to a method used to provide custom get hash code functionality.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CustomGetHashCodeAttribute : Attribute
    {
    }
}
