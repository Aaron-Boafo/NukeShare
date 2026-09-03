namespace NukeShare.Core.Logger
{
    public interface ILogger
    {
        void LogWarning(string message);
        void LogError(string message);
        void LogDebug(string message);
    }

    public class Logger : ILogger
    {
        public void LogWarning(string message)
        {
            WriteLine("WARN", message, ConsoleColor.Yellow);
        }

        public void LogError(string message)
        {
            WriteLine("ERROR", message, ConsoleColor.Red);
        }

        public void LogDebug(string message)
        {
            WriteLine("DEBUG", message, ConsoleColor.Gray);
        }

        private static void WriteLine(string level, string message, ConsoleColor color)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
            Console.ForegroundColor = originalColor;
        }
    }
}
