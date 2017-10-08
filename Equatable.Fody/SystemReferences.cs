namespace Equatable.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using JetBrains.Annotations;

    using Mono.Cecil;

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
    internal class SystemReferences
    {
        public SystemReferences([NotNull] ModuleDefinition moduleDefinition, [NotNull] IAssemblyResolver assemblyResolver)
        {
            var coreTypes = new CoreTypes(moduleDefinition, assemblyResolver);

            //GetFieldFromHandle = coreTypes.GetMethod<FieldInfo, RuntimeFieldHandle>(nameof(FieldInfo.GetFieldFromHandle));
            //PropertyInfoType = coreTypes.GetType<PropertyInfo>();
            //GetTypeFromHandle = coreTypes.GetMethod<Type, RuntimeTypeHandle>(nameof(Type.GetTypeFromHandle));
            //GetPropertyInfo = coreTypes.GetMethod<Type, string>(nameof(Type.GetProperty));

            IEquatableType = coreTypes.GetType(typeof(IEquatable<>));
            StringEquals = coreTypes.GetMethod<string, string, string, StringComparison>(nameof(string.Equals));
            ObjectEquals = coreTypes.GetMethod<object, object, object>(nameof(object.Equals));
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
                var assemblies = new[] { "mscorlib", "System.Reflection", "System.Runtime" };
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


        //[NotNull]
        //public MethodReference GetTypeFromHandle { get; }
        //[NotNull]
        //public TypeReference PropertyInfoType { get; }
        //[NotNull]
        //public MethodReference GetFieldFromHandle { get; }
        //[NotNull]
        //public MethodReference GetPropertyInfo { get; }

        [NotNull]
        public TypeReference IEquatableType { get; }

        [NotNull]
        public MethodReference ObjectEquals { get; }

        [NotNull]
        public MethodReference StringEquals { get; }

        [NotNull, ItemNotNull]
        private static IEnumerable<TypeDefinition> GetTypes([NotNull] IAssemblyResolver assemblyResolver, [NotNull] string name)
        {
            return assemblyResolver.Resolve(new AssemblyNameReference(name, null))?.MainModule.Types ?? Enumerable.Empty<TypeDefinition>();
        }
    }
}