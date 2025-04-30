using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace MsgTruck
{
    /// <summary>
    /// Checks the type of a snowflake, e.g. Channel Type and Message Type
    /// </summary>
    internal class ChannelManager
    {
        private const string _invalidChannelErrorMessage = "This command can be used only in text channel.";
        private SnowFlakeIdParser _parser;
        private PermissionChecker _permissionChecker;
        internal ChannelManager(SnowFlakeIdParser parser, PermissionChecker permissionChecker)
        {
            _parser = parser;
            _permissionChecker = permissionChecker;
        }
        internal static bool IsThread(IChannel channel) =>
            channel.ChannelType == ChannelType.NewsThread
            || channel.ChannelType == ChannelType.PublicThread
            || channel.ChannelType == ChannelType.PrivateThread;

        internal static bool IsThreadOrTextChannel(IChannel channel)
            => channel.ChannelType == ChannelType.Text || IsThread(channel);

        internal ChannelValidationResult GetTargetChannel
            (SocketInteractionContext context, ModalData modalData)
        {
            if (!IsThreadOrTextChannel(context.Channel))
            {
                return ChannelValidationResult.FromUserFailure(_invalidChannelErrorMessage);
            }
            IMessageChannel? channel = _parser.ParseChannel(context.Guild, modalData.Channel);
            if (channel == null)
            {
                return ChannelValidationResult.FromSystemFailure
                    ($"Invalid Channel ID or name {modalData.Channel}.", null);
            }
            if (!_permissionChecker
                .HasPermission(context.Guild.CurrentUser, channel as IGuildChannel, out var rejected))
            {
                if (!rejected.Any())
                {
                    return ChannelValidationResult.FromSystemFailure
                        ($"Seems like the <#{channel.Id}> has weird issue, check the log.",
                        $"The channel {channel.Name} is not valid Guild Channel.");
                }
                else
                {
                    return ChannelValidationResult.FromUserFailure
                        ($"Permission denied. The bot doesn't have these permissions for <#{channel.Id}>:" +
                        $"{string.Join(", ", rejected.Select(r => Enum.GetName(r)))}");
                }
            }
            return ChannelValidationResult.FromMaybeSuccess(channel);
        }
        internal async Task<ChannelValidationResult> CreateTargetThread
            (SocketInteractionContext context, ModalData modalData, bool isPublicThread)
        {
            if (!IsThreadOrTextChannel(context.Channel))
            {
                return ChannelValidationResult.FromUserFailure(_invalidChannelErrorMessage);
            }
            ITextChannel? createTarget = null;
            if (IsThread(context.Channel))
            {
                var parent = (context.Channel as SocketThreadChannel)?.ParentChannel;
                createTarget = parent as ITextChannel;
            }
            else
            {
                createTarget = context.Channel as ITextChannel;
            }
            if (createTarget == null)
            {
                return ChannelValidationResult.FromSystemFailure
                    ($"Current channel <#{context.Channel.Id}> is somehow marked as invalid. Maybe technical issue?",
                    $"Error with channel {context.Channel.Name} (ID: {context.Channel.Id}), null value found.");
            }
            if (!_permissionChecker
                .HasNewThreadPermission(context.Guild.CurrentUser, createTarget, out _, isPublicThread))
            {
                return ChannelValidationResult.FromUserFailure
                    ($"The bot doesn't have permission to create {(isPublicThread ? "Public" : "Private")} channel.");
            }
            var result = await createTarget.CreateThreadAsync(modalData.Channel,
                type: isPublicThread ? ThreadType.PublicThread : ThreadType.PrivateThread);
            return ChannelValidationResult.FromMaybeSuccess(result);
        }
    }

}
