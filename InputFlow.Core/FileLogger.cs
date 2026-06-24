using System;
using System.IO;

namespace InputFlow.Core
{
    /// <summary>
    /// Logs messages to a text file with timestamps.
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _path;
        private readonly object _lock = new();

        public FileLogger(string logFilePath)
        {
            _path = logFilePath;
        }

        public void Info(string message) => Write("INFO", message);
        public void Warning(string message) => Write("WARN", message);
        public void Error(string message) => Write("ERR", message);

        private void Write(string level, string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            lock (_lock)
            {
                File.AppendAllText(_path, line + Environment.NewLine);
            }
        }
    }
}