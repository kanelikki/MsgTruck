using Discord;
using Discord.Webhook;

namespace MsgTruck
{
    /// <summary>
    /// Manages webhook for sending persona-changed message, used during moving message.
    /// </summary>
    internal class WebhookMessageSender : IDisposable
    {
        private IWebhook? _webhook;
        private DiscordWebhookClient? _webhookClient;
        private IGuild _guild;
        public IIntegrationChannel Channel { get; init; }
        private IMessageCopySender _sender;

        internal WebhookMessageSender(IGuild guild, IIntegrationChannel channel, IMessageCopySender sender)
        {
            _guild = guild;
            Channel = channel;
            _sender = sender;
        }
        /// <summary>
        /// Initialises (finds or creates) webhook for message with changed persona.
        /// </summary>
        /// <returns><c>true</c> if succeeded to get a webhook, otherwise <c>false</c>.</returns>
        internal Task<bool> InitAsync() => FindOrCreateWebhookAsync();

        /// <summary>
        /// Copy the message to the another channel, using webhook.
        /// </summary>
        /// <param name="message">The message to copy.</param>
        /// <returns><c>true</c> if it is copied, <c>false</c> if the message copying is skipped for some reason.</returns>
        /// <exception cref="ApplicationException">The class is not initialised, so there's no web hook client.</exception>
        internal Task<bool> SendWebhookMessageAsync(IMessage message)
        {
            if (_webhookClient == null)
            {
                throw new ApplicationException("Webhook client is not created normally.");
            }
            var user = message.Author;
            return _sender.CopyMessageAsync(_webhookClient, message);
        }

        internal async Task<IWebhook?> FindWebhookAsync()
        {
            var self = (await _guild.GetCurrentUserAsync()).Id;
            var allHooks = (await _guild.GetWebhooksAsync())
                .Where(c => c.ChannelId == Channel.Id && c.ApplicationId == self);
            if (!allHooks.Any()) return null;
            foreach (var hook in allHooks.Skip(1))
            {
                await hook.DeleteAsync();
            }
            return allHooks.First();
        }
        private async Task<bool> CreateAndSetWebhookAsync()
        {
            if (_webhook != null)
            {
                await _webhook.DeleteAsync();
            }
            var cancelToken = new CancellationToken(false);
            _webhook = await Channel.CreateWebhookAsync(
                "MsgTruck", options: GetRequestOptions(cancelToken));
            cancelToken.ThrowIfCancellationRequested();
            if (cancelToken.IsCancellationRequested)
            {
                _webhook = null;
                return false;
            }
            return true;
        }

        private async Task<bool> FindOrCreateWebhookAsync()
        {
            _webhook = await FindWebhookAsync();
            if (_webhook == null)
            {
                if (await CreateAndSetWebhookAsync())
                {
                    _webhookClient = new DiscordWebhookClient(_webhook);
                    return true;
                }
                return false;
            }
            else
            {
                _webhookClient = new DiscordWebhookClient(_webhook);
                return true;
            }
        }

        private RequestOptions GetRequestOptions(CancellationToken cancellationToken) =>
            new RequestOptions
            {
                RetryMode = RetryMode.Retry502 | RetryMode.RetryRatelimit,
                Timeout = 5000,
                CancelToken = cancellationToken
            };

        public void Dispose()
        {
            _webhookClient?.Dispose();
        }
    }
}
