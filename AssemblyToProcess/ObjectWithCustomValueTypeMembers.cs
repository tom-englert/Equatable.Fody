namespace AssemblyToProcess
{
    using System;
    using System.ComponentModel.Composition;

    using Equatable;

    using NUnit.Framework;

    using Target = ObjectWithCustomValueTypeMembers;

    [ImplementsEquatable]
    internal class ObjectWithCustomValueTypeMembers
    {
        [Equals]
        public string _field;

        [Equals]
        public ReferenceStruct Property1 { get; set; }

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
                _field = "test",
                Property1 = new ReferenceStruct { Property1 = 5, Property2 = "test" },
            };

            var right = new Target
            {
                _field = "test",
                Property1 = new ReferenceStruct { Property1 = 5, Property2 = "test" },
            };

            Assert.AreEqual(left, right);
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenFirstItemIsDifferent()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = new ReferenceStruct { Property1 = 5, Property2 = "test" },
            };

            var right = new Target
            {
                _field = "test",
                Property1 = new ReferenceStruct { Property1 = 6, Property2 = "test" },
            };

            Assert.AreNotEqual(left, right);
            Assert.AreNotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenSecondItemIsDifferent()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = new ReferenceStruct { Property1 = 5, Property2 = "test" },
            };

            var right = new Target
            {
                _field = "test",
                Property1 = new ReferenceStruct { Property1 = 5, Property2 = "Test" },
            };

            Assert.AreNotEqual(left, right);
            Assert.AreNotEqual(left.GetHashCode(), right.GetHashCode());
        }
    }
}
