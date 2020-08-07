using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Equatable.Fody;

using Fody;

using Mono.Cecil;

using TomsToolbox.Essentials;

namespace Tests
{
    internal class WeaverHelper : DefaultAssemblyResolver
    {
        private static readonly Dictionary<string, WeaverHelper> _cache = new Dictionary<string, WeaverHelper>();

        private readonly TestResult _testResult;

        public Assembly Assembly => _testResult.Assembly;

        public IEnumerable<string> Errors => _testResult.Errors.Select(LogError);

        public IEnumerable<string> Messages => _testResult.Messages.Select(LogInfo);

        public IEnumerable<string> Warnings => _testResult.Warnings.Select(LogError);
        
        public static WeaverHelper Create(string assemblyName = "AssemblyToProcess")
        {
            lock (typeof(WeaverHelper))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                return _cache.ForceValue(assemblyName, _ => new WeaverHelper(assemblyName));
            }
        }

        private WeaverHelper(string assemblyName)
        {
            _testResult = new ModuleWeaver().ExecuteTestRun(assemblyName + ".dll", true, null, null, null, new[] { "0x80131869" });
        }

        private string LogInfo(LogMessage message)
        {
            return message.MessageImportance + ": "+ message.Text;
        }

        private string LogError(SequencePointMessage e)
        {
            var message = e.Text;
            var sequencePoint = e.SequencePoint;

            if (sequencePoint != null)
            {
                message = message + $"\r\n\t({sequencePoint.Document.Url}@{sequencePoint.StartLine}:{sequencePoint.StartColumn}\r\n\t => {File.ReadAllLines(sequencePoint.Document.Url).Skip(sequencePoint.StartLine - 1).FirstOrDefault()}";
            }

            return message;
        }
    }
}
