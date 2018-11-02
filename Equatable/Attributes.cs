namespace Equatable
{
    using System;

    /// <summary>
    /// Apply this attribute to a class that should have <see cref="IEquatable{T}"/> auto-implemented. You need to additionally mark the members that make up equality for this type with the <see cref="EqualsAttribute"/>, <see cref="CustomEqualsAttribute"/> and/or <see cref="CustomGetHashCodeAttribute"/>.
    /// See <see href="https://github.com/tom-englert/Equatable.Fody"/> for details.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class ImplementsEquatableAttribute : Attribute
    {
    }

    /// <summary>
    /// Apply this attribute to all properties or fields that should be tested for equality when auto-implementing <see cref="IEquatable{T}"/> for the declaring type.
    /// See <see href="https://github.com/tom-englert/Equatable.Fody"/> for details.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class EqualsAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EqualsAttribute"/> class.
        /// </summary>
        /// <param name="stringComparison">The string comparison.</param>
        // ReSharper disable once UnusedParameter.Local
        public EqualsAttribute(StringComparison stringComparison = StringComparison.Ordinal)
        {
        }
    }

    /// <summary>
    /// Apply this attribute to a method used to provide custom compare functionality when auto-implementing <see cref="IEquatable{T}"/> for the declaring type.
    /// See <see href="https://github.com/tom-englert/Equatable.Fody"/> for details.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CustomEqualsAttribute : Attribute
    {
    }

    /// <summary>
    /// Apply this attribute to a method used to provide custom get hash code functionality when auto-implementing <see cref="IEquatable{T}"/> for the declaring type.
    /// See <see href="https://github.com/tom-englert/Equatable.Fody"/> for details.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CustomGetHashCodeAttribute : Attribute
    {
    }
}
