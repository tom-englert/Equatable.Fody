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
        public abstract Instruction GetValueInstruction { get; }

        [NotNull]
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

            public override Instruction GetValueInstruction => Instruction.Create(OpCodes.Ldfld, _field);
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

            public override Instruction GetValueInstruction => Instruction.Create(OpCodes.Callvirt, _property.GetMethod);
        }
    }
}
