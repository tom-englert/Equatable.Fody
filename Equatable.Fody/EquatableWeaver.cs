namespace Equatable.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    using TypeSystem = global::Fody.TypeSystem;

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
        [NotNull]
        private readonly global::Fody.TypeSystem _typeSystem;

        public EquatableWeaver([NotNull] ModuleWeaver moduleWeaver)
        {
            _logger = moduleWeaver;
            _typeSystem = moduleWeaver.TypeSystem;
            _moduleDefinition = moduleWeaver.ModuleDefinition;
            _systemReferences = moduleWeaver.SystemReferences;
            var hashCodeMethod = InjectStaticHashCodeClass(_moduleDefinition);
            _aggregateHashCodeMethod = InjectAggregateMethod(hashCodeMethod);
            _getHashCode = InjectGetHashCodeMethod(hashCodeMethod, _systemReferences);
            _getStringHashCode = InjectGetStringHashCodeMethod(hashCodeMethod, _systemReferences);
        }

        public void Execute()
        {
            var allTypes = _moduleDefinition.GetTypes();

            var allClasses = allTypes
                .Where(type => type != null && type.IsClass && (type.BaseType != null));

            foreach (var classDefinition in allClasses)
            {
                var membersToCompare = MemberDefinition.GetMembers(classDefinition)
                    .Where(member => member.EqualsAttribute != null)
                    .ToArray();

                var classHasImplementEqualityAttribute = classDefinition.CustomAttributes.GetAttribute(AttributeNames.ImplementsEquatable) != null;
                var customEquals = classDefinition.Methods.FirstOrDefault(m => m.CustomAttributes.GetAttribute(AttributeNames.CustomEquals) != null);
                var customGetHashCode = classDefinition.Methods.FirstOrDefault(m => m.CustomAttributes.GetAttribute(AttributeNames.CustomGetHashCode) != null);

                if (!membersToCompare.Any() && (customEquals == null) && (customGetHashCode == null))
                {
                    if (classHasImplementEqualityAttribute)
                    {
                        _logger.LogError($"Class {classDefinition} has the {AttributeNames.ImplementsEquatable} attribute, but no member is marked with a member attribute.", classDefinition.GetEntryPoint());
                    }
                    continue;
                }

                if (!classHasImplementEqualityAttribute)
                {
                    _logger.LogWarning($"Class {classDefinition} has members marked with equality attributes, but the class has not {AttributeNames.ImplementsEquatable} attribute. It's recommended to add the {AttributeNames.ImplementsEquatable}.", classDefinition.GetEntryPoint());
                }

                try
                {
                    InjectEquatable(classDefinition, membersToCompare, customEquals, customGetHashCode);
                }
                catch (WeavingException ex)
                {
                    _logger.LogError(ex.Message, ex.SequencePoint);
                }
            }

            foreach (var action in _postProcessActions)
            {
                action();
            }
        }

        [NotNull]
        private TypeDefinition InjectStaticHashCodeClass([NotNull] ModuleDefinition moduleDefinition)
        {
            var type = new TypeDefinition("", "<HashCode>", TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, _typeSystem.ObjectReference);

            type.MarkAsCompilerGenerated(_systemReferences);

            moduleDefinition.Types.Add(type);

            return type;
        }

        [NotNull]
        private MethodDefinition InjectAggregateMethod([NotNull] TypeDefinition type)
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

            var method = new MethodDefinition("Aggregate", MethodAttributes.Static | MethodAttributes.HideBySig, _typeSystem.Int32Reference);

            method.Parameters.AddRange(Parameter.Create("hash1", _typeSystem.Int32Reference), Parameter.Create("hash2", _typeSystem.Int32Reference));

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

            method.Body.Optimize();

            type.Methods.Add(method);

            return method;
        }

        [NotNull]
        private MethodDefinition InjectGetHashCodeMethod([NotNull] TypeDefinition type, [NotNull] SystemReferences systemReferences)
        {
            var method = new MethodDefinition("GetHashCode", MethodAttributes.Static | MethodAttributes.HideBySig, _typeSystem.Int32Reference);

            method.Parameters.AddRange(Parameter.Create("value", _typeSystem.ObjectReference));

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

            method.Body.Optimize();

            type.Methods.Add(method);

            return method;
        }

        [NotNull]
        private MethodDefinition InjectGetStringHashCodeMethod([NotNull] TypeDefinition type, [NotNull] SystemReferences systemReferences)
        {
            var method = new MethodDefinition("GetStringHashCode", MethodAttributes.Static | MethodAttributes.HideBySig, _typeSystem.Int32Reference);

            method.Parameters.AddRange(
                Parameter.Create("value", _typeSystem.StringReference),
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

            method.Body.Optimize();

            type.Methods.Add(method);

            return method;
        }

        private void InjectEquatable([NotNull] TypeDefinition classDefinition, [NotNull, ItemNotNull] ICollection<MemberDefinition> membersToCompare, [CanBeNull] MethodDefinition customEquals, [CanBeNull] MethodDefinition customGetHashCode)
        {
            _logger.LogInfo($"Weaving IEquatable into {classDefinition}");

            VerifyCustomEqualsSignature(classDefinition, customEquals);
            VerifyCustomGetHashCodeSignature(classDefinition, customGetHashCode);

            if (classDefinition.Interfaces.Any(i => i.InterfaceType.Resolve().FullName == typeof(IEquatable<>).FullName))
                throw new WeavingException($"Class {classDefinition} already implements {typeof(IEquatable<>)}", classDefinition.GetEntryPoint());

            classDefinition.Interfaces.Add(new InterfaceImplementation(_systemReferences.IEquatable.MakeGenericInstanceType(classDefinition.ReferenceFrom(classDefinition))));

            var internalEqualsMethod = CreateInternalEqualsMethod(classDefinition, membersToCompare, customEquals);
            classDefinition.AddMethod(internalEqualsMethod);

            var equalsTypeMethod = CreateTypedEqualsMethod(classDefinition, internalEqualsMethod);
            classDefinition.AddMethod(equalsTypeMethod);

            var getHashCodeMethod = CreateGetHashCode(classDefinition, membersToCompare, customGetHashCode);
            classDefinition.AddMethod(getHashCodeMethod);

            classDefinition.AddMethod(CreateObjectEqualsOverrideMethod(classDefinition, equalsTypeMethod));
            classDefinition.AddMethod(CreateEqualityOperator(classDefinition, internalEqualsMethod));
            classDefinition.AddMethod(CreateInequalityOperator(classDefinition, internalEqualsMethod));
        }

        private void VerifyCustomGetHashCodeSignature([NotNull] TypeDefinition classDefinition, [CanBeNull] MethodDefinition customGetHashCode)
        {
            if (customGetHashCode != null)
            {
                if (customGetHashCode.ReturnType.FullName != typeof(int).FullName)
                    throw new WeavingException($"Custom get hash code method in class {classDefinition} must have a return type of {typeof(int)}!", customGetHashCode.GetEntryPoint());

                if ((customGetHashCode.Parameters.Count != 0))
                    throw new WeavingException($"Custom get hash code method in class {classDefinition} must have no parameters!", customGetHashCode.GetEntryPoint());

                if (customGetHashCode.IsAbstract || customGetHashCode.IsStatic)
                    throw new WeavingException($"Custom get hash code method in class {classDefinition} must not be abstract!", customGetHashCode.GetEntryPoint());
            }
        }

        private void VerifyCustomEqualsSignature([NotNull] TypeDefinition classDefinition, [CanBeNull] MethodDefinition customEquals)
        {
            if (customEquals != null)
            {
                if (customEquals.ReturnType.FullName != typeof(bool).FullName)
                    throw new WeavingException($"Custom equals method in class {classDefinition} must have a return type of {typeof(bool)}!", customEquals.GetEntryPoint());

                if ((customEquals.Parameters.Count != 1) || (customEquals.Parameters[0].ParameterType.Resolve() != classDefinition))
                    throw new WeavingException($"Custom equals method in class {classDefinition} must have one parameter of type {classDefinition}!", customEquals.GetEntryPoint());

                if (customEquals.IsAbstract || customEquals.IsStatic)
                    throw new WeavingException($"Custom equals method in class {classDefinition} must not be a non-abstract member method!", customEquals.GetEntryPoint());
            }
        }

        private struct EqualityMethods
        {
            public MethodDefinition EqualsMethod { get; set; }
            public MethodDefinition GetHashCodeMethod { get; set; }
        }

        private EqualityMethods GetBaseEqualsAndHashCode([NotNull] TypeDefinition classDefinition)
        {
            var baseType = classDefinition.BaseType.Resolve();

            if ((baseType.FullName == typeof(object).FullName) || (baseType.FullName == typeof(ValueType).FullName))
                return default;

            var baseEquals = baseType.TryFindMethod("Equals", baseType);
            var baseGetHashCode = baseType.TryFindMethod("GetHashCode");

            if (baseEquals == null)
            {
                if (baseGetHashCode != null)
                {
                    _logger.LogWarning($"{baseType} overrides GetHashCode, but does not override Equals!");
                    return default;
                }
            }
            else if (baseGetHashCode == null)
            {
                _logger.LogWarning($"{baseType} overrides Equals, but does not override GetHashCode!");
                return default;
            }

            return new EqualityMethods { EqualsMethod = baseEquals, GetHashCodeMethod = baseGetHashCode };
        }

        [NotNull]
        private MethodDefinition CreateInternalEqualsMethod([NotNull] TypeDefinition classDefinition, [NotNull, ItemNotNull] IEnumerable<MemberDefinition> membersToCompare, [CanBeNull] MethodDefinition customEqualsMethod)
        {
            var method = new MethodDefinition("<InternalEquals>", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, _typeSystem.BooleanReference);
            method.MarkAsCompilerGenerated(_systemReferences);

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

                if (!classDefinition.IsValueType)
                {
                    Instruction c1, l1;

                    instructions.InsertRange(ref index,
                        Instruction.Create(OpCodes.Ldarg_0),
                        c1 = Instruction.Create(OpCodes.Nop),
                        Instruction.Create(OpCodes.Ldarg_1),
                        Instruction.Create(OpCodes.Ldnull),
                        Instruction.Create(OpCodes.Ceq),
                        Instruction.Create(OpCodes.Ret),

                        l1 = Instruction.Create(OpCodes.Ldarg_1),
                        Instruction.Create(OpCodes.Brfalse, returnFalseLabel));

                    instructions.Replace(c1, Instruction.Create(OpCodes.Brtrue, l1));
                }

                var baseEqualsMethod = GetBaseEqualsAndHashCode(classDefinition).EqualsMethod;
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

                method.Body.Optimize();
            });

            return method;
        }

        [NotNull]
        private MethodDefinition CreateTypedEqualsMethod([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition internalEqualsMethod)
        {
            var method = new MethodDefinition("Equals", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final, _typeSystem.BooleanReference);
            method.MarkAsCompilerGenerated(_systemReferences);

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
                instructions.AddRange(
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Call, internalEqualsMethod.ReferenceFrom(classDefinition)));
            }

            instructions.Add(Instruction.Create(OpCodes.Ret));

            method.Body.Optimize();

            return method;
        }

        [NotNull]
        private MethodDefinition CreateObjectEqualsOverrideMethod([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition equalsTypeMethod)
        {
            var method = new MethodDefinition("Equals", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, _typeSystem.BooleanReference);
            method.MarkAsCompilerGenerated(_systemReferences);

            method.Parameters.Add(Parameter.Create("obj", _typeSystem.ObjectReference));

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

            method.Body.Optimize();

            return method;
        }

        [NotNull]
        private MethodDefinition CreateEqualityOperator([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition internalEqualsMethod)
        {
            var method = new MethodDefinition("op_Equality", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName, _typeSystem.BooleanReference);
            method.MarkAsCompilerGenerated(_systemReferences);

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

            method.Body.Optimize();

            return method;
        }

        [NotNull]
        private MethodDefinition CreateInequalityOperator([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition internalEqualsMethod)
        {
            var method = new MethodDefinition("op_Inequality", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName, _typeSystem.BooleanReference);
            method.MarkAsCompilerGenerated(_systemReferences);

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

            method.Body.Optimize();

            return method;
        }

        [NotNull]
        private MethodDefinition CreateGetHashCode([NotNull] TypeDefinition classDefinition, [NotNull, ItemNotNull] IEnumerable<MemberDefinition> membersToCompare, [CanBeNull] MethodDefinition customGetHashCode)
        {
            var method = new MethodDefinition("GetHashCode", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, _typeSystem.Int32Reference);
            method.MarkAsCompilerGenerated(_systemReferences);

            var instructions = method.Body.Instructions;

            instructions.AddRange(
                Instruction.Create(OpCodes.Ldc_I4, 0),
                Instruction.Create(OpCodes.Ret));

            _postProcessActions.Add(() =>
            {
                var index = 1;

                var baseGetHashCode = GetBaseEqualsAndHashCode(classDefinition).GetHashCodeMethod;
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


                method.Body.Optimize();
            });

            return method;
        }
    }
}