namespace MsgTruck
{
    internal class ChannelValidationResult
    {
        internal bool IsSuccess => Channel != null;
        internal Discord.IMessageChannel? Channel { get; init; }
        internal string ErrorMessage { get; init; } = "";
        internal string LoggerErrorMessage { get; init; } = "";
        internal bool UseLogger { get; init; }
        private ChannelValidationResult() { }
        internal static ChannelValidationResult FromUserFailure(string interactionErrorMessage)
            => new ChannelValidationResult()
            {
                Channel = null,
                ErrorMessage = interactionErrorMessage,
                UseLogger = false
            };
        internal static ChannelValidationResult FromSystemFailure
            (string interactionErrorMessage, string? logErrorMessage)
            => new ChannelValidationResult()
            {
                Channel = null,
                ErrorMessage = interactionErrorMessage,
                LoggerErrorMessage = string.IsNullOrEmpty(logErrorMessage)?
                    interactionErrorMessage:logErrorMessage,
                UseLogger = true
            };
        internal static ChannelValidationResult FromSuccess(Discord.IMessageChannel channel)
            => new ChannelValidationResult()
            {
                Channel = channel
            };
        internal static ChannelValidationResult FromMaybeSuccess(Discord.IMessageChannel? channel)
        {
            if (channel != null)
            {
                return FromSuccess(channel);
            }
            return FromSystemFailure(
                "Failed to retrieve the channel. We dont' know why...",
                "The channel value is Null. Might be issue in final creation/retriving."
                );
        }
    }
}
