namespace Equatable.Fody
{
    using Mono.Cecil;

    internal static class Parameter
    {
        public static ParameterDefinition Create(string name, TypeReference type)
        {
            return new ParameterDefinition(name, ParameterAttributes.None, type);
        }
    }
}
