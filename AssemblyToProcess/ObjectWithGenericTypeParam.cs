namespace AssemblyToProcess
{
    using System;
    using System.ComponentModel.Composition;

    using Equatable;

    using NUnit.Framework;

    namespace Base
    {
        using Target = ObjectWithGenericTypeParam<string, ObjectWithValueTypeMembers>;

        internal class ObjectWithGenericTypeParam<T1, T2>
        {
            [Equals]
            public string _field;

            [Equals]
            public Tuple<int, T1> Property1 { get; set; }

            [Equals]
            public Tuple<int, T2> Property2 { get; set; }
        }

        internal static class ObjectWithGenericTypeParamTests
        {
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

    namespace Derived
    {
        using AssemblyToProcess.Base;

        using Target = DerivedObjectWithGenericParam<ObjectWithValueTypeMembers>;

        internal class DerivedObjectWithGenericParam<T3>
            : ObjectWithGenericTypeParam<string, T3>
        {
            [Equals]
            public string Property3 { get; set; }
        }

        internal static class DerivedObjectWithGenericTypeParamTests
        {

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
                    Property1 = new Tuple<int, string>(5, "test"),
                    Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
                    Property3 = "x"
                };

                var right = new Target
                {
                    _field = "test",
                    Property1 = new Tuple<int, string>(5, "test"),
                    Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
                    Property3 = "x"
                };

                Assert.AreEqual(left, right);
                Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
            }

            [Export]
            public static void AreDifferentWhenTheDerivedPropertyIsDifferent()
            {
                var left = new Target
                {
                    _field = "test",
                    Property1 = new Tuple<int, string>(5, "test"),
                    Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
                    Property3 = "x"
                };

                var right = new Target
                {
                    _field = "test",
                    Property1 = new Tuple<int, string>(5, "test"),
                    Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
                    Property3 = "y"
                };

                Assert.AreNotEqual(left, right);
                Assert.AreNotEqual(left.GetHashCode(), right.GetHashCode());
            }

            [Export]
            public static void AreDifferentWhenOneBaseAttributedPropertyIsDifferent()
            {
                var left = new Target
                {
                    _field = "test",
                    Property1 = new Tuple<int, string>(5, "test"),
                    Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
                    Property3 = "x"
                };

                var right = new Target
                {
                    _field = "test",
                    Property1 = new Tuple<int, string>(5, "test"),
                    Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 4 }),
                    Property3 = "x"
                };

                Assert.AreNotEqual(left, right);
                Assert.AreNotEqual(left.GetHashCode(), right.GetHashCode());
            }

            [Export]
            public static void AreDifferentWhenAllBaseAttributedPropertiesAreDifferent()
            {
                var left = new Target
                {
                    _field = "test",
                    Property1 = new Tuple<int, string>(5, "test"),
                    Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
                    Property3 = "x"
                };

                var right = new Target
                {
                    _field = "test",
                    Property1 = new Tuple<int, string>(6, "test"),
                    Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 4 }),
                    Property3 = "x"
                };

                Assert.AreNotEqual(left, right);
                Assert.AreNotEqual(left.GetHashCode(), right.GetHashCode());
            }

            [Export]
            public static void AreDifferentWhenTheBaseFieldIsDifferent()
            {
                var left = new Target
                {
                    _field = "test",
                    Property1 = new Tuple<int, string>(5, "test"),
                    Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
                    Property3 = "x"
                };

                var right = new Target
                {
                    _field = "test1",
                    Property1 = new Tuple<int, string>(5, "test"),
                    Property2 = new Tuple<int, ObjectWithValueTypeMembers>(2, new ObjectWithValueTypeMembers { Property1 = 3 }),
                    Property3 = "x"
                };

                Assert.AreNotEqual(left, right);
                Assert.AreNotEqual(left.GetHashCode(), right.GetHashCode());
            }
        }
    }
}
