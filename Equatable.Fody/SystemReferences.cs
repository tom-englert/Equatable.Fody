namespace Equatable.Fody
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Mono.Cecil;

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    internal class SystemReferences
    {
        public SystemReferences(global::Fody.TypeSystem typeSystem, ModuleDefinition moduleDefinition, IAssemblyResolver assemblyResolver)
        {
            var coreTypes = new CoreTypes(moduleDefinition, assemblyResolver);

            IEquatable = coreTypes.GetType(typeof(IEquatable<>));
            StringEquals = coreTypes.GetMethod<string, string, string, StringComparison>(nameof(string.Equals));
            ObjectEquals = coreTypes.GetMethod<object, object, object>(nameof(object.Equals));
            ObjectGetHashCode = coreTypes.GetMethod<object>(nameof(object.GetHashCode));

            StringComparer = coreTypes.GetType<StringComparer>();
            StringComparerGetHashCode = coreTypes.GetMethod<StringComparer, string>(nameof(StringComparer.GetHashCode));
            StringComparerEquals = coreTypes.GetMethod<StringComparer, string, string>(nameof(StringComparer.Equals));

            GeneratedCodeAttributeCtor = coreTypes.GetMethod<GeneratedCodeAttribute, string, string>(".ctor");
            DebuggerNonUserCodeAttributeCtor = coreTypes.GetMethod<DebuggerNonUserCodeAttribute>(".ctor");

            TypeSystem = typeSystem;
        }

        class CoreTypes
        {
            private readonly TypeDefinition[] _types;
                private readonly ModuleDefinition _moduleDefinition;

            public CoreTypes(ModuleDefinition moduleDefinition, IAssemblyResolver assemblyResolver)
            {
                _moduleDefinition = moduleDefinition;
                var assemblies = new[] { "mscorlib", "System", "System.Reflection", "System.Runtime", "System.Diagnostics.Tools", "netstandard", "System.Runtime.Extensions", "System.Diagnostics.Debug" };
                _types = assemblies.SelectMany(assembly => GetTypes(assemblyResolver, assembly)).ToArray();
            }

                public TypeDefinition GetTypeDefinition(Type type)
            {
                return _types.FirstOrDefault(x => x.FullName == type.FullName) ?? throw new InvalidOperationException($"Type {type} not found");
            }

                public TypeDefinition GetTypeDefinition<T>()
            {
                return GetTypeDefinition(typeof(T));
            }

                public TypeReference GetType(Type type)
            {
                return _moduleDefinition.ImportReference(GetTypeDefinition(type));
            }

                public TypeReference GetType<T>()
            {
                return _moduleDefinition.ImportReference(GetTypeDefinition<T>());
            }

                public MethodReference GetMethod<T>(string name, params Type[] parameters)
            {
                return _moduleDefinition.ImportReference(GetTypeDefinition<T>().FindMethod(name, parameters));
            }

                public MethodReference GetMethod<T, TP1>(string name)
            {
                return GetMethod<T>(name, typeof(TP1));
            }

                public MethodReference GetMethod<T, TP1, TP2>(string name)
            {
                return GetMethod<T>(name, typeof(TP1), typeof(TP2));
            }

                public MethodReference GetMethod<T, TP1, TP2, TP3>(string name)
            {
                return GetMethod<T>(name, typeof(TP1), typeof(TP2), typeof(TP3));
            }
        }

        public global::Fody.TypeSystem TypeSystem { get; }

        public TypeReference IEquatable { get; }

        public MethodReference ObjectEquals { get; }

        public MethodReference ObjectGetHashCode { get; }

        public MethodReference StringEquals { get; }

        public TypeReference StringComparer { get; }

        public MethodReference StringComparerGetHashCode { get; }

        public MethodReference StringComparerEquals { get; }

        public MethodReference GeneratedCodeAttributeCtor { get; }

        public MethodReference DebuggerNonUserCodeAttributeCtor { get; }

        private static IEnumerable<TypeDefinition> GetTypes(IAssemblyResolver assemblyResolver, string name)
        {
            return assemblyResolver.Resolve(new AssemblyNameReference(name, null))?.MainModule.Types ?? Enumerable.Empty<TypeDefinition>();
        }
    }
}