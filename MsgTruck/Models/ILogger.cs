namespace MsgTruck
{
    public interface ILogger
    {
        Task LogAsync(string v, Discord.LogSeverity info);
    }
}