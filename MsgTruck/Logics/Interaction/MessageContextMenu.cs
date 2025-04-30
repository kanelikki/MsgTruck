using Discord;
using Discord.Interactions;

namespace MsgTruck.Modal
{
    public class MessageContextMenu : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ILogger _logger;
        public MessageContextMenu(ILogger logger)
        {
            _logger = logger;
        }

        [MessageCommand("Move Messages")]
        public Task MoveMessages(IMessage message)
            => OpenMoveMessageDialog(message, $"MMOV:{message.Id.ToString()}",
                "Channel Name or ID (ID takes priority)");

        [MessageCommand("Move To New Thread")]
        public Task MoveMessagesToNewThread(IMessage message)
            => OpenMoveMessageDialog(message, $"MMOV_T:{message.Id.ToString()}",
                "NEW Public Thread name");
        [MessageCommand("Move To New Private Thread")]
        public Task MoveMessagesToNewPrivateThread(IMessage message)
            => OpenMoveMessageDialog(message, $"MMOV_T_PRV:{message.Id.ToString()}",
                "NEW Private Thread name");
        private Task OpenMoveMessageDialog
            (IMessage message, string customId, string channelLabel)
        {
            if (!Context.Guild.CurrentUser.GuildPermissions.ManageWebhooks)
            {
                return RespondAsync(
                    "The bot doesn't have **Manage Webhook** permission.\n" +
                    "Please enable the *manage webhook* in the role settings.",
                    ephemeral: true);
            }
            return RespondWithModalAsync(
                            (
                            new ModalBuilder()
                            .WithTitle("Load your mail to the truck")
                            .WithCustomId(customId)
                            .AddTextInput("End of message ID or URL (empty = only this)",
                                nameof(ModalData.EndId), required: false)
                            .AddTextInput(channelLabel, nameof(ModalData.Channel), required: true)
                            ).Build()
                        );

        }
    }
}
