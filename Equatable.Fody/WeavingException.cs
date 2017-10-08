namespace Equatable.Fody
{
    using System;

    using JetBrains.Annotations;

    using Mono.Cecil;

    internal class WeavingException : Exception
    {
        public WeavingException([NotNull] string message, [CanBeNull] MethodReference method = null)
            : base(message)
        {
            Method = method;
        }

        [CanBeNull]
        public MethodReference Method { get; }
    }
}
