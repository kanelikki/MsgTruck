using Discord;

namespace MsgTruck.Entry
{
    /// <summary>
    /// Console implementation of logging.
    /// </summary>
    internal class ConsoleLogger : ILogger
    {
        public async Task LogAsync(string detail, LogSeverity severity)
        {
            Console.ForegroundColor = GetColor(severity);
            await Console.Out.WriteLineAsync($"[{DateTime.Now}] {detail}");
        }
        private ConsoleColor GetColor(LogSeverity severity) => severity switch
        {
            LogSeverity.Debug => ConsoleColor.Green,
            LogSeverity.Info => ConsoleColor.Cyan,
            LogSeverity.Warning => ConsoleColor.Yellow,
            LogSeverity.Error => ConsoleColor.Red,
            LogSeverity.Critical => ConsoleColor.Magenta,
            LogSeverity.Verbose => ConsoleColor.Gray,
            _ => ConsoleColor.White
        };
    }
}
