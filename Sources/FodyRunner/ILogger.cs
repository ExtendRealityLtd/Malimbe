namespace Malimbe.FodyRunner
{
    public interface ILogger
    {
        void Log(LogLevel level, string message);
    }
}
