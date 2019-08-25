namespace Equatable.Fody
{
    using System.Collections.Generic;

    using global::Fody;

    using JetBrains.Annotations;

    using Mono.Cecil.Cil;

    public class ModuleWeaver : BaseModuleWeaver, ILogger
    {
        // Will log an informational message to MSBuild
        [NotNull]
        internal SystemReferences SystemReferences => new SystemReferences(TypeSystem, ModuleDefinition, ModuleDefinition.AssemblyResolver);

        public override void Execute()
        {
            new EquatableWeaver(this).Execute();
            CleanReferences();
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield break;
        }

        public override bool ShouldCleanReference => true;

        private void CleanReferences()
        {
            new ReferenceCleaner(ModuleDefinition, this).RemoveAttributes();
        }

        void ILogger.LogDebug(string message)
        {
            LogDebug(message);
        }

        void ILogger.LogInfo(string message)
        {
            LogInfo(message);
        }

        void ILogger.LogWarning(string message, SequencePoint sequencePoint)
        {
            if (sequencePoint != null)
            {
                LogWarningPoint(message, sequencePoint);
            }
            else
            {
                LogWarning(message);
            }
        }

        void ILogger.LogError(string message, SequencePoint sequencePoint)
        {
            if (sequencePoint != null)
            {
                LogErrorPoint(message, sequencePoint);
            }
            else
            {
                LogError(message);
            }
        }
    }
}
