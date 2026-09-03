namespace NukeShare.Core.Logger
{
    public interface ILogger
    {
        void LogWarning(string message);
        void LogError(string message);
        void LogDebug(string message);
    }

    public class Logger: ILogger
    {
        public void LogWarning(string message) { }
        public  void LogError(string message) { }
        public void LogDebug(string message) { }
    }
}
