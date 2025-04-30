using Discord;
using Discord.Webhook;

namespace MsgTruck
{
    internal interface IMessageCopySender
    {
        internal Task<bool> CopyMessageAsync
            (DiscordWebhookClient client, IMessage message);
    }
}
