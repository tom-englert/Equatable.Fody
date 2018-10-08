﻿namespace AssemblyToProcess
{
    using System;
    using System.ComponentModel.Composition;

    using Equatable;

    using Xunit;

    using Target = ObjectWithCaseInsensitiveStrings;

    [ImplementsEquatable]
    internal class ObjectWithCaseInsensitiveStrings
    {
        [Equals(StringComparison.OrdinalIgnoreCase)]
        public string Property1 { get; set; }

        [Equals]
        public string Property2 { get; set; }

        [Export]
        public static void ImplementsIEquatable()
        {
            var target = new Target();

            Assert.True(target is IEquatable<Target>);
        }

        [Export]
        public static void AreEqualWhenAllPropertiesAreEqual()
        {
            var left = new Target
            {
                Property1 = "Test",
                Property2 = "a string",
            };

            var right = new Target
            {
                Property1 = "Test",
                Property2 = "a string",
            };

            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
            Assert.Equal(HashCode.Aggregate(0, HashCode.Aggregate(StringComparer.OrdinalIgnoreCase.GetHashCode(left.Property1), left.Property2.GetHashCode())), left.GetHashCode());
        }

        [Export]
        public static void AreEqualWhenAllPropertiesAreCaseEqual()
        {
            var left = new Target
            {
                Property1 = "Test",
                Property2 = "a string",
            };

            var right = new Target
            {
                Property1 = "test",
                Property2 = "a string",
            };

            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
            Assert.Equal(HashCode.Aggregate(0, HashCode.Aggregate(StringComparer.OrdinalIgnoreCase.GetHashCode(left.Property1), left.Property2.GetHashCode())), left.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenAllPropertiesAreNonCaseEqual()
        {
            var left = new Target
            {
                Property1 = "Test",
                Property2 = "a string",
            };

            var right = new Target
            {
                Property1 = "test",
                Property2 = "A string",
            };

            Assert.NotEqual(left, right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenOneAttributedPropertyIsDifferent()
        {
            var left = new Target
            {
                Property1 = "Test",
                Property2 = "a string",
            };

            var right = new Target
            {
                Property1 = "Test",
                Property2 = "b string",
            };

            Assert.NotEqual(left, right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenAllAttributedPropertiesAreDifferent()
        {
            var left = new Target
            {
                Property1 = "Test",
                Property2 = "a string",
            };

            var right = new Target
            {
                Property1 = "Different",
                Property2 = "c string",
            };

            Assert.NotEqual(left, right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }
    }
}
