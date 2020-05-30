namespace AssemblyToProcess
{
    using System;

    using Equatable;

    class ReferenceObject : IEquatable<ReferenceObject>
    {
        private bool _bool { get; set; }
        private double _double { get; set; }

        private string _string;
        private ReferenceObject _referenceObject;
        private ReferenceObject _referenceObject1 { get; set; }
        private ReferenceObject[] _objectArray;
        private ReferenceStruct[] _valueArray;
        private string[] _stringArray;
        private Tuple<ReferenceObject, ReferenceStruct> _generic;
        private ReferenceStruct _struct;
        private WeavedReferenceStruct _struct2;
        private WeavedReferenceObject _object1;

        public override int GetHashCode()
        {
            unchecked
            {
                var x = 0;
                x = ((x << 5) + x) ^ _bool.GetHashCode();
                x = ((x << 5) + x) ^ _double.GetHashCode();
                return x;
            }
        }

        public int GetHashCode1()
        {
            unchecked
            {
                var x = 0;
                x = ((x << 5) + x);
                x ^= _bool.GetHashCode();
                x = ((x << 5) + x);
                x ^= _double.GetHashCode();
                return x;
            }
        }


        int GetHashCode2()
        {
            return Aggregate(Aggregate(Aggregate(0, HashCode.GetStringHashCode(_string, StringComparer.OrdinalIgnoreCase)), HashCode.GetHashCode(_struct2)), HashCode.GetHashCode(_referenceObject1));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ReferenceObject);
        }

        public bool Equals(ReferenceObject other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return InternalEquals(this, other);
        }

        private static bool InternalEquals(ReferenceObject left, ReferenceObject right)
        {
            if (left == null)
                return right == null;
            if (right == null)
                return false;

            return left._bool == right._bool
                   && left._double == right._double;
        }


        private static bool InternalEquals_Ref(ReferenceObject left, ReferenceObject right)
        {
            return left._bool ==  right._bool
                  && left._double == right._double
                   && left._struct.Equals(right._struct)
                   && left._struct == right._struct
                   && left._struct2.Equals(right._struct2)
                   && string.Equals(left._string, right._string, StringComparison.Ordinal)
                   && Equals(left._string, right._string)
                   && Equals(left._referenceObject, right._referenceObject)
                   && Equals(left._objectArray, right._objectArray)
                   && Equals(left._valueArray, right._valueArray)
                   && Equals(left._stringArray, right._stringArray)
                   && Equals(left._generic, right._generic)
                ;
        }

        public static int Aggregate(int hash1, int hash2)
        {
            unchecked
            {
                return ((hash1 << 5) + hash1) ^ hash2;
            }
        }
    }

    [ImplementsEquatable]
    class WeavedReferenceObject
    {
        [Equals]
        private bool _field;
    }
}
