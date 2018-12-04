namespace Malimbe.FodyRunner
{
    using System;
    using System.Linq;
    using System.Xml.Linq;

    internal sealed class LogForwarder : global::ILogger
    {
        private static readonly string _configurationElementName = typeof(Runner).Namespace;
        private static readonly string[] _configurationElementSplitSeparators =
        {
            ",", " ", "\t", "\n", "\r", "\r\n"
        };

        private readonly ILogger _logger;
        private string _currentWeaverName;
        private LogLevel _logLevel = LogLevel.None;

        public LogForwarder(ILogger logger) =>
            _logger = logger;

        public void SetLogLevelFromConfiguration(XDocument configurationDocument) =>
            _logLevel = configurationDocument.Root?.Elements(_configurationElementName)
                    .Elements(nameof(LogLevel))
                    .Select(element => element.Value)
                    .SelectMany(
                        value => value.Split(
                            _configurationElementSplitSeparators,
                            StringSplitOptions.RemoveEmptyEntries))
                    .Select(value => value.Trim())
                    .Select(value => Enum.TryParse(value, true, out LogLevel level) ? level : LogLevel.None)
                    .Aggregate((level1, level2) => level1 | level2)
                ?? LogLevel.None;

        public void SetCurrentWeaverName(string weaverName) =>
            _currentWeaverName = weaverName;

        public void ClearWeaverName() =>
            _currentWeaverName = null;

        public void LogDebug(string message)
        {
            if (_logLevel.HasFlag(LogLevel.Debug))
            {
                _logger.Log(LogLevel.Debug, PrefixMessageWithCurrentWeaverName(message));
            }
        }

        public void LogInfo(string message)
        {
            if (_logLevel.HasFlag(LogLevel.Info))
            {
                _logger.Log(LogLevel.Info, PrefixMessageWithCurrentWeaverName(message));
            }
        }

        public void LogMessage(string message, int level)
        {
            if (_logLevel.HasFlag(LogLevel.Info))
            {
                _logger.Log(LogLevel.Info, PrefixMessageWithCurrentWeaverName(message));
            }
        }

        public void LogWarning(string message)
        {
            if (_logLevel.HasFlag(LogLevel.Warning))
            {
                _logger.Log(LogLevel.Warning, PrefixMessageWithCurrentWeaverName(message));
            }
        }

        public void LogError(string message)
        {
            if (_logLevel.HasFlag(LogLevel.Error))
            {
                _logger.Log(LogLevel.Error, PrefixMessageWithCurrentWeaverName(message));
            }
        }

        public void LogWarning(
            string message,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber) =>
            LogWarning(message);

        public void LogError(
            string message,
            string file,
            int lineNumber,
            int columnNumber,
            int endLineNumber,
            int endColumnNumber) =>
            LogError(message);

        private string PrefixMessageWithCurrentWeaverName(string message) =>
            _currentWeaverName == null ? message : $"{_currentWeaverName}: {message}";
    }
}
