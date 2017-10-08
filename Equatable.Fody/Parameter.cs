namespace Equatable.Fody
{
    using JetBrains.Annotations;

    using Mono.Cecil;

    internal static class Parameter
    {
        [NotNull]
        public static ParameterDefinition Create([NotNull] string name, [NotNull] TypeReference type)
        {
            return new ParameterDefinition(name, ParameterAttributes.None, type);
        }
    }
}
