using Discord;
using Discord.Interactions;

namespace MsgTruck
{
    public class MessageContextModal : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ILogger _logger;
        private readonly SnowFlakeIdParser _idParser;
        private readonly PermissionChecker _permissionChecker;
        private readonly ChannelManager _channelManager;
        public MessageContextModal(ILogger logger)
        {
            _logger = logger;
            _idParser = new();
            _permissionChecker = new();
            _channelManager = new(_idParser, _permissionChecker);
        }
        [ModalInteraction("MMOV:*")]
        public async Task AcceptMoveModalAsync(string identifier, ModalData modalData)
        {
            var channelResult = _channelManager.GetTargetChannel(Context, modalData);
            IMessageChannel? channel = channelResult.Channel;
            if (channel == null)
            {
                await RespondAndLogAsync(channelResult);
            }
            else
            {
                await ReadModalAndMoveAsync(identifier, channel, modalData);
            }
        }
        [ModalInteraction("MMOV_T:*")]
        public Task AcceptPublicThreadMoveModalAsync(string identifier, ModalData modalData)
            => AcceptThreadMoveModalAsync(identifier, modalData, true);
        [ModalInteraction("MMOV_T_PRV:*")]
        public Task AcceptPrivateThreadMoveModalAsync(string identifier, ModalData modalData)
            => AcceptThreadMoveModalAsync(identifier, modalData, false);

        public async Task AcceptThreadMoveModalAsync
            (string identifier, ModalData modalData, bool isPublicThread)
        {
            var channelResult = await _channelManager.CreateTargetThread(Context, modalData, isPublicThread);
            IMessageChannel? channel = channelResult.Channel;

            if (channel == null)
            {
                await RespondAndLogAsync(channelResult);
            }
            else
            {
                await ReadModalAndMoveAsync(identifier, channel, modalData);
            }
        }

        private async Task ReadModalAndMoveAsync(string identifier, IMessageChannel channel, ModalData modalData)
        {
            if (string.IsNullOrWhiteSpace(modalData.EndId))
            {
                modalData.EndId = identifier;
            }
            (IMessage? msg, IMessage? endMsg) = await ParseMessagesAsync(Context.Channel, identifier, modalData.EndId);
            if (msg == null || endMsg == null) return;
            var messageMover = MessageMover.GetMessageMover(Context.Guild, channel, _channelManager, _logger);
            if (messageMover == null)
            {
                await RespondAndLogAsync("Program internal error: The channel is null, so can't send the message.");
                return;
            }
            await RespondAsync($"Messages are migrating to <#{channel.Id}>...Don't be panic!", ephemeral: true);
            await channel.SendMessageAsync($"Message from <t:{msg.Timestamp.ToUnixTimeSeconds()}:f>");
            try
            {
                await messageMover.MoveAsync(msg, endMsg.Timestamp);
                await FollowupAsync($"Done Moving to the <#{channel.Id}>. Thank you for the patience.", ephemeral: true);
            }
            finally
            {
                await channel.SendMessageAsync($"Last Message sent <t:{endMsg.Timestamp.ToUnixTimeSeconds()}:f>");
            }
        }

        private async Task<(IMessage? StartMsg, IMessage? EndMsg)>
            ParseMessagesAsync(IChannel targetChannel, string startMsgId, string endMsgId)
        {
            var msgResults = await Task.WhenAll(
                _idParser.TryParseMessageAsync(Context.Channel, startMsgId),
                _idParser.TryParseMessageAsync(Context.Channel, endMsgId));
            if (msgResults[0].Result != ParseStatus.Success)
            {
                await RespondAndLogAsync(SnowFlakeIdParser.WrongIDPassText);
                return (null, null);
            }
            switch (msgResults[1].Result)
            {
                case ParseStatus.InvalidId:
                    await RespondAndLogAsync(SnowFlakeIdParser.WrongIDErrorText);
                    return (null, null);
                case ParseStatus.InvalidData:
                    await RespondAndLogAsync(SnowFlakeIdParser.NoMessageErrorText);
                    return (null, null);
                default:
                    return (msgResults[0].Message, msgResults[1].Message);
            }
        }
        private Task RespondAndLogAsync(ChannelValidationResult errorResult)
        {
            if (errorResult.UseLogger)
            {
                return RespondAsync(errorResult.ErrorMessage, ephemeral: true);
            }
            return RespondAndLogAsync(errorResult.ErrorMessage, errorResult.LoggerErrorMessage);
        }
        private Task RespondAndLogAsync(string errorMessage)
            => RespondAndLogAsync(errorMessage, errorMessage);
        private Task RespondAndLogAsync(string errorMessage, string logErrorMessage)
        {
            return Task.WhenAll([RespondAsync(errorMessage, ephemeral: true),
                _logger.LogAsync(logErrorMessage, LogSeverity.Error)]);
        }
    }
}
