using System.Text;
using Discord;

namespace MsgTruck
{
    /// <summary>
    /// Contains logics for copying various special message, e.g. Poll, Sticker etc.
    /// Connected with <see cref="MessageCopySender" />.
    /// </summary>
    internal class MessageContentParser
    {
        private readonly HttpClient _httpClient = new();
        public const int MaxTextLength = 2000;

        /// <summary>
        /// Generates poll result summary text from a message.
        /// </summary>
        /// <remarks>This does not close the poll, and can't guarantee the correct result for very active poll.</remarks>
        /// <param name="message">Message that might contain the poll.</param>
        /// <returns><c>null</c> if it's not a poll or it's not a valid poll, otherwise <c>string</c> of poll result summary.</returns>
        internal string? GetPollText(IMessage message)
        {
            if (!(message is IUserMessage userMessage) || userMessage.Poll == null) return null;
            var userPoll = userMessage.Poll;

            StringBuilder pollResultContent = new StringBuilder();
            pollResultContent.Append($"## ");
            if (userPoll.Value.Question.Emoji != null)
            {
                pollResultContent.Append($":{userPoll.Value.Question.Emoji.Name}:");
            }
            pollResultContent.AppendLine($"{userPoll.Value.Question.Text}");
            if (userPoll.Value.AllowMultiselect)
            {
                pollResultContent.AppendLine("-# Multiselect allowed");
            }

            bool isOngoing = userPoll.Value.ExpiresAt > DateTimeOffset.Now;
            if (isOngoing)
            {
                pollResultContent.AppendLine("*The poll was still open.*");
            }
            if (userPoll.Value.Results != null)
            {
                var pollCounts = userPoll.Value.Results.Value.AnswerCounts
                    .ToDictionary(a => a.AnswerId, a => a.Count);
                var answerSum = pollCounts.Sum(a => a.Value);
                foreach (var pollEntry in userPoll.Value.Answers)
                {
                    pollResultContent.Append($"> {pollEntry.AnswerId.ToString()}. ");
                    var emoji = pollEntry.PollMedia.Emoji;
                    if (emoji != null) pollResultContent.Append($"{emoji.Name} ");
                    pollResultContent.Append($"{pollEntry.PollMedia.Text} : ");

                    uint answerCount = 0;
                    double ratio = 0;
                    if (pollCounts!.TryGetValue(pollEntry.AnswerId, out var amount))
                    {
                        answerCount = amount;
                        ratio = ((double)answerCount / answerSum);
                    }
                    pollResultContent.AppendLine($"**{answerCount}** " +
                        $"(**{ratio.ToString("P2", System.Globalization.CultureInfo.InvariantCulture)}**)");
                }
            }
            else
            {
                pollResultContent.AppendLine("> **No poll data available**.");
            }
            return pollResultContent.ToString();
        }
        internal bool IsMessageCopyable(IMessage message, string? pollText)
            => IsMessageNotEmpty(message, pollText) && !message.IsPinned
            && message is IUserMessage;
        internal bool IsMessageNotEmpty(IMessage message, string? pollText)
            => (pollText != null) || !string.IsNullOrWhiteSpace(message.Content)
            || message.Attachments.Any() || message.Embeds.Any() || message.Components.Any();

        /// <summary>
        /// Copies attachment files and returns the copied file data, so can re-send it in the new message.
        /// </summary>
        /// <param name="iAttachments">Attachment from <see cref="IMessage"/>.</param>
        /// <returns>Attachment files in the format that used for message sending.</returns>
        internal async Task<IEnumerable<FileAttachment>> ToFileAttachments(IEnumerable<IAttachment> iAttachments)
            => (await Task.WhenAll(iAttachments.Select(ToFileAttachment))).Where(r => r != null).Cast<FileAttachment>().ToList();

