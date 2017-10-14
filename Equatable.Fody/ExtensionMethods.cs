namespace Equatable.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    internal static class ExtensionMethods
    {
        [CanBeNull]
        public static CustomAttribute GetAttribute([NotNull, ItemNotNull] this IEnumerable<CustomAttribute> attributes, [CanBeNull] string attributeName)
        {
            return attributes.FirstOrDefault(attribute => attribute.Constructor?.DeclaringType?.FullName == attributeName);
        }

        [CanBeNull]
        public static SequencePoint GetEntryPoint([CanBeNull] this MethodDefinition method, [CanBeNull] ISymbolReader symbolReader)
        {
            if (method == null)
                return null;

            return symbolReader?.Read(method)?.SequencePoints?.FirstOrDefault();
        }

        [ContractAnnotation("propertyName:null => false")]
        public static bool IsPropertySetterCall([NotNull] this Instruction instruction, [CanBeNull] out string propertyName)
        {
            return IsPropertyCall(instruction, "set_", out propertyName);
        }

        [ContractAnnotation("propertyName:null => false")]
        public static bool IsPropertyGetterCall([NotNull] this Instruction instruction, [CanBeNull] out string propertyName)
        {
            return IsPropertyCall(instruction, "get_", out propertyName);
        }

        [ContractAnnotation("propertyName:null => false")]
        private static bool IsPropertyCall([NotNull] this Instruction instruction, [NotNull] string prefix, [CanBeNull] out string propertyName)
        {
            propertyName = null;

            if (instruction.OpCode.Code != Code.Call)
            {
                return false;
            }

            var operand = instruction.Operand as MethodDefinition;
            if (operand == null)
            {
                return false;
            }

            if (!(operand.IsSetter || operand.IsGetter))
            {
                return false;
            }

            var operandName = operand.Name;
            if (operandName?.StartsWith(prefix, StringComparison.Ordinal) != true)
            {
                return false;
            }

            propertyName = operandName.Substring(prefix.Length);
            return true;
        }

        [CanBeNull]
        public static FieldDefinition FindAutoPropertyBackingField([NotNull] this PropertyDefinition property, [NotNull, ItemNotNull] IEnumerable<FieldDefinition> fields)
        {
            var propertyName = property.Name;

            return fields.FirstOrDefault(field => field.Name == $"<{propertyName}>k__BackingField");
        }

        [ContractAnnotation("instruction:null => false")]
        public static bool IsExtensionMethodCall([CanBeNull] this Instruction instruction, [CanBeNull] string methodName)
        {
            if (instruction?.OpCode.Code != Code.Call)
                return false;

            var operand = instruction.Operand as GenericInstanceMethod;
            if (operand == null)
                return false;

            if (operand.DeclaringType?.FullName != "AutoProperties.BackingFieldAccessExtensions")
                return false;

            if (operand.Name != methodName)
                return false;

            return true;
        }

        [CanBeNull]
        public static MethodDefinition WhenAccessibleInDerivedClass([CanBeNull] this MethodDefinition baseMethodDefinition)
        {
            return baseMethodDefinition?.IsPrivate != false ? null : baseMethodDefinition;
        }

        [NotNull, ItemNotNull]
        public static IEnumerable<TypeDefinition> GetSelfAndBaseTypes([NotNull] this TypeDefinition type)
        {
            yield return type;

            while ((type = type.BaseType?.Resolve()) != null)
            {
                yield return type;
            }
        }

        [CanBeNull]
        public static TValue GetValueOrDefault<TKey, TValue>([NotNull] this IDictionary<TKey, TValue> dictionary, [CanBeNull] TKey key)
        {
            if (ReferenceEquals(key, null))
                return default(TValue);

            return dictionary.TryGetValue(key, out var value) ? value : default(TValue);
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
    }
}