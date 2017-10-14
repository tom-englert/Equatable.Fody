namespace AssemblyToProcess
{
    using System;

    using Equatable;

    public class Point
    {
        [Equals]
        private int _x;

        [Equals(StringComparison.OrdinalIgnoreCase)]
        public string Y { get; set; }

        public int Z { get; set; }

        [CustomEquals]
        bool CustomLogic(Point other)
        {
            return Z == other.Z || Z == 0 || other.Z == 0;
        }
    }

    public class CustomGetHashCode
    {
        [Equals]
        private int _x;

        [Equals]
        public int Y { get; set; }

        public int Z { get; set; }

        [CustomGetHashCode]
        int CustomGetHashCodeMethod()
        {
            return 42;
        }
    }
}
