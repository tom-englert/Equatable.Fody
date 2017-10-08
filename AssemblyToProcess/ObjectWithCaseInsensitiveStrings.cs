namespace AssemblyToProcess
{
    using System;
    using System.ComponentModel.Composition;

    using Equatable;

    using NUnit.Framework;

    using Target = ObjectWithCaseInsensitiveStrings;

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

            Assert.IsTrue(target is IEquatable<Target>);
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

            Assert.AreEqual(left, right);
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

            Assert.AreEqual(left, right);
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

            Assert.AreNotEqual(left, right);
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

            Assert.AreNotEqual(left, right);
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

            Assert.AreNotEqual(left, right);
        }
    }
}
