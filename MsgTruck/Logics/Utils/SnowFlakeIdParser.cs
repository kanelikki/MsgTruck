using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;

namespace MsgTruck
{
    /// <summary>
    /// Validates input and finds a snowflake data, e.g. channel, message.
    /// </summary>
    internal class SnowFlakeIdParser
    {
        private readonly Regex _testRegex = new(@"https:\/\/discord\.com\/channels\/[0-9]+\/[0-9]+\/(?<msgid>[0-9]+)", RegexOptions.IgnoreCase);

        internal const string NoMessageErrorText = "The message cannot be found! Could be Cache issue. In this case, we can do nothing...";
        internal const string WrongIDPassText = "Invalid start message ID passed. This can be a bug of the program.";
        internal const string WrongIDErrorText = "The message ID is invalid. Please provide valid Message ID or Message URL.";

        internal SnowFlakeIdParser()
        {
        }
        private bool TryGetMessageId(string messageIdentifier, out ulong result)
        {
            var match = _testRegex.Match(messageIdentifier);
            if (match.Success)
            {
                if (!match.Groups.TryGetValue("msgid", out Group? matchGroup) || matchGroup == null)
                { 
                    result = 0;
                    return false;
                }
                return ulong.TryParse(matchGroup.Value, out result);
            }
            else return ulong.TryParse(messageIdentifier, out result);
        }
        internal IMessageChannel? ParseChannel(SocketGuild guild, string channelIdentifier)
        {
            IMessageChannel? channel = null;
            if (ulong.TryParse(channelIdentifier, out ulong channelId))
            {
                channel = guild.GetChannel(channelId) as IMessageChannel;
            }
            if(channel == null)
            {
                channel = guild.Channels.FirstOrDefault(x => x.Name == channelIdentifier) as IMessageChannel;
            }
            return channel;
        }
        internal async Task<(ParseStatus Result, IMessage? Message)> TryParseMessageAsync
            (ISocketMessageChannel msgChannel, string msgIdString)
        {
            if (!TryGetMessageId(msgIdString, out ulong msgId))
            {
                return (ParseStatus.InvalidId, null);
            }
            var msg = await msgChannel.GetMessageAsync(msgId);
            if (msg == null)
            {
                return (ParseStatus.InvalidData, null);
            }
            return (ParseStatus.Success, msg);
        }
    }
}
