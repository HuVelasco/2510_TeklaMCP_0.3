using System;
using System.Text;

namespace TeklaDrawingExtractor.Utilities
{
    /// <summary>
    /// Logger for extraction progress and errors
    /// </summary>
    public class Logger
    {
        private StringBuilder _log = new StringBuilder();
        private Action<string> _outputHandler;

        public Logger(Action<string> outputHandler = null)
        {
            _outputHandler = outputHandler;
        }

        public void Info(string message)
        {
            LogMessage("INFO", message);
        }

        public void Success(string message)
        {
            LogMessage("✓", message);
        }

        public void Warning(string message)
        {
            LogMessage("⚠", message);
        }

        public void Error(string message)
        {
            LogMessage("❌", message);
        }

        public void Error(string message, Exception ex)
        {
            LogMessage("❌", message + ": " + ex.Message);
        }

        private void LogMessage(string level, string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string logLine = $"[{timestamp}] {level} {message}";

            _log.AppendLine(logLine);
            _outputHandler?.Invoke(logLine);
        }

        public string GetFullLog()
        {
            return _log.ToString();
        }

        public void Clear()
        {
            _log.Clear();
        }
    }
}
