namespace Equatable.Fody
{
    using System;
    using System.Linq;
    using System.Xml.Linq;

    using JetBrains.Annotations;

    using Mono.Cecil;
    using Mono.Cecil.Cil;

    public class ModuleWeaver : ILogger
    {
        // Will log an informational message to MSBuild
        [NotNull]
        public Action<string> LogDebug { get; set; }

        [NotNull]
        public Action<string> LogInfo { get; set; }

        [NotNull]
        public Action<string> LogWarning { get; set; }

        [NotNull]
        public Action<string> LogError { get; set; }

        [NotNull]
        public Action<string, SequencePoint> LogWarningPoint { get; set; }

        [NotNull]
        public Action<string, SequencePoint> LogErrorPoint { get; set; }

        // An instance of Mono.Cecil.ModuleDefinition for processing
        [NotNull]
        public ModuleDefinition ModuleDefinition { get; set; }

        [NotNull]
        public IAssemblyResolver AssemblyResolver { get; set; }

        [NotNull]
        internal SystemReferences SystemReferences => new SystemReferences(ModuleDefinition, AssemblyResolver);

        public ModuleWeaver()
        {
            CosturaUtility.Initialize();

            LogDebug = LogInfo = LogWarning = LogError = _ => { };
            LogErrorPoint = LogWarningPoint = (_, __) => { };
            ModuleDefinition = ModuleDefinition.CreateModule("empty", ModuleKind.Dll);
            AssemblyResolver = new DefaultAssemblyResolver();
        }

        public void Execute()
        {
            new EquatableWeaver(this).Execute();
            CleanReferences();
        }

        private void CleanReferences()
        {
            var referenceCleaner = new ReferenceCleaner(ModuleDefinition, this);
            referenceCleaner.RemoveAttributes();
            referenceCleaner.RemoveReferences();
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
