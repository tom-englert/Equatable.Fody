namespace AssemblyToProcess
{
    using System;
    using System.ComponentModel.Composition;

    using Equatable;

    using Xunit;

    using Target = ObjectWithGenericTypeMembers;

    // [ImplementsEquatable] => show a warning, if not annotated.
    internal class ObjectWithGenericTypeMembers
    {
        [Equals]
        public string _field;

        [Equals]
        public Tuple<int, string> Property1 { get; set; }

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
                Property1 = new Tuple<int, string>(5, "test"),
            };

            var right = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(5, "test"),
            };

            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenFirstTupleItemIsDifferent()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(5, "test"),
            };

            var right = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(6, "test"),
            };

            Assert.NotEqual(left, right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenSecondTupleItemIsDifferent()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(5, "test"),
            };

            var right = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(5, "Test"),
            };

            Assert.NotEqual(left, right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }
    }
}