        private async Task<FileAttachment?> ToFileAttachment(IAttachment attachment)
        {
            using var fResult = await _httpClient.GetAsync(attachment.Url);
            if (!fResult.IsSuccessStatusCode) return null;
            var fStream = new MemoryStream();
            await fResult.Content.CopyToAsync(fStream);
            return new FileAttachment(fStream, attachment.Filename, attachment.Description, attachment.IsSpoiler());
        }
        /// <summary>
        /// Makes placeholder text for stickers.
        /// </summary>
        /// <remarks>
        /// Due to the technical limit, sticker cannot be moved directly.
        /// This method generates string that indicates the sticker was existed there.
        /// </remarks>
        /// <param name="stickers">Sticker values from <see cref="IMessage"/>.</param>
        /// <returns>Text that represents a sticker.</returns>
        private string StickersAsString(IEnumerable<IStickerItem> stickers) =>
            Environment.NewLine + string.Join(Environment.NewLine,
                stickers.Select(s => $"> [Sticker {s.Name}]")
                );
        /// <summary>
        /// Copies interaction components from a message, possibly from a bot.
        /// </summary>
        /// <remarks>
        /// The <see cref="MessageComponent"/> is NOT an implementation of <see cref="IMessageComponent"/>.
        /// <c>IMessageComponent</c> represents **one** component, e.g. button, select menu etc,
        /// while <c>MessageComponent</c> is collection of **possibly multiple components**, *per message*.
        /// The maximum depth for <see cref="ActionRowComponent"/> is 10, more than the depth won't be copied.
        /// Copies only content **without copying functionality**.
        /// Currently <see cref="ButtonComponent"/>, <see cref="SelectMenuComponent"/>, <see cref="ActionRowComponent"/> are supported.
        /// Unsupported components will be ignored and not copied.
        /// </remarks>
        /// <param name="components">Components to copy, from <see cref="IMessage"/>.</param>
        /// <returns>Message Component class that is used for sending components in message.</returns>
        internal MessageComponent? CopyComponents(IEnumerable<IMessageComponent> components)
            => CopyComponents(components, 0);
        private MessageComponent? CopyComponents(IEnumerable<IMessageComponent> components, int depth)
        {
            if (depth > 10) return null;
            if (!components.Any()) return null;
            var builder = new ComponentBuilder();
            foreach (var component in components)
            {
                switch (component)
                {
                    case ButtonComponent button:
                        builder.WithButton(button.Label, Guid.NewGuid().ToString());
                        break;
                    case SelectMenuComponent selectMenu:
                        var selectMenuBuilder = new SelectMenuBuilder();
                        foreach (var menu in selectMenu.Options)
                        {
                            var option = new SelectMenuOptionBuilder()
                                .WithLabel(menu.Label)
                                .WithValue(Guid.NewGuid().ToString());
                            if (menu.IsDefault != null && menu.IsDefault.Value) option.WithDefault(true);
                            selectMenuBuilder.AddOption(option);
                        }
                        builder.WithSelectMenu(selectMenuBuilder);
                        break;
                    case ActionRowComponent actionRow:
                        if (depth < 10)
                        {
                            var actionComponents = CopyComponents(actionRow.Components, depth+1);
                            var actionRowBuilder = new ActionRowBuilder();
                            if (actionComponents != null)
                            {
                                foreach (var c in actionComponents.Components)
                                {
                                    actionRowBuilder.AddComponent(c);
                                }
                            } 
                            builder.AddRow(actionRowBuilder);
                        }
                        break;
                }
            }
            return builder.Build();
        }
        internal string PrependContent(IMessage message, string? url)
        {
            var content = message.Content;
            if (message.Stickers.Any())
            {
                content = AddTextIfHasSpace
                    (content, StickersAsString(message.Stickers));
            }
            if (url != null && url.StartsWith(MessageCopySender.UrlPrefix))
            {
                content = AddTextIfHasSpace(content, url);
            }
            return content;
        }
        private string AddTextIfHasSpace(string content, string additionalText)
        {
            if (content.Length + additionalText.Length + 2 <= MaxTextLength)
            {
                return string.IsNullOrWhiteSpace(content) ?
                    additionalText: additionalText + Environment.NewLine + content;
            }
            return content;
        }
    }
}
