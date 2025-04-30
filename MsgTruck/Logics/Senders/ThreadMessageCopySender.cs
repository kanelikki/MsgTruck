using Discord;
using Discord.WebSocket;

namespace MsgTruck
{
    internal class ThreadMessageCopySender : MessageCopySender
    {
        internal ThreadMessageCopySender(IGuild guild, SocketThreadChannel targetChannel, ILogger logger)
            : base(guild, targetChannel, logger)
        {
            _threadId = targetChannel.Id;
        }
    }
}
