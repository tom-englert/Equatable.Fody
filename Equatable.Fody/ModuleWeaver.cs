namespace Equatable.Fody
{
    using System.Collections.Generic;

    using global::Fody;

    using Mono.Cecil.Cil;

    public class ModuleWeaver : BaseModuleWeaver, ILogger
    {
        internal SystemReferences SystemReferences => new SystemReferences(TypeSystem, ModuleDefinition, ModuleDefinition.AssemblyResolver);

        public override void Execute()
        {
            // System.Diagnostics.Debugger.Launch();

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
            new ReferenceCleaner(ModuleDefinition).RemoveAttributes();
        }

        void ILogger.LogDebug(string message)
        {
            WriteDebug(message);
        }

        void ILogger.LogInfo(string message)
        {
            WriteInfo(message);
        }

        void ILogger.LogWarning(string message, SequencePoint? sequencePoint)
        {
            WriteWarning(message, sequencePoint);
        }

        void ILogger.LogError(string message, SequencePoint? sequencePoint)
        {
            WriteError(message, sequencePoint);
        }
    }
}
