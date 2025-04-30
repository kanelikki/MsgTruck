using Discord;

namespace MsgTruck
{
    /// <summary>
    /// Handles message moving process
    /// </summary>
    /// <remarks>This instance is created PER COMMAND.</remarks>
    internal class MessageMover
    {
        private const int _chunk = 100;
        private readonly ILogger _logger;
        private readonly RateLimitManager _rateLimitManager = new();
        private IIntegrationChannel _channel;
        private static readonly List<ulong> _runningList = new();
        public DateTimeOffset LastMessageTimeStamp { get; private set; }
        private readonly WebhookMessageSender _webhookMessageSender;

        private MessageMover(IIntegrationChannel channel, ILogger logger, IMessageCopySender sender)
        {
            _logger = logger;
            _channel = channel;
            _webhookMessageSender = new(channel.Guild, channel, sender);
        }
        internal static MessageMover? GetMessageMover
            (IGuild guild, IChannel channel, ChannelManager channelManager, ILogger logger)
        {
            if (ChannelManager.IsThread(channel))
            {
                var asThread = channel as Discord.WebSocket.SocketThreadChannel;
                var parentChannel = asThread?.ParentChannel as IIntegrationChannel;
                if (asThread == null || parentChannel == null) return null;

                return new MessageMover
                    (parentChannel, logger, new ThreadMessageCopySender(guild, asThread, logger));
            }
            else
            {
                var iChannel = channel as IIntegrationChannel;
                if (iChannel == null) return null;
                return new MessageMover(
                    iChannel, logger, new MessageCopySender(guild, channel, logger));
            }
        }
        internal async Task MoveAsync(IMessage message, DateTimeOffset until)
        {
            var channelId = _webhookMessageSender.Channel.Id;
            if (_runningList.Contains(channelId))
            {
                await _logger.LogAsync($"A task in the <#{channelId}> is already running.", LogSeverity.Error);
                throw new ApplicationException("Task is already running.");
            }
            if (message == null)
            {
                await _logger.LogAsync($"A task in the <#{channelId}> is already running.", LogSeverity.Error);
                throw new ArgumentException("The message is invalid.");
            }
            if (_channel == null)
            {
                await _logger.LogAsync(
                    $"failed while sending message to nonexistent channel, message: {message?.Id}", LogSeverity.Error);
                throw new ArgumentException("The channel is invalid.");
            }
            var messages = await GetMessagesAsync(message);
            try
            {
                if (await _webhookMessageSender.InitAsync())
                {
                    _runningList.Add(channelId);

                    //first message must be moved aswell
                    if (await _webhookMessageSender.SendWebhookMessageAsync(message))
                    {
                        await message.DeleteAsync();
                    }
                    while (messages.Any())
                    {
                        messages = await MoveChunkAsync(messages, until);
                    }
                }
                else throw new ApplicationException("Failed to get webhook.");
            }
            catch (Exception e)
            {
                await _logger.LogAsync($"{e.Message}", LogSeverity.Error);
#if DEBUG
                await _logger.LogAsync($"{e.StackTrace}", LogSeverity.Error);
#endif
                throw;
            }
            finally
            {
                _webhookMessageSender.Dispose();
                _runningList.Remove(channelId);
            }
            return;
        }
        private async Task<IEnumerable<IMessage>> GetMessagesAsync(IMessage message) =>
            (await message.Channel.GetMessagesAsync(message, Direction.After, limit: _chunk)
            .FlattenAsync())
            .OrderBy(d => d.Timestamp);

        //returns next message group, null if it's done
        private async Task<IEnumerable<IMessage>> MoveChunkAsync
            (IEnumerable<IMessage> messages, DateTimeOffset until)
        {
            if (!messages.Any()) return Enumerable.Empty<IMessage>();
            IMessage? lastMessage = null;
            IMessage? nextDeleteMessage = null;
            foreach (var msg in messages.OrderBy(m => m.Timestamp))
            {
                if (nextDeleteMessage != null)
                {
                    await Task.Delay(_rateLimitManager.GetRateLimit());
                    await nextDeleteMessage.DeleteAsync();
                }
                if (msg.Timestamp > until) return Enumerable.Empty<IMessage>();
                    lastMessage = msg;
                if (await _webhookMessageSender.SendWebhookMessageAsync(msg))
                {
                    nextDeleteMessage = msg;
                }
                else nextDeleteMessage =  null;
                LastMessageTimeStamp = msg.Timestamp;
            }
            if(lastMessage == null) return Enumerable.Empty<IMessage>();
            var nextSequence = await GetMessagesAsync(lastMessage);
            await lastMessage.DeleteAsync();
            return nextSequence;
        }
    }
}
