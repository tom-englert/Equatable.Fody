namespace Equatable.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    internal static class ExtensionMethods
    {
        [CanBeNull]
        public static CustomAttribute GetAttribute([NotNull, ItemNotNull] this IEnumerable<CustomAttribute> attributes, [CanBeNull] string attributeName)
        {
            return attributes.FirstOrDefault(attribute => attribute.Constructor?.DeclaringType?.FullName == attributeName);
        }

        public static void AddRange<T>([NotNull, ItemCanBeNull] this IList<T> collection, [NotNull, ItemCanBeNull] params T[] values)
        {
            AddRange(collection, (IEnumerable<T>) values);
        }

        public static void AddRange<T>([NotNull, ItemCanBeNull] this IList<T> collection, [NotNull, ItemCanBeNull] IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                collection.Add(value);
            }
        }

        public static void InsertRange<T>([NotNull, ItemCanBeNull] this IList<T> collection, ref int index, [NotNull, ItemCanBeNull] params T[] values)
        {
            foreach (var value in values)
            {
                collection.Insert(index++, value);
            }
        }

        public static void Replace([NotNull, ItemNotNull] this IList<Instruction> instructions, [NotNull] Instruction oldValue, [NotNull] Instruction newValue)
        {
            var index = instructions.IndexOf(oldValue);
            if (index == -1)
                throw new ArgumentException("Not a member of instructions", nameof(oldValue));

            instructions[index] = newValue;
        }

        public static bool AccessesMember([NotNull] this MethodDefinition method, [NotNull] IMemberDefinition member)
        {
            return method.Body?.Instructions?.Any(inst => inst?.Operand == member) ?? false;
        }

        [NotNull]
        public static MethodDefinition FindMethod([NotNull] this TypeDefinition type, [NotNull] string name, [NotNull, ItemNotNull] params Type[] parameters)
        {
            return type.Methods.First(x => (x.Name == name) && x.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters.Select(p => p.FullName)));
        }

        [CanBeNull]
        public static MethodDefinition TryFindMethod([NotNull] this TypeDefinition type, [NotNull] string name, [NotNull, ItemNotNull] params TypeReference[] parameters)
        {
            return type.Methods.FirstOrDefault(x => (x.Name == name) && x.Parameters.Select(p => p.ParameterType).SequenceEqual(parameters, TypeReferenceEqualityComparer.Default));
        }

        [CanBeNull]
        public static MethodDefinition TryFindMethod([NotNull] this TypeDefinition type, [NotNull] string name, [NotNull, ItemNotNull] params Type[] parameters)
        {
            return type.Methods.FirstOrDefault(x => (x.Name == name) && x.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters.Select(p => p.FullName)));
        }

        [CanBeNull]
        public static MethodDefinition TryFindMethod([NotNull] this TypeDefinition type, [NotNull] string name)
        {
            return type.Methods.FirstOrDefault(x => (x.Name == name) && x.Parameters.Count == 0);
        }

        public static bool GetEqualityOperator([NotNull] this TypeDefinition type, [CanBeNull] out MethodDefinition method)
        {
            method = type.TryFindMethod("op_Equality", type, type);

            return method?.IsStatic == true
                   && method.ReturnType?.FullName == typeof(bool).FullName && method.IsStatic;
        }

        [NotNull]
        public static FieldReference ReferenceFrom([NotNull] this FieldDefinition field, [NotNull] TypeReference callingType)
        {
            return callingType.Module.ImportReference(field.InternalReferenceFrom(callingType));
        }

        [NotNull]
        private static FieldReference InternalReferenceFrom([NotNull] this FieldDefinition field, [NotNull] TypeReference callingType)
        {
            if (!field.DeclaringType.HasGenericParameters)
                return field;

            return new FieldReference(field.Name, field.FieldType, field.DeclaringType.InternalReferenceFrom(callingType));
        }

        [NotNull]
        private static TypeReference GetGenericReference([NotNull] this TypeReference type, [NotNull, ItemNotNull] ICollection<TypeReference> arguments)
        {
            if (!type.HasGenericParameters)
                return type;

            if (type.GenericParameters.Count != arguments.Count)
                throw new ArgumentException("Generic parameters mismatch");

            var instance = new GenericInstanceType(type);
            foreach (var argument in arguments)
                instance.GenericArguments.Add(argument);

            return instance;
        }

        [NotNull]
        public static MethodReference ReferenceFrom([NotNull] this MethodReference callee, [NotNull] TypeReference callingType)
        {
            return callingType.Module.ImportReference(callee.InternalReferenceFrom(callingType));
        }

        [NotNull]
        private static MethodReference InternalReferenceFrom([NotNull] this MethodReference callee, [NotNull] TypeReference callingType)
        {
            var declaringType = callee.DeclaringType.InternalReferenceFrom(callingType);

            var reference = new MethodReference(callee.Name, callee.ReturnType)
            {
                DeclaringType = declaringType,
                HasThis = callee.HasThis,
                ExplicitThis = callee.ExplicitThis,
                CallingConvention = callee.CallingConvention,
            };

            reference.Parameters.AddRange(callee.Parameters.Select(parameter => new ParameterDefinition(parameter.ParameterType)));
            reference.GenericParameters.AddRange(callee.GenericParameters.Select(parameter => new GenericParameter(parameter.Name, reference)));

            return reference;
        }

        [NotNull]
        public static TypeReference ReferenceFrom([NotNull] this TypeReference calleeType, [NotNull] TypeReference callingType)
        {
            return callingType.Module.ImportReference(calleeType.InternalReferenceFrom(callingType));
        }

        [NotNull]
        private static TypeReference InternalReferenceFrom([NotNull] this TypeReference calleeType, [NotNull] TypeReference callingType)
        {
            var calleeTypeDefinition = calleeType.Resolve();

            var baseType = callingType;
            var genericParameters = callingType.GenericParameters.ToArray();
            var genericArguments = genericParameters.Cast<TypeReference>().ToArray();

            while (baseType.Resolve() != calleeTypeDefinition)
            {
                baseType = baseType.Resolve().BaseType;
                if (baseType == null)
                    return calleeType;

                if (!(baseType is IGenericInstance genericInstance))
                    continue;

                var arguments = genericInstance.GenericArguments.ToArray();

                for (var i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];

                    if (!argument.ContainsGenericParameter)
                        continue;

                    for (var k = 0; k < genericParameters.Length; k++)
                    {
                        if (genericParameters[k].Name != argument.Name)
                            continue;

                        arguments[i] = genericArguments[k];
                        break;
                    }
                }

                genericArguments = arguments;
                genericParameters = baseType.GetElementType().GenericParameters.ToArray();
            }

            return calleeType.GetGenericReference(genericArguments.ToArray());
        }

        public static void AddMethod([NotNull] this TypeDefinition type, [NotNull] MethodDefinition method)
        {

            var existing = type.Methods.FirstOrDefault(m =>
                m.Name == method.Name
                && m.Parameters.Select(p => p.ParameterType).SequenceEqual(method.Parameters.Select(p => p.ParameterType), TypeReferenceEqualityComparer.Default)
                && m.GenericParameters.Count == method.GenericParameters.Count);

            if (existing != null)
            {
                throw new WeavingException($"The class {type} already has a method {existing}", existing.GetEntryPoint());
            }

            type.Methods.Add(method);
        }

        public static void MarkAsComplierGenerated([NotNull] this ICustomAttributeProvider target, [NotNull] SystemReferences systemReferences)
        {
            var assemblyName = typeof(ModuleWeaver).Assembly.GetName();
            var version = assemblyName.Version.ToString();
            var name = assemblyName.Name;

            var complierGenerated = new CustomAttribute(systemReferences.GeneratedCodeAttributeCtor);
            complierGenerated.ConstructorArguments.Add(new CustomAttributeArgument(systemReferences.TypeSystem.String, name));
            complierGenerated.ConstructorArguments.Add(new CustomAttributeArgument(systemReferences.TypeSystem.String, version));

            var debuggerNonUserCode = new CustomAttribute(systemReferences.DebuggerNonUserCodeAttributeCtor);

            target.CustomAttributes.AddRange(complierGenerated, debuggerNonUserCode);
        }

        [CanBeNull]
        public static SequencePoint GetEntryPoint([CanBeNull] this MethodReference method)
        {
            var methodDefinition = method?.Resolve();

            return methodDefinition?.Module?.SymbolReader?.Read(methodDefinition)?.SequencePoints?.FirstOrDefault();
        }

        [CanBeNull]
        public static SequencePoint GetEntryPoint([CanBeNull] this TypeDefinition type)
        {
            var classDefinition = type?.Resolve();
            var entryPoint = classDefinition.GetConstructors().FirstOrDefault() ?? classDefinition.GetMethods().OrderBy(m => m.IsSpecialName ? 1 : 0).FirstOrDefault();

            if (entryPoint == null)
                return null;

            return classDefinition?.Module?.SymbolReader?.Read(entryPoint)?.SequencePoints?.FirstOrDefault();
        }

    }
}