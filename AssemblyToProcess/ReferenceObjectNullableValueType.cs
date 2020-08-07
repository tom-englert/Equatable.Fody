namespace AssemblyToProcess
{
    using System;

    using Equatable;

    class ReferenceObjectNullableValueType : IEquatable<ReferenceObjectNullableValueType>
    {
        public int? Property1 { get; set; }

        private static bool InternalEquals(ReferenceObjectNullableValueType left, ReferenceObjectNullableValueType right)
        {
            return Equals(left.Property1, right.Property1);
        }

        public bool Equals(ReferenceObjectNullableValueType other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return InternalEquals(this, other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ReferenceObjectNullableValueType);
        }
    }

    [ImplementsEquatable]
    class ReferenceObjectNullableValueType1
    {
        [Equals]
        public int? Property1 { get; set; }
    }
}
