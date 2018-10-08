﻿namespace AssemblyToProcess
{
    using System;
    using System.ComponentModel.Composition;

    using Equatable;

    using Xunit;

    using Target = ObjectWithValueTypeMembers;

    [ImplementsEquatable]
    internal class ObjectWithValueTypeMembers
    {
        [Equals]
        public string _field;

        [Equals]
        public int Property1 { get; set; }

        [Equals]
        public double Property2 { get; set; }

        public bool Property3 { get; set; }

        
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
                Property1 = 5,
                Property2 = 3.5,
                Property3 = false
            };

            var right = new Target
            {
                _field = "test",
                Property1 = 5,
                Property2 = 3.5,
                Property3 = false
            };

            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreEqualWhenAttributedPropertiesAreEqual()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = 5,
                Property2 = 3.5,
                Property3 = true
            };

            var right = new Target
            {
                _field = "test",
                Property1 = 5,
                Property2 = 3.5,
                Property3 = false
            };

            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenOneAttributedPropertyIsDifferent()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = 5,
                Property2 = 4.5,
                Property3 = true
            };

            var right = new Target
            {
                _field = "test",
                Property1 = 5,
                Property2 = 3.5,
                Property3 = false
            };

            Assert.NotEqual(left, right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenAllAttributedPropertiesAreDifferent()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = 4,
                Property2 = 4.5,
                Property3 = true
            };

            var right = new Target
            {
                _field = "test",
                Property1 = 5,
                Property2 = 3.5,
                Property3 = false
            };

            Assert.NotEqual(left, right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenTheFieldIsDifferent()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = 5,
                Property2 = 3.5,
                Property3 = false
            };

            var right = new Target
            {
                _field = "test1",
                Property1 = 5,
                Property2 = 3.5,
                Property3 = false
            };

            Assert.NotEqual(left, right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void AreDifferentWhenTheFieldIsDifferentInCase()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = 5,
                Property2 = 3.5,
                Property3 = false
            };

            var right = new Target
            {
                _field = "Test",
                Property1 = 5,
                Property2 = 3.5,
                Property3 = false
            };

            Assert.NotEqual(left, right);
            Assert.NotEqual(left.GetHashCode(), right.GetHashCode());
        }

        [Export]
        public static void IsDifferentFromNull()
        {
            var left = new Target
            {
                _field = "test",
                Property1 = 5,
                Property2 = 3.5,
                Property3 = false
            };

            Assert.NotEqual(left, null);
        }
    }
}
