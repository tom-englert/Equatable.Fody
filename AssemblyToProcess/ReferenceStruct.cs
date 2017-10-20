namespace AssemblyToProcess
{
    using System;

    using Equatable;

    struct ReferenceStruct : IEquatable<ReferenceStruct>
    {
        public int Property1 { get; set; }

        public string Property2 { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (!(obj is ReferenceStruct))
                return false;

            return Equals((ReferenceStruct)obj);
        }

        public bool Equals(ReferenceStruct other)
        {
            return InternalEquals(this, other);
        }

        public static bool operator == (ReferenceStruct left, ReferenceStruct right)
        {
            return InternalEquals(left, right);
        }
        public static bool operator !=(ReferenceStruct left, ReferenceStruct right)
        {
            return !InternalEquals(left, right);
        }

        private static bool InternalEquals(ReferenceStruct left, ReferenceStruct right)
        {
            return left.Property1 == right.Property1 && left.Property2 == right.Property2;
        }

        public override int GetHashCode()
        {
            return HashCode.Aggregate(Property1.GetHashCode(), Property2.GetHashCode());
        }
    }

    [ImplementsEquatable]
    struct WeavedReferenceStruct
    {
        [Equals]
        private bool _field;
    }
}
