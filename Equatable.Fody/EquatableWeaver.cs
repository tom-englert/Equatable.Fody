namespace Equatable.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    public class EquatableWeaver
    {
        [NotNull]
        private readonly ILogger _logger;
        [NotNull]
        private readonly ModuleDefinition _moduleDefinition;
        [NotNull]
        private readonly SystemReferences _systemReferences;
        [NotNull, ItemNotNull]
        private static readonly HashSet<string> _simpleTypes = new HashSet<string>(new[]
        {
            "System.Boolean",
            "System.Byte",
            "System.SByte",
            "System.Char",
            "System.Double",
            "System.Single",
            "System.Int32",
            "System.UInt32",
            "System.Int64",
            "System.UInt64",
            "System.Int16",
            "System.UInt16"
        });
        [NotNull]
        private readonly MethodDefinition _aggregateHashCodeMethod;
        [NotNull, ItemNotNull]
        private readonly IList<Action> _postProcessActions = new List<Action>();
        [NotNull]
        private readonly MethodDefinition _getHashCode;
        [NotNull]
        private readonly MethodDefinition _getStringHashCode;

        public EquatableWeaver([NotNull] ModuleWeaver moduleWeaver)
        {
            _logger = moduleWeaver;
            _moduleDefinition = moduleWeaver.ModuleDefinition;
            _systemReferences = moduleWeaver.SystemReferences;
            var hashCodeMethod = InjectHashCode(_moduleDefinition);
            _aggregateHashCodeMethod = InjectAggregate(hashCodeMethod);
            _getHashCode = InjectGetHashCode(hashCodeMethod, _systemReferences);
            _getStringHashCode = InjectGetStringHashCode(hashCodeMethod, _systemReferences);
        }

        public void Execute()
        {
            var allTypes = _moduleDefinition.GetTypes();

            // ReSharper disable once AssignNullToNotNullAttribute
            var allClasses = allTypes
                .Where(type => type != null && type.IsClass && (type.BaseType != null));

            foreach (var classDefinition in allClasses)
            {
                var membersToCompare = MemberDefinition.GetMembers(classDefinition)
                    .Where(member => member.EqualsAttribute != null)
                    .ToArray();

                var customEquals = classDefinition.Methods.FirstOrDefault(m => m.CustomAttributes.GetAttribute(AttributeNames.CustomEquals) != null);
                var customGetHashCode = classDefinition.Methods.FirstOrDefault(m => m.CustomAttributes.GetAttribute(AttributeNames.CustomGetHashCode) != null);

                // TODO: verify signature of custom methods...

                if (!membersToCompare.Any() && (customEquals == null) && (customGetHashCode == null))
                    continue;

                InjectEquatable(classDefinition, membersToCompare, customEquals, customGetHashCode);
            }

            foreach (var action in _postProcessActions)
            {
                action();
            }
        }

        [NotNull]
        private static TypeDefinition InjectHashCode([NotNull] ModuleDefinition moduleDefinition)
        {
            /*
            static int Aggregate(int hash1, int hash2)
            {
                unchecked
                {
                    return ((hash1 << 5) + hash1) ^ hash2;
                }
            }
            */

            var type = new TypeDefinition("", "<HashCode>", TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, moduleDefinition.TypeSystem.Object);

            moduleDefinition.Types.Add(type);

            return type;
        }

        [NotNull]
        private static MethodDefinition InjectAggregate([NotNull] TypeDefinition type)
        {
            var typeSystem = type.Module.TypeSystem;
            var method = new MethodDefinition("Aggregate", MethodAttributes.Static | MethodAttributes.HideBySig, typeSystem.Int32);

            method.Parameters.AddRange(Parameter.Create("hash1", typeSystem.Int32), Parameter.Create("hash2", typeSystem.Int32));

            method.Body.Instructions.AddRange(
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldc_I4_5),
                Instruction.Create(OpCodes.Shl),
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Add),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Xor),
                Instruction.Create(OpCodes.Ret)
            );

            type.Methods.Add(method);

            return method;
        }

        [NotNull]
        private static MethodDefinition InjectGetHashCode([NotNull] TypeDefinition type, [NotNull] SystemReferences systemReferences)
        {
            var typeSystem = type.Module.TypeSystem;
            var method = new MethodDefinition("GetHashCode", MethodAttributes.Static | MethodAttributes.HideBySig, typeSystem.Int32);

            method.Parameters.AddRange(Parameter.Create("value", typeSystem.Object));

            Instruction c1, l1;

            var instructions = method.Body.Instructions;

            instructions.AddRange(
                Instruction.Create(OpCodes.Ldarg_0),
                c1 = Instruction.Create(OpCodes.Nop),
                Instruction.Create(OpCodes.Ldc_I4_0),
                Instruction.Create(OpCodes.Ret),
                l1 = Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Callvirt, systemReferences.ObjectGetHashCode),
                Instruction.Create(OpCodes.Ret));

            instructions.Replace(c1, Instruction.Create(OpCodes.Brtrue_S, l1));

            type.Methods.Add(method);

            return method;
        }

        [NotNull]
        private static MethodDefinition InjectGetStringHashCode([NotNull] TypeDefinition type, [NotNull] SystemReferences systemReferences)
        {
            var typeSystem = type.Module.TypeSystem;
            var method = new MethodDefinition("GetStringHashCode", MethodAttributes.Static | MethodAttributes.HideBySig, typeSystem.Int32);

            method.Parameters.AddRange(
                Parameter.Create("value", typeSystem.String),
                Parameter.Create("comparer", systemReferences.StringComparer));

            Instruction c1, l1;

            var instructions = method.Body.Instructions;

            instructions.AddRange(
                Instruction.Create(OpCodes.Ldarg_0),
                c1 = Instruction.Create(OpCodes.Nop),
                Instruction.Create(OpCodes.Ldc_I4_0),
                Instruction.Create(OpCodes.Ret),
                l1 = Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Callvirt, systemReferences.StringComparerGetHashCode),
                Instruction.Create(OpCodes.Ret));

            instructions.Replace(c1, Instruction.Create(OpCodes.Brtrue_S, l1));

            type.Methods.Add(method);

            return method;
        }


        private void InjectEquatable([NotNull] TypeDefinition classDefinition, [NotNull, ItemNotNull] ICollection<MemberDefinition> membersToCompare, [CanBeNull] MethodDefinition customEquals, [CanBeNull] MethodDefinition customGetHashCode)
        {
            classDefinition.Interfaces.Add(new InterfaceImplementation(_systemReferences.IEquatable.MakeGenericInstanceType(classDefinition.ReferenceFrom(classDefinition))));

            var baseImplementations = GetBaseEqualsAndHashCode(classDefinition);

            var methods = classDefinition.Methods;

            var internalEqualsMethod = CreateInternalEqualsMethod(classDefinition, membersToCompare, customEquals, baseImplementations.equals);
            methods.Add(internalEqualsMethod);

            var equalsTypeMethod = CreateTypedEqualsMethod(classDefinition, internalEqualsMethod);
            methods.Add(equalsTypeMethod);

            var getHashCodeMethod = CreateGetHashCode(classDefinition, membersToCompare, customGetHashCode, baseImplementations.getHashCode);
            methods.Add(getHashCodeMethod);

            methods.Add(CreateObjectEqualsOverrideMethod(classDefinition, equalsTypeMethod));
            methods.Add(CreateEqualityOperator(classDefinition, internalEqualsMethod));
            methods.Add(CreateInequalityOperator(classDefinition, internalEqualsMethod));
        }

        private (MethodDefinition equals, MethodDefinition getHashCode) GetBaseEqualsAndHashCode([NotNull] TypeDefinition classDefinition)
        {
            var baseType = classDefinition.BaseType.Resolve();

            if ((baseType.FullName == typeof(object).FullName) || (baseType.FullName == typeof(ValueType).FullName))
                return (null, null);

            var baseEquals = baseType.TryFindMethod("Equals", baseType);
            var baseGetHashCode = baseType.TryFindMethod("GetHashCode");

            if (baseEquals == null)
            {
                if (baseGetHashCode != null)
                {
                    _logger.LogWarning($"{baseType} overrides GetHashCode, but does not override Equals!");
                    return (null, null);
                }
            }
            else if (baseGetHashCode == null)
            {
                _logger.LogWarning($"{baseType} overrides Equals, but does not override GetHashCode!");
                return (null, null);
            }

            return (baseEquals, baseGetHashCode);
        }

        [NotNull]
        private MethodDefinition CreateInternalEqualsMethod([NotNull] TypeDefinition classDefinition, [NotNull, ItemNotNull] IEnumerable<MemberDefinition> membersToCompare, [CanBeNull] MethodDefinition customEqualsMethod, [CanBeNull] MethodDefinition baseEqualsMethod)
        {
            var method = new MethodDefinition("<InternalEquals>", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, _moduleDefinition.TypeSystem.Boolean);

            var classReference = classDefinition.ReferenceFrom(classDefinition);

            method.Parameters.AddRange(
                Parameter.Create("left", classReference),
                Parameter.Create("right", classReference));

            var instructions = method.Body.Instructions;

            instructions.AddRange(
                Instruction.Create(OpCodes.Ldc_I4_0),
                Instruction.Create(OpCodes.Ret));

            _postProcessActions.Add(() =>
            {

                var returnFalseLabel = instructions[0];

                var index = 0;

                if (baseEqualsMethod != null)
                {
                    instructions.InsertRange(ref index,
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Ldarg_1),
                        Instruction.Create(OpCodes.Castclass, baseEqualsMethod.Parameters.First().ParameterType.Resolve().ReferenceFrom(classDefinition)),
                        Instruction.Create(OpCodes.Call, baseEqualsMethod.ReferenceFrom(classDefinition)),
                        Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
                }

                if (customEqualsMethod != null)
                {
                    instructions.InsertRange(ref index,
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Ldarg_1),
                        Instruction.Create(OpCodes.Call, customEqualsMethod.ReferenceFrom(classDefinition)),
                        Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
                }

                foreach (var memberDefinition in membersToCompare)
                {
                    var loadArgument0Instruction = memberDefinition.GetLoadArgumentInstruction(method, 0);
                    var loadArgument1Instruction = memberDefinition.GetLoadArgumentInstruction(method, 1);
                    var loadInstruction = memberDefinition.GetValueInstruction(classDefinition);
                    var memberType = memberDefinition.MemberType.Resolve();

                    if (memberType.FullName == typeof(string).FullName)
                    {
                        var comparison = (StringComparison)(memberDefinition.EqualsAttribute.ConstructorArguments.Select(a => a.Value).FirstOrDefault() ?? default(StringComparison));
                        var comparer = _systemReferences.StringComparer;
                        var getComparerMethod = comparer.Resolve().Properties.FirstOrDefault(p => p.Name == comparison.ToString())?.GetMethod;

                        instructions.InsertRange(ref index,
                            Instruction.Create(OpCodes.Call, getComparerMethod.ReferenceFrom(classDefinition)),
                            loadArgument0Instruction,
                            loadInstruction,
                            loadArgument1Instruction,
                            loadInstruction,
                            Instruction.Create(OpCodes.Callvirt, _systemReferences.StringComparerEquals),
                            Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
                    }
                    else if (memberType.IsValueType)
                    {
                        if (_simpleTypes.Contains(memberType.FullName))
                        {
                            instructions.InsertRange(ref index,
                                loadArgument0Instruction,
                                loadInstruction,
                                loadArgument1Instruction,
                                loadInstruction,
                                Instruction.Create(OpCodes.Bne_Un, returnFalseLabel));
                        }
                        else if (memberType.GetEqualityOperator(out var equalityMethod))
                        {
                            instructions.InsertRange(ref index,
                                loadArgument0Instruction,
                                loadInstruction,
                                loadArgument1Instruction,
                                loadInstruction,
                                Instruction.Create(OpCodes.Call, _moduleDefinition.ImportReference(equalityMethod)),
                                Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
                        }
                        else
                        {
                            instructions.InsertRange(ref index,
                                loadArgument0Instruction,
                                loadInstruction,
                                Instruction.Create(OpCodes.Box, memberType),
                                loadArgument1Instruction,
                                loadInstruction,
                                Instruction.Create(OpCodes.Box, memberType),
                                Instruction.Create(OpCodes.Call, _systemReferences.ObjectEquals),
                                Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
                        }
                    }
                    else
                    {
                        if (memberType.GetEqualityOperator(out var equalityMethod))
                        {
                            instructions.InsertRange(ref index,
                                loadArgument0Instruction,
                                loadInstruction,
                                loadArgument1Instruction,
                                loadInstruction,
                                Instruction.Create(OpCodes.Call, _moduleDefinition.ImportReference(equalityMethod)),
                                Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
                        }
                        else
                        {
                            instructions.InsertRange(ref index,
                                loadArgument0Instruction,
                                loadInstruction,
                                loadArgument1Instruction,
                                loadInstruction,
                                Instruction.Create(OpCodes.Call, _systemReferences.ObjectEquals),
                                Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
                        }
                    }
                }

                var lastIndex = index - 1;
                if (index >= 0)
                {
                    if (instructions[lastIndex].OpCode == OpCodes.Bne_Un)
                    {
                        instructions[lastIndex] = Instruction.Create(OpCodes.Ceq);
                    }
                    else
                    {
                        instructions.RemoveAt(lastIndex);
                        index -= 1;
                    }
                }

                instructions.InsertRange(ref index,
                    Instruction.Create(OpCodes.Ret));

                if (!instructions.Any(i => i.Operand == returnFalseLabel))
                {
                    instructions.RemoveAt(index);
                    instructions.RemoveAt(index);
                }
            });

            return method;
        }

        [NotNull]
        private MethodDefinition CreateTypedEqualsMethod([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition internalEqualsMethod)
        {
            var method = new MethodDefinition("Equals", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final, _moduleDefinition.TypeSystem.Boolean);

            method.Parameters.Add(Parameter.Create("other", classDefinition.ReferenceFrom(classDefinition)));
            method.IsFinal = true;

            var instructions = method.Body.Instructions;

            if (classDefinition.IsValueType)
            {
                instructions.AddRange(
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldobj, classDefinition.ReferenceFrom(classDefinition)),
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Call, internalEqualsMethod.ReferenceFrom(classDefinition)));
            }
            else
            {
                Instruction c1, c2, l1, l2;

                instructions.AddRange(
                    Instruction.Create(OpCodes.Ldarg_1),
                    c1 = Instruction.Create(OpCodes.Nop),
                    Instruction.Create(OpCodes.Ldc_I4_0),
                    Instruction.Create(OpCodes.Ret),

                    l1 = Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldarg_1),
                    c2 = Instruction.Create(OpCodes.Nop),
                    Instruction.Create(OpCodes.Ldc_I4_1),
                    Instruction.Create(OpCodes.Ret),

                    l2 = Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Call, internalEqualsMethod.ReferenceFrom(classDefinition)));

                instructions.Replace(c1, Instruction.Create(OpCodes.Brtrue_S, l1));
                instructions.Replace(c2, Instruction.Create(OpCodes.Bne_Un_S, l2));
            }

            instructions.Add(Instruction.Create(OpCodes.Ret));

            return method;
        }

        [NotNull]
        private MethodDefinition CreateObjectEqualsOverrideMethod([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition equalsTypeMethod)
        {
            var method = new MethodDefinition("Equals", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, _moduleDefinition.TypeSystem.Boolean);

            method.Parameters.Add(Parameter.Create("obj", _moduleDefinition.TypeSystem.Object));

            var instructions = method.Body.Instructions;

            if (classDefinition.IsValueType)
            {
                Instruction c1, c2, l1, l2;

                instructions.AddRange(
                    Instruction.Create(OpCodes.Ldarg_1),
                    c1 = Instruction.Create(OpCodes.Nop),
                    Instruction.Create(OpCodes.Ldc_I4_0),
                    Instruction.Create(OpCodes.Ret),

                    l1 = Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Isinst, classDefinition.ReferenceFrom(classDefinition)),
                    c2 = Instruction.Create(OpCodes.Nop),
                    Instruction.Create(OpCodes.Ldc_I4_0),
                    Instruction.Create(OpCodes.Ret),

                    l2 = Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Unbox_Any, classDefinition.ReferenceFrom(classDefinition)),
                    Instruction.Create(OpCodes.Call, equalsTypeMethod.ReferenceFrom(classDefinition)));

                instructions.Replace(c1, Instruction.Create(OpCodes.Brtrue_S, l1));
                instructions.Replace(c2, Instruction.Create(OpCodes.Brtrue_S, l2));
            }
            else
            {
                instructions.AddRange(
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Isinst, classDefinition.ReferenceFrom(classDefinition)),
                    Instruction.Create(OpCodes.Call, equalsTypeMethod.ReferenceFrom(classDefinition)));
            }

            instructions.Add(Instruction.Create(OpCodes.Ret));

            return method;
        }

        [NotNull]
        private MethodDefinition CreateEqualityOperator([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition internalEqualsMethod)
        {
            var method = new MethodDefinition("op_Equality", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName, _moduleDefinition.TypeSystem.Boolean);

            var classReference = classDefinition.ReferenceFrom(classDefinition);

            method.Parameters.AddRange(
                Parameter.Create("left", classReference),
                Parameter.Create("right", classReference));

            method.Body.Instructions.AddRange(
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Call, internalEqualsMethod.ReferenceFrom(classDefinition)),
                Instruction.Create(OpCodes.Ret)
            );

            return method;
        }

        [NotNull]
        private MethodDefinition CreateInequalityOperator([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition internalEqualsMethod)
        {
            var method = new MethodDefinition("op_Inequality", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName, _moduleDefinition.TypeSystem.Boolean);

            var typeReference = classDefinition.ReferenceFrom(classDefinition);

            method.Parameters.AddRange(
                Parameter.Create("left", typeReference),
                Parameter.Create("right", typeReference));

            method.Body.Instructions.AddRange(
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Call, internalEqualsMethod.ReferenceFrom(classDefinition)),
                Instruction.Create(OpCodes.Ldc_I4_0),
                Instruction.Create(OpCodes.Ceq),
                Instruction.Create(OpCodes.Ret)
            );

            return method;
        }

        [NotNull]
        private MethodDefinition CreateGetHashCode([NotNull] TypeDefinition classDefinition, [NotNull, ItemNotNull] IEnumerable<MemberDefinition> membersToCompare, [CanBeNull] MethodDefinition customGetHashCode, [CanBeNull] MethodDefinition baseGetHashCode)
        {
            var method = new MethodDefinition("GetHashCode", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, _moduleDefinition.TypeSystem.Int32);

            var instructions = method.Body.Instructions;

            instructions.AddRange(
                Instruction.Create(OpCodes.Ldc_I4, 0),
                Instruction.Create(OpCodes.Ret));

            _postProcessActions.Add(() =>
            {
                var index = 1;

                if (baseGetHashCode != null)
                {
                    instructions.InsertRange(ref index,
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Call, baseGetHashCode.ReferenceFrom(classDefinition)),
                        Instruction.Create(OpCodes.Call, _aggregateHashCodeMethod)
                    );
                }

                if (customGetHashCode != null)
                {
                    instructions.InsertRange(ref index,
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Call, customGetHashCode.ReferenceFrom(classDefinition)),
                        Instruction.Create(OpCodes.Call, _aggregateHashCodeMethod)
                    );
                }

                foreach (var memberDefinition in membersToCompare)
                {
                    var memberType = memberDefinition.MemberType;

                    if (memberType.FullName == typeof(string).FullName)
                    {
                        var comparison = (StringComparison)memberDefinition.EqualsAttribute.ConstructorArguments.Select(a => a.Value).FirstOrDefault();
                        var comparer = _systemReferences.StringComparer;
                        var getComparerMethod = comparer.Resolve().Properties.FirstOrDefault(p => p.Name == comparison.ToString())?.GetMethod;

                        instructions.InsertRange(ref index,
                            Instruction.Create(OpCodes.Ldarg_0),
                            memberDefinition.GetValueInstruction(classDefinition),
                            Instruction.Create(OpCodes.Call, getComparerMethod.ReferenceFrom(classDefinition)),
                            Instruction.Create(OpCodes.Call, _getStringHashCode)
                        );
                    }
                    else if (memberType.IsValueType)
                    {
                        instructions.InsertRange(ref index,
                            Instruction.Create(OpCodes.Ldarg_0),
                            memberDefinition.GetValueInstruction(classDefinition));

                        if (memberType.FullName != typeof(int).FullName)
                        {
                            instructions.InsertRange(ref index,
                                Instruction.Create(OpCodes.Box, memberType),
                                Instruction.Create(OpCodes.Callvirt, _systemReferences.ObjectGetHashCode)
                            );
                        }
                    }
                    else
                    {
                        instructions.InsertRange(ref index,
                            Instruction.Create(OpCodes.Ldarg_0),
                            memberDefinition.GetValueInstruction(classDefinition),
                            Instruction.Create(OpCodes.Call, _getHashCode)
                        );
                    }

                    instructions.InsertRange(ref index,
                        Instruction.Create(OpCodes.Call, _aggregateHashCodeMethod));
                }
            });

            return method;
        }
    }
}