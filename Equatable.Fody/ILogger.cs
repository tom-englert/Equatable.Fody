namespace Equatable.Fody
{
    using Mono.Cecil.Cil;

    internal interface ILogger
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarning(string message, SequencePoint? sequencePoint = null);
        void LogError(string message, SequencePoint? sequencePoint = null);
    }
}
