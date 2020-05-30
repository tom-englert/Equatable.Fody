namespace Equatable.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    internal static class ExtensionMethods
    {
        public static CustomAttribute? GetAttribute(this IEnumerable<CustomAttribute> attributes, string? attributeName)
        {
            return attributes.FirstOrDefault(attribute => attribute.Constructor?.DeclaringType?.FullName == attributeName);
        }

        public static void AddRange<T>(this IList<T> collection, params T[] values)
        {
            AddRange(collection, (IEnumerable<T>) values);
        }

        public static void AddRange<T>(this IList<T> collection, IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                collection.Add(value);
            }
        }

        public static void InsertRange<T>(this IList<T> collection, ref int index, params T[] values)
        {
            foreach (var value in values)
            {
                collection.Insert(index++, value);
            }
        }

        public static void Replace(this IList<Instruction> instructions, Instruction oldValue, Instruction newValue)
        {
            var index = instructions.IndexOf(oldValue);
            if (index == -1)
                throw new ArgumentException("Not a member of instructions", nameof(oldValue));

            instructions[index] = newValue;
        }

        public static bool AccessesMember(this MethodDefinition method, IMemberDefinition member)
        {
            return method.Body?.Instructions?.Any(inst => inst?.Operand == member) ?? false;
        }

        public static MethodDefinition FindMethod(this TypeDefinition type, string name, params Type[] parameters)
        {
            return type.Methods.First(x => (x.Name == name) && x.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters.Select(p => p.FullName)));
        }

        public static MethodDefinition? TryFindMethod(this TypeDefinition type, string name, params TypeReference[] parameters)
        {
            return type.Methods.FirstOrDefault(x => (x.Name == name) && x.Parameters.Select(p => p.ParameterType).SequenceEqual(parameters, TypeReferenceEqualityComparer.Default));
        }

        public static MethodDefinition? TryFindMethod(this TypeDefinition type, string name, params Type[] parameters)
        {
            return type.Methods.FirstOrDefault(x => (x.Name == name) && x.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters.Select(p => p.FullName)));
        }

        public static MethodDefinition? TryFindMethod(this TypeDefinition type, string name)
        {
            return type.Methods.FirstOrDefault(x => (x.Name == name) && x.Parameters.Count == 0);
        }

        public static bool GetEqualityOperator(this TypeDefinition type, out MethodDefinition? method)
        {
            method = type.TryFindMethod("op_Equality", type, type);

            return method?.IsStatic == true
                   && method.ReturnType?.FullName == typeof(bool).FullName && method.IsStatic;
        }

        public static FieldReference ReferenceFrom(this FieldDefinition field, TypeReference callingType)
        {
            return callingType.Module.ImportReference(field.InternalReferenceFrom(callingType));
        }

        private static FieldReference InternalReferenceFrom(this FieldDefinition field, TypeReference callingType)
        {
            if (!field.DeclaringType.HasGenericParameters)
                return field;

            return new FieldReference(field.Name, field.FieldType, field.DeclaringType.InternalReferenceFrom(callingType));
        }

        private static TypeReference GetGenericReference(this TypeReference type, ICollection<TypeReference> arguments)
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

        public static MethodReference ReferenceFrom(this MethodReference callee, TypeReference callingType)
        {
            return callingType.Module.ImportReference(callee.InternalReferenceFrom(callingType));
        }

        private static MethodReference InternalReferenceFrom(this MethodReference callee, TypeReference callingType)
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

        public static TypeReference ReferenceFrom(this TypeReference calleeType, TypeReference callingType)
        {
            return callingType.Module.ImportReference(calleeType.InternalReferenceFrom(callingType));
        }

        private static TypeReference InternalReferenceFrom(this TypeReference calleeType, TypeReference callingType)
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

        public static void AddMethod(this TypeDefinition type, MethodDefinition method)
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

        public static void MarkAsCompilerGenerated(this ICustomAttributeProvider target, SystemReferences systemReferences)
        {
            var assemblyName = typeof(ModuleWeaver).Assembly.GetName();
            var version = assemblyName.Version.ToString();
            var name = assemblyName.Name;

            var complierGenerated = new CustomAttribute(systemReferences.GeneratedCodeAttributeCtor);
            complierGenerated.ConstructorArguments.Add(new CustomAttributeArgument(systemReferences.TypeSystem.StringReference, name));
            complierGenerated.ConstructorArguments.Add(new CustomAttributeArgument(systemReferences.TypeSystem.StringReference, version));

            var debuggerNonUserCode = new CustomAttribute(systemReferences.DebuggerNonUserCodeAttributeCtor);

            target.CustomAttributes.AddRange(complierGenerated, debuggerNonUserCode);
        }

        public static SequencePoint? GetEntryPoint(this MethodReference? method)
        {
            var methodDefinition = method?.Resolve();

            return methodDefinition?.Module?.SymbolReader?.Read(methodDefinition)?.SequencePoints?.FirstOrDefault();
        }

        public static SequencePoint? GetEntryPoint(this TypeDefinition? type)
        {
            var classDefinition = type?.Resolve();
            var entryPoint = classDefinition.GetConstructors().FirstOrDefault() ?? classDefinition.GetMethods().OrderBy(m => m.IsSpecialName ? 1 : 0).FirstOrDefault();

            if (entryPoint == null)
                return null;

            return classDefinition?.Module?.SymbolReader?.Read(entryPoint)?.SequencePoints?.FirstOrDefault();
        }

    }
}