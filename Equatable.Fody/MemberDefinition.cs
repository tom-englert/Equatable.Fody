namespace Equatable.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    internal abstract class MemberDefinition
    {
        private MemberDefinition([NotNull] ICustomAttributeProvider member)
        {
            EqualsAttribute = member.CustomAttributes.GetAttribute(AttributeNames.Equals);
        }

        [CanBeNull]
        public CustomAttribute EqualsAttribute { get; }

        [NotNull]
        public abstract TypeReference MemberType { get; }

        [NotNull]
        public abstract Instruction GetValueInstruction([NotNull] TypeReference caller);

        [NotNull]
        public virtual Instruction GetLoadArgumentInstruction(MethodDefinition method, int index)
        {
            switch (index)
            {
                case 0:
                    return Instruction.Create(OpCodes.Ldarg_0);
                case 1:
                    return Instruction.Create(OpCodes.Ldarg_1);
                default:
                    throw new InvalidOperationException("unsupported argument index");
            }
        }

        [NotNull, ItemNotNull]
        public static IEnumerable<MemberDefinition> GetMembers([NotNull] TypeDefinition classDefinition)
        {
            return classDefinition.Fields.Select(f => (MemberDefinition)new FieldMemberDefinition(f))
                .Concat(classDefinition.Properties.Select(p => new PropertyMemberDefinition(p)));
        }


        private class FieldMemberDefinition : MemberDefinition
        {
            [NotNull]
            private readonly FieldDefinition _field;

            public FieldMemberDefinition([NotNull] FieldDefinition field)
                : base(field)
            {
                _field = field;
            }

            public override TypeReference MemberType => _field.FieldType;

            public override Instruction GetValueInstruction(TypeReference caller)
            {
                return Instruction.Create(OpCodes.Ldfld, _field.ReferenceFrom(caller));
            }
        }

        private class PropertyMemberDefinition : MemberDefinition
        {
            [NotNull]
            private readonly PropertyDefinition _property;

            public PropertyMemberDefinition([NotNull] PropertyDefinition property)
                : base(property)
            {
                _property = property;
            }

            public override TypeReference MemberType => _property.PropertyType;

            public override Instruction GetValueInstruction(TypeReference caller)
            {
                var opCode = _property.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt;

                return Instruction.Create(opCode, _property.GetMethod.ReferenceFrom(caller));
            }

            [NotNull]
            public override Instruction GetLoadArgumentInstruction(MethodDefinition method, int index)
            {
                if (!_property.DeclaringType.IsValueType)
                    return base.GetLoadArgumentInstruction(method, index);

                return Instruction.Create(OpCodes.Ldarga_S, method.Parameters[index]);
            }
        }
    }
}
