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

        public EquatableWeaver([NotNull] ModuleWeaver moduleWeaver)
        {
            _logger = moduleWeaver;
            _moduleDefinition = moduleWeaver.ModuleDefinition;
            _systemReferences = moduleWeaver.SystemReferences;
            _aggregateHashCodeMethod = InjectAggregateHashCode(_moduleDefinition);
        }

        public void Execute()
        {
            var allTypes = _moduleDefinition.GetTypes();

            // ReSharper disable once AssignNullToNotNullAttribute
            var allClasses = allTypes
                .Where(type => type != null && type.IsClass && (type.BaseType != null));

            foreach (var classDefinition in allClasses)
            {
                var membersToCompare = MemberDefinition.GetMembers(classDefinition).Where(member => member.EqualsAttribute != null);

                var customEquals = classDefinition.Methods.FirstOrDefault(m => m.CustomAttributes.GetAttribute(AttributeNames.CustomEquals) != null);
                var customGetHashCode = classDefinition.Methods.FirstOrDefault(m => m.CustomAttributes.GetAttribute(AttributeNames.CustomGetHashCode) != null);

                if (!membersToCompare.Any() && (customEquals == null) && (customGetHashCode == null))
                    continue;

                InjectEquatable(classDefinition, membersToCompare, customEquals, customGetHashCode);
            }
        }

        [NotNull]
        private static MethodDefinition InjectAggregateHashCode([NotNull] ModuleDefinition moduleDefinition)
        {
            var typeSystem = moduleDefinition.TypeSystem;

            var type = new TypeDefinition("", "<HashCode>", TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, typeSystem.Object);
            var method = new MethodDefinition("Aggregate", MethodAttributes.Static | MethodAttributes.HideBySig, typeSystem.Int32);

            method.Parameters.AddRange(new ParameterDefinition("hash1", ParameterAttributes.None, typeSystem.Int32), new ParameterDefinition("hash2", ParameterAttributes.None, typeSystem.Int32));

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
            moduleDefinition.Types.Add(type);

            return method;
        }

        private void InjectEquatable([NotNull] TypeDefinition classDefinition, [NotNull] IEnumerable<MemberDefinition> membersToCompare, MethodDefinition customEquals, MethodDefinition customGetHashCode)
        {
            classDefinition.Interfaces.Add(new InterfaceImplementation(_systemReferences.IEquatable.MakeGenericInstanceType(classDefinition)));
            var methods = classDefinition.Methods;

            var internalEqualsMethod = CreateInternalEqualsMethod(classDefinition, membersToCompare, customEquals);
            var equalsTypeMethod = CreateEqualsTypeMethod(classDefinition, internalEqualsMethod);
            var getHashCodeMethod = CreateGetHashCode(classDefinition, membersToCompare, customGetHashCode);

            methods.Add(internalEqualsMethod);
            methods.Add(equalsTypeMethod);
            methods.Add(CreateEqualsObjectMethod(classDefinition, equalsTypeMethod));
            methods.Add(CreateEqualityOperator(classDefinition, internalEqualsMethod));
            methods.Add(CreateInequalityOperator(classDefinition, internalEqualsMethod));
            methods.Add(getHashCodeMethod);
        }

        [NotNull]
        private MethodDefinition CreateInternalEqualsMethod([NotNull] TypeDefinition classDefinition, [NotNull] IEnumerable<MemberDefinition> membersToCompare, MethodDefinition customEquals)
        {
            var method = new MethodDefinition("<InternalEquals>", MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Static, _moduleDefinition.TypeSystem.Boolean);

            method.Parameters.AddRange(
                new ParameterDefinition("left", ParameterAttributes.None, classDefinition),
                new ParameterDefinition("right", ParameterAttributes.None, classDefinition));

            var instructions = method.Body.Instructions;

            Instruction returnFalseLabel;
            var postfix = new[]
            {
                Instruction.Create(OpCodes.Ret),
                returnFalseLabel = Instruction.Create(OpCodes.Ldc_I4_0),
                Instruction.Create(OpCodes.Ret)
            };

            foreach (var memberDefinition in membersToCompare)
            {
                var loadInstruction = memberDefinition.GetValueInstruction;
                var memberType = memberDefinition.MemberType.Resolve();

                if (memberType.FullName == typeof(string).FullName)
                {
                    var comparison = (StringComparison)memberDefinition.EqualsAttribute.ConstructorArguments.Select(a => a.Value).FirstOrDefault();
                    var comparer = _systemReferences.StringComparer;
                    var getComparerMethod = comparer.Resolve().Properties.FirstOrDefault(p => p.Name == comparison.ToString())?.GetMethod;

                    instructions.AddRange(
                        Instruction.Create(OpCodes.Call, _moduleDefinition.ImportReference(getComparerMethod)),
                        Instruction.Create(OpCodes.Ldarg_0),
                        loadInstruction,
                        Instruction.Create(OpCodes.Ldarg_1),
                        loadInstruction,
                        Instruction.Create(OpCodes.Callvirt, _systemReferences.StringComparerEquals),
                        Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
                }
                else if (memberType.IsValueType)
                {
                    if (_simpleTypes.Contains(memberType.FullName))
                    {
                        instructions.AddRange(
                            Instruction.Create(OpCodes.Ldarg_0),
                            loadInstruction,
                            Instruction.Create(OpCodes.Ldarg_1),
                            loadInstruction,
                            Instruction.Create(OpCodes.Bne_Un, returnFalseLabel));
                    }
                    else if (memberType.GetEqualityOperator(out var equalityMethod))
                    {
                        instructions.AddRange(
                            Instruction.Create(OpCodes.Ldarg_0),
                            loadInstruction,
                            Instruction.Create(OpCodes.Ldarg_1),
                            loadInstruction,
                            Instruction.Create(OpCodes.Call, _moduleDefinition.ImportReference(equalityMethod)),
                            Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
                    }
                    else
                    {
                        instructions.AddRange(
                            Instruction.Create(OpCodes.Ldarg_0),
                            loadInstruction,
                            Instruction.Create(OpCodes.Box, memberType),
                            Instruction.Create(OpCodes.Ldarg_1),
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
                        instructions.AddRange(
                            Instruction.Create(OpCodes.Ldarg_0),
                            loadInstruction,
                            Instruction.Create(OpCodes.Ldarg_1),
                            loadInstruction,
                            Instruction.Create(OpCodes.Call, _moduleDefinition.ImportReference(equalityMethod)),
                            Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
                    }
                    else
                    {
                        instructions.AddRange(
                            Instruction.Create(OpCodes.Ldarg_0),
                            loadInstruction,
                            Instruction.Create(OpCodes.Ldarg_1),
                            loadInstruction,
                            Instruction.Create(OpCodes.Call, _systemReferences.ObjectEquals),
                            Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
                    }
                }
            }

            if (customEquals != null)
            {
                instructions.AddRange(
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Callvirt, customEquals),
                    Instruction.Create(OpCodes.Brfalse, returnFalseLabel));
            }

            var index = instructions.Count - 1;
            if (index >= 0)
            {
                if (instructions[index].OpCode == OpCodes.Bne_Un)
                {
                    instructions[index] = Instruction.Create(OpCodes.Ceq);
                }
                else
                {
                    instructions.RemoveAt(index);
                }
            }
            else
            {
                instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));
            }

            instructions.AddRange(postfix);

            return method;
        }

        [NotNull]
        private MethodDefinition CreateEqualsTypeMethod([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition internalEqualsMethod)
        {
            var method = new MethodDefinition("Equals", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final, _moduleDefinition.TypeSystem.Boolean);

            method.Parameters.Add(new ParameterDefinition("other", ParameterAttributes.None, classDefinition));
            method.IsFinal = true;

            var instructions = method.Body.Instructions;

            if (classDefinition.IsValueType)
            {
                instructions.AddRange(
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldobj, classDefinition),
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Call, internalEqualsMethod));
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
                    Instruction.Create(OpCodes.Call, internalEqualsMethod));

                instructions.Replace(c1, Instruction.Create(OpCodes.Brtrue_S, l1));
                instructions.Replace(c2, Instruction.Create(OpCodes.Bne_Un_S, l2));
            }

            instructions.Add(Instruction.Create(OpCodes.Ret));

            return method;
        }

        [NotNull]
        private MethodDefinition CreateEqualsObjectMethod([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition equalsTypeMethod)
        {
            var method = new MethodDefinition("Equals", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, _moduleDefinition.TypeSystem.Boolean);

            method.Parameters.Add(new ParameterDefinition("obj", ParameterAttributes.None, _moduleDefinition.TypeSystem.Object));

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
                    Instruction.Create(OpCodes.Isinst, classDefinition),
                    c2 = Instruction.Create(OpCodes.Nop),
                    Instruction.Create(OpCodes.Ldc_I4_0),
                    Instruction.Create(OpCodes.Ret),

                    l2 = Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Unbox_Any, classDefinition),
                    Instruction.Create(OpCodes.Call, equalsTypeMethod));

                instructions.Replace(c1, Instruction.Create(OpCodes.Brtrue_S, l1));
                instructions.Replace(c2, Instruction.Create(OpCodes.Brtrue_S, l2));
            }
            else
            {
                instructions.AddRange(
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldarg_1),
                    Instruction.Create(OpCodes.Isinst, classDefinition),
                    Instruction.Create(OpCodes.Call, equalsTypeMethod));
            }

            instructions.Add(Instruction.Create(OpCodes.Ret));

            return method;
        }

        [NotNull]
        private MethodDefinition CreateEqualityOperator([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition internalEqualsMethod)
        {
            var method = new MethodDefinition("op_Equality", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName, _moduleDefinition.TypeSystem.Boolean);

            method.Parameters.AddRange(
                new ParameterDefinition("left", ParameterAttributes.None, classDefinition),
                new ParameterDefinition("right", ParameterAttributes.None, classDefinition));

            method.Body.Instructions.AddRange(
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Call, internalEqualsMethod),
                Instruction.Create(OpCodes.Ret)
            );

            return method;
        }

        [NotNull]
        private MethodDefinition CreateInequalityOperator([NotNull] TypeDefinition classDefinition, [NotNull] MethodDefinition internalEqualsMethod)
        {
            var method = new MethodDefinition("op_Inequality", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static | MethodAttributes.SpecialName, _moduleDefinition.TypeSystem.Boolean);

            method.Parameters.AddRange(
                new ParameterDefinition("left", ParameterAttributes.None, classDefinition),
                new ParameterDefinition("right", ParameterAttributes.None, classDefinition));

            method.Body.Instructions.AddRange(
                Instruction.Create(OpCodes.Ldarg_0),
                Instruction.Create(OpCodes.Ldarg_1),
                Instruction.Create(OpCodes.Call, internalEqualsMethod),
                Instruction.Create(OpCodes.Ldc_I4_0),
                Instruction.Create(OpCodes.Ceq),
                Instruction.Create(OpCodes.Ret)
            );

            return method;
        }

        [NotNull]
        private MethodDefinition CreateGetHashCode([NotNull] TypeDefinition classDefinition, [NotNull, ItemNotNull] IEnumerable<MemberDefinition> membersToCompare, [CanBeNull] MethodDefinition customGetHashCode)
        {
            var method = new MethodDefinition("GetHashCode", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual, _moduleDefinition.TypeSystem.Int32);

            var instructions = method.Body.Instructions;

            instructions.Add(Instruction.Create(OpCodes.Ldc_I4, 397));

            foreach (var memberDefinition in membersToCompare)
            {
                var memberType = memberDefinition.MemberType;

                if (memberType.FullName == typeof(string).FullName)
                {
                    var comparison = (StringComparison)memberDefinition.EqualsAttribute.ConstructorArguments.Select(a => a.Value).FirstOrDefault();
                    var comparer = _systemReferences.StringComparer;
                    var getComparerMethod = comparer.Resolve().Properties.FirstOrDefault(p => p.Name == comparison.ToString())?.GetMethod;

                    instructions.AddRange(
                        Instruction.Create(OpCodes.Call, _moduleDefinition.ImportReference(getComparerMethod)),
                        Instruction.Create(OpCodes.Ldarg_0),
                        memberDefinition.GetValueInstruction,
                        Instruction.Create(OpCodes.Callvirt, _systemReferences.StringComparerGetHashCode)
                        );
                }
                else if (memberType.IsValueType)
                {
                    instructions.AddRange(
                        Instruction.Create(OpCodes.Ldarg_0),
                        memberDefinition.GetValueInstruction);

                    if (memberType.FullName != typeof(int).FullName)
                    {
                        instructions.AddRange(
                            Instruction.Create(OpCodes.Box, memberType),
                            Instruction.Create(OpCodes.Callvirt, _systemReferences.ObjectGetHashCode)
                        );
                    }
                }
                else
                {
                    instructions.AddRange(
                        Instruction.Create(OpCodes.Ldarg_0),
                        memberDefinition.GetValueInstruction,
                        Instruction.Create(OpCodes.Callvirt, _systemReferences.ObjectGetHashCode)
                    );
                }

                instructions.Add(Instruction.Create(OpCodes.Call, _aggregateHashCodeMethod));
            }

            if (customGetHashCode != null)
            {
                instructions.AddRange(
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Call, customGetHashCode),
                    Instruction.Create(OpCodes.Call, _aggregateHashCodeMethod)
                );
            }

            instructions.Add(Instruction.Create(OpCodes.Ret));

            return method;
        }
    }
}