namespace AssemblyToProcess
{
    using System;
    using System.ComponentModel.Composition;

    using Equatable;

    using Xunit;

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

            Assert.True(target is IEquatable<Target>);
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

            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
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

            Assert.NotEqual(left, right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
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

            Assert.NotEqual(left, right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }
    }
}
