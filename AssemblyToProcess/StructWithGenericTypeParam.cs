namespace AssemblyToProcess
{
    using System;
    using System.ComponentModel.Composition;

    using Equatable;

    using NUnit.Framework;

    using Target = StructWithGenericTypeParam<string, ObjectWithValueTypeMembers>;

    [ImplementsEquatable]
    internal struct StructWithGenericTypeParam<T1, T2>
    {
        [Equals]
        public string _field;

        [Equals]
        public Tuple<int, T1> Property1 { get; set; }

        [Equals]
        public Tuple<int, T2> Property2 { get; set; }
    }

    [ImplementsEquatable] // Give a warning that no member is annotated with an equals-attribute.
    internal struct StructWithGenericTypeParamRef<T1, T2> : IEquatable<StructWithGenericTypeParamRef<T1, T2>>
    {
        public string _field;

        public Tuple<int, T1> Property1 { get; set; }

        public Tuple<int, T2> Property2 { get; set; }

        static bool InternalEquals(StructWithGenericTypeParamRef<T1, T2> left, StructWithGenericTypeParamRef<T1, T2> right)
        {
            return StringComparer.Ordinal.Equals(left._field, right._field)
                   && Equals(left.Property1, right.Property1)
                   && Equals(left.Property2, right.Property2);

        }

        public bool Equals(StructWithGenericTypeParamRef<T1, T2> other)
        {
            throw new NotImplementedException();
        }
    }

    internal static class StructWithGenericTypeParamTests
    {
        [Export]
        public static void ImplementsIEquatable()
        {
            object target = new Target();

            Assert.IsTrue(target is IEquatable<Target>);
        }

        [Export]
        public static void AreEqualWhenAllPropertiesAreEqual()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(5, "test"),
                Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
            };

            var right = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(5, "test"),
                Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
            };

            Assert.AreEqual(left, right);
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenOneAttributedPropertyIsDifferent()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(5, "test"),
                Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
            };

            var right = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(5, "test"),
                Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 4 }),
            };

            Assert.AreNotEqual(left, right);
            Assert.AreNotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenAllAttributedPropertiesAreDifferent()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(5, "test"),
                Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
            };

            var right = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(6, "test"),
                Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 4 }),
            };

            Assert.AreNotEqual(left, right);
            Assert.AreNotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenTheFieldIsDifferent()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = new Tuple<int, string>(5, "test"),
                Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
            };

            var right = new Target
            {
                _field = "test1",
                Property1 = new Tuple<int, string>(5, "test"),
                Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
            };

            Assert.AreNotEqual(left, right);
            Assert.AreNotEqual(left.GetHashCode(), right.GetHashCode());
        }
    }
}
