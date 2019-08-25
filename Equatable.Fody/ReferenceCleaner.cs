namespace Equatable.Fody
{
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    using Mono.Cecil;

    internal class ReferenceCleaner
    {
        [NotNull, ItemNotNull]
        private static readonly HashSet<string> _attributesToRemove = new HashSet<string>
        {
            AttributeNames.ImplementsEquatable,
            AttributeNames.Equals,
            AttributeNames.CustomEquals,
            AttributeNames.CustomGetHashCode
        };

        [NotNull]
        private readonly ModuleDefinition _moduleDefinition;
        [NotNull]
        private readonly ILogger _logger;

        public ReferenceCleaner([NotNull] ModuleDefinition moduleDefinition, [NotNull] ILogger logger)
        {
            _logger = logger;
            _moduleDefinition = moduleDefinition;
        }

        public void RemoveAttributes()
        {
            foreach (var type in _moduleDefinition.GetTypes())
            {
                ProcessType(type);
            }

            RemoveAttributes(_moduleDefinition.CustomAttributes);
            RemoveAttributes(_moduleDefinition.Assembly.CustomAttributes);
        }

        private void ProcessType([NotNull] TypeDefinition type)
        {
            RemoveAttributes(type.CustomAttributes);

            foreach (var property in type.Properties)
            {
                RemoveAttributes(property.CustomAttributes);
            }

            foreach (var field in type.Fields)
            {
                RemoveAttributes(field.CustomAttributes);
            }

            foreach (var method in type.Methods)
            {
                RemoveAttributes(method.CustomAttributes);
            }
        }

        private void RemoveAttributes([NotNull, ItemNotNull] ICollection<CustomAttribute> customAttributes)
        {
            var attributesToRemove = customAttributes
                .Where(attribute => _attributesToRemove.Contains(attribute.Constructor.DeclaringType.FullName))
                .ToArray();

            foreach (var customAttribute in attributesToRemove.ToList())
            {
                customAttributes.Remove(customAttribute);
            }
        }
    }
}