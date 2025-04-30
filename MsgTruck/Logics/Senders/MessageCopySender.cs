using System.Runtime.CompilerServices;
using Discord;
using Discord.Webhook;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace MsgTruck
{
    /// <summary>
    /// Checks content of the message and copies it properly, e.g. reply, file append etc.
    /// </summary>
    internal class MessageCopySender : IMessageCopySender
    {
        private readonly Dictionary<ulong, ulong> _replyMessageMap = new();
        private readonly ulong _guild;
        private readonly ulong _channel;
        private readonly ILogger _logger;
        private readonly MessageContentParser _parser = new();
        protected ulong? _threadId { get; set; }

        internal const string UrlPrefix = "https://discord.com/channels/";

        //if message sending builder is implemented in discord.net next version, this code can be updated as well
        /// <summary>
        /// The constructor of the reader. Parameters are needed for reply link.
        /// </summary>
        /// <param name="guild">ID of the guild that performs the message moving.</param>
        /// <param name="channel">Destination channel to move the content.</param>
        internal MessageCopySender(IGuild guild, IChannel channel, ILogger logger)
        {
            _guild = guild.Id;
            _channel = channel.Id;
            _logger = logger;
        }
        public async Task<bool> CopyMessageAsync(DiscordWebhookClient client, IMessage message)
        {
            string? url = null;
            var user = message.Author;
            if (message.Type == MessageType.Reply &&
                message.Reference.GuildId.IsSpecified && message.Reference.MessageId.IsSpecified)
            {
                var reference = message.Reference;
                if (_replyMessageMap.TryGetValue(reference.MessageId.Value, out var movedMessageId))
                {
                    url = GetReplyUrl(_guild, _channel, movedMessageId);
                }
                else
                {
                    url = GetReplyUrl(
                        message.Reference.GuildId.Value, message.Reference.ChannelId, message.Reference.MessageId.Value);
                }
            }
            string content = _parser.PrependContent(message, url);
            string? pollText = _parser.GetPollText(message);

            Task<ulong> task;
            IEnumerable<FileAttachment>? fileAttachments = null;
            try
            {
                if (_parser.IsMessageCopyable(message, pollText))
                {
                    //"Due to limitations set by the Discord API, it's not possible to send both an attachment and a poll in the same message."
                    //https://docs.discordnet.dev/guides/polls/polls.html
                    if (message.Attachments.Any())
                    {
                        fileAttachments = await _parser.ToFileAttachments(message.Attachments);
                        task = client.SendFilesAsync(
                            fileAttachments, content, embeds: message.Embeds as IEnumerable<Embed>,
                            threadId: _threadId, components: _parser.CopyComponents(message.Components),
                            username: user.GlobalName, avatarUrl: user.GetAvatarUrl());
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(content) && pollText != null)
                        {
                            content = "## [ Poll ]";
                        }
                        task = client.SendMessageAsync(
                            content, embeds: message.Embeds as IEnumerable<Embed>,
                            components: _parser.CopyComponents(message.Components),
                            threadId: _threadId,
                            username: user.GlobalName, avatarUrl: user.GetAvatarUrl());
                    }
                    var sentMsgId = await task;
                    _replyMessageMap.Add(message.Id, sentMsgId);
                    if (pollText != null)
                    {
                        await client.SendMessageAsync(
                            pollText, threadId: _threadId
                        );
                    }
                    return true;
                }
                else return false;
            }
            catch (Exception e)
            {
                await _logger.LogAsync(e.Message, LogSeverity.Error);
                if(e.InnerException!=null) await _logger.LogAsync(e.InnerException.Message, LogSeverity.Error);
#if DEBUG
                if(e.StackTrace != null) await _logger.LogAsync(e.StackTrace, LogSeverity.Error);
#endif
                throw;
            }
            finally
            {
                if (fileAttachments != null)
                {
                    foreach (var f in fileAttachments)
                    {
                        f.Dispose();
                    }
                }
            }
        }
        private string GetReplyUrl(ulong guildId, ulong channelId, ulong msgId)
            => UrlPrefix+$"{guildId}/{channelId}/{msgId}";
    }
}
