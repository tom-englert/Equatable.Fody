namespace AssemblyToProcess
{
    using System;

    using Equatable;

    class ReferenceObjectSimple : IEquatable<ReferenceObjectSimple>
    {
        public string Property1 { get; set; }

        private static bool InternalEquals(ReferenceObjectSimple left, ReferenceObjectSimple right)
        {
            return Equals(left.Property1, right.Property1);
        }

        public bool Equals(ReferenceObjectSimple other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return InternalEquals(this, other);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ReferenceObjectSimple);
        }
    }

    [ImplementsEquatable]
    class ReferenceObjectSimple1
    {
        [Equals]
        public string Property1 { get; set; }
    }
}
