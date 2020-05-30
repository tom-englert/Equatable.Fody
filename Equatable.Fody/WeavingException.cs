namespace Equatable.Fody
{
    using System;

    using Mono.Cecil.Cil;

    internal class WeavingException : Exception
    {
        public WeavingException(string message, SequencePoint? sequencePoint = null)
            : base(message)
        {
            SequencePoint = sequencePoint;
        }

        public SequencePoint? SequencePoint { get; }
    }
}
