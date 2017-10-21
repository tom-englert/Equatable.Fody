namespace Equatable.Fody
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using JetBrains.Annotations;

    using Mono.Cecil;

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    internal class SystemReferences
    {
        public SystemReferences([NotNull] ModuleDefinition moduleDefinition, [NotNull] IAssemblyResolver assemblyResolver)
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

            TypeSystem = moduleDefinition.TypeSystem;
        }

        class CoreTypes
        {
            [NotNull, ItemNotNull]
            private readonly TypeDefinition[] _types;
            [NotNull]
            private readonly ModuleDefinition _moduleDefinition;

            public CoreTypes([NotNull] ModuleDefinition moduleDefinition, [NotNull] IAssemblyResolver assemblyResolver)
            {
                _moduleDefinition = moduleDefinition;
                var assemblies = new[] { "mscorlib", "System", "System.Reflection", "System.Runtime", "System.Diagnostics.Tools" };
                _types = assemblies.SelectMany(assembly => GetTypes(assemblyResolver, assembly)).ToArray();
            }

            [NotNull]
            public TypeDefinition GetTypeDefinition([NotNull] Type type)
            {
                return _types.First(x => x.FullName == type.FullName);
            }

            [NotNull]
            public TypeDefinition GetTypeDefinition<T>()
            {
                return GetTypeDefinition(typeof(T));
            }

            [NotNull]
            public TypeReference GetType(Type type)
            {
                return _moduleDefinition.ImportReference(GetTypeDefinition(type));
            }

            [NotNull]
            public TypeReference GetType<T>()
            {
                return _moduleDefinition.ImportReference(GetTypeDefinition<T>());
            }

            [NotNull]
            public MethodReference GetMethod<T>([NotNull] string name, [NotNull, ItemNotNull] params Type[] parameters)
            {
                return _moduleDefinition.ImportReference(GetTypeDefinition<T>().FindMethod(name, parameters));
            }

            [NotNull]
            public MethodReference GetMethod<T, TP1>([NotNull] string name)
            {
                return GetMethod<T>(name, typeof(TP1));
            }

            [NotNull]
            public MethodReference GetMethod<T, TP1, TP2>([NotNull] string name)
            {
                return GetMethod<T>(name, typeof(TP1), typeof(TP2));
            }

            [NotNull]
            public MethodReference GetMethod<T, TP1, TP2, TP3>([NotNull] string name)
            {
                return GetMethod<T>(name, typeof(TP1), typeof(TP2), typeof(TP3));
            }
        }

        [NotNull]
        public TypeSystem TypeSystem { get; }

        [NotNull]
        public TypeReference IEquatable { get; }

        [NotNull]
        public MethodReference ObjectEquals { get; }

        [NotNull]
        public MethodReference ObjectGetHashCode { get; }

        [NotNull]
        public MethodReference StringEquals { get; }

        [NotNull]
        public TypeReference StringComparer { get; }

        [NotNull]
        public MethodReference StringComparerGetHashCode { get; }

        [NotNull]
        public MethodReference StringComparerEquals { get; }

        [NotNull]
        public MethodReference GeneratedCodeAttributeCtor { get; }

        [NotNull]
        public MethodReference DebuggerNonUserCodeAttributeCtor { get; }

        [NotNull, ItemNotNull]
        private static IEnumerable<TypeDefinition> GetTypes([NotNull] IAssemblyResolver assemblyResolver, [NotNull] string name)
        {
            return assemblyResolver.Resolve(new AssemblyNameReference(name, null))?.MainModule.Types ?? Enumerable.Empty<TypeDefinition>();
        }
    }
}