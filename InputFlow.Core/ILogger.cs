namespace InputFlow.Core
{
    /// <summary>
    /// Basic logging interface used by InputFlow to record important events.
    /// Implementations should avoid logging sensitive user input.
    /// </summary>
    public interface ILogger
    {
        void Info(string message);
        void Warning(string message);
        void Error(string message);
    }
}