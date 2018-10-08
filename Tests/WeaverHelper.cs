using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Fody;

using JetBrains.Annotations;

using Mono.Cecil;

using TomsToolbox.Core;

namespace Tests
{
    using System.IO;

    using Equatable.Fody;

    using Mono.Cecil.Cil;

    internal class WeaverHelper : DefaultAssemblyResolver
    {
        [NotNull]
        private static readonly Dictionary<string, WeaverHelper> _cache = new Dictionary<string, WeaverHelper>();

        [NotNull]
        private readonly TestResult _testResult;

        [NotNull]
        public Assembly Assembly => _testResult.Assembly;

        [NotNull, ItemNotNull]
        public IEnumerable<string> Errors => _testResult.Errors.Select(e => LogError(e.Text, e.SequencePoint));

        [NotNull, ItemNotNull]
        public IEnumerable<string> Messages => _testResult.Messages.Select(m => LogInfo(m));
        

        [NotNull]
        public static WeaverHelper Create([NotNull] string assemblyName = "AssemblyToProcess")
        {
            lock (typeof(WeaverHelper))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                return _cache.ForceValue(assemblyName, _ => new WeaverHelper(assemblyName));
            }
        }

        private WeaverHelper([NotNull] string assemblyName)
        {
            _testResult = new ModuleWeaver().ExecuteTestRun(assemblyName + ".dll", true, null, null, null, new[] { "0x80131869" });
        }

        private string LogInfo([NotNull] LogMessage message)
        {
            return message.MessageImportance + ": "+ message.Text;
        }

        private string LogError([NotNull] string message, [CanBeNull] SequencePoint sequencePoint)
        {
            if (sequencePoint != null)
            {
                message = message + $"\r\n\t({sequencePoint.Document.Url}@{sequencePoint.StartLine}:{sequencePoint.StartColumn}\r\n\t => {File.ReadAllLines(sequencePoint.Document.Url).Skip(sequencePoint.StartLine - 1).FirstOrDefault()}";
            }

            return message;
        }
    }
}
