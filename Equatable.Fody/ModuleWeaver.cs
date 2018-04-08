namespace Equatable.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using global::Fody;

    using JetBrains.Annotations;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class ModuleWeaver : BaseModuleWeaver, ILogger
    {
        // Will log an informational message to MSBuild
        [NotNull]
        internal SystemReferences SystemReferences => new SystemReferences(ModuleDefinition, ModuleDefinition.AssemblyResolver);

        public ModuleWeaver()
        {
            CosturaUtility.Initialize();
        }

        public override void Execute()
        {
            new EquatableWeaver(this).Execute();
        }

        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield break;
        }

        public override bool ShouldCleanReference => true;

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
