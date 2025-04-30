using Discord;

namespace MsgTruck
{
    /// <summary>
    /// Checks permission of the channel, mostly for this bot.
    /// </summary>
    internal class PermissionChecker
    {
        private readonly IEnumerable<ChannelPermission> _allowedPermissions = [
                ChannelPermission.ViewChannel,
                ChannelPermission.ManageWebhooks,
                ChannelPermission.SendMessages
            ];
        private readonly IEnumerable<ChannelPermission> _allowedThreadPermissions = [
                ChannelPermission.ViewChannel,
                ChannelPermission.ManageWebhooks,
                ChannelPermission.SendMessages,
                ChannelPermission.SendMessagesInThreads
            ];

        internal bool HasPermission(IGuildUser currentUser, IGuildChannel? channel,
            out IEnumerable<ChannelPermission> missedPermissions)
            => HasPermission(currentUser, channel, out missedPermissions,
                ChannelManager.IsThread(channel) ?  _allowedThreadPermissions : _allowedPermissions);
        internal bool HasNewThreadPermission(IGuildUser currentUser, IGuildChannel? channel,
            out IEnumerable<ChannelPermission> missedPermissions, bool publicThread = true)
            => HasPermission(currentUser, channel, out missedPermissions,
                publicThread ? [ChannelPermission.CreatePublicThreads] : [ChannelPermission.CreatePrivateThreads]);

        private bool HasPermission(IGuildUser currentUser, IGuildChannel? channel,
            out IEnumerable<ChannelPermission> missedPermissions, IEnumerable<ChannelPermission> requiredPermissions)
        {
            var missedPermissionsList = new List<ChannelPermission>();
            if (channel == null)
            {
                missedPermissions = missedPermissionsList;
                return false;
            }
            var permissions = currentUser.GetPermissions(channel);
            foreach (var perm in requiredPermissions)
            {
                if (!permissions.Has(perm))
                {
                    missedPermissionsList.Add(perm);
                }
            }
            missedPermissions = missedPermissionsList;
            return !missedPermissions.Any();

        }
    }
}
