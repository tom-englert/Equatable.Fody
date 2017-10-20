namespace Equatable.Fody
{
    using System;

    using JetBrains.Annotations;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    internal class WeavingException : Exception
    {
        public WeavingException([NotNull] string message, [CanBeNull] SequencePoint sequencePoint = null)
            : base(message)
        {
            SequencePoint = sequencePoint;
        }

        [CanBeNull]
        public SequencePoint SequencePoint { get; }
    }
}
