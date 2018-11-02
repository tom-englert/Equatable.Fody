#pragma warning disable 169
#pragma warning disable 649

namespace AssemblyToProcess
{
    using System;

    using Equatable;

    class ObjectWithAlreadyImplementedInterface : IEquatable<ObjectWithAlreadyImplementedInterface>
    {
        [Equals]
        private int _field;

        public bool Equals(ObjectWithAlreadyImplementedInterface other)
        {
            throw new NotImplementedException();
        }
    }

    class ObjectWithAlreadyImplementedMethods
    {
        [Equals]
        private int _field;

        public override bool Equals(object obj)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return _field;
        }
    }

}
