namespace Equatable.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    internal abstract class MemberDefinition
    {
        private MemberDefinition(ICustomAttributeProvider member)
        {
            EqualsAttribute = member.CustomAttributes.GetAttribute(AttributeNames.Equals);
        }

        public CustomAttribute? EqualsAttribute { get; }

        public abstract TypeReference MemberType { get; }

        public abstract Instruction GetValueInstruction(TypeReference caller);

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

        public static IEnumerable<MemberDefinition> GetMembers(TypeDefinition classDefinition)
        {
            return classDefinition.Fields.Select(f => (MemberDefinition)new FieldMemberDefinition(f))
                .Concat(classDefinition.Properties.Select(p => new PropertyMemberDefinition(p)));
        }

        private class FieldMemberDefinition : MemberDefinition
        {
                private readonly FieldDefinition _field;

            public FieldMemberDefinition(FieldDefinition field)
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
                private readonly PropertyDefinition _property;

            public PropertyMemberDefinition(PropertyDefinition property)
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

                public override Instruction GetLoadArgumentInstruction(MethodDefinition method, int index)
            {
                if (!_property.DeclaringType.IsValueType)
                    return base.GetLoadArgumentInstruction(method, index);

                return Instruction.Create(OpCodes.Ldarga_S, method.Parameters[index]);
            }
        }
    }
}
