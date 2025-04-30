using Moq;
using Discord;

namespace MsgTruck.Tests
{
    public class MessageContentParserTest
    {
        private readonly MessageContentParser _parser = new();
        //can't check detail because poll is struct that can't be mocked
        [Fact]
        public void GetPollText_Returns_Poll()
        {
            var nopoll = new Mock<IUserMessage>();
            nopoll.Setup(m => m.Poll).Returns(()=>null);
            Assert.Null(_parser.GetPollText(nopoll.Object));
            var hasPoll = new Mock<IUserMessage>();
            hasPoll.Setup(m => m.Poll).Returns(()=>new Poll());
            Assert.NotNull(_parser.GetPollText(hasPoll.Object));
        }
        [Fact]
        public void IsMessageCopyable_Skips_SystemMessage()
        {
            var systemMessageMock = new Mock<ISystemMessage>();
            systemMessageMock.SetupGet(s => s.Content).Returns("System Message");
            var systemMessageCopyTest = _parser
                .IsMessageCopyable(systemMessageMock.Object, null);
            Assert.False(systemMessageCopyTest);
        }
        [Fact]
        public void PrependContent_Attaches_UrlWithSticker()
        {
            var fullText = "hi";
            var msg = GetMessageMock(fullText);
            var urlPrepend = _parser.PrependContent
                (msg, MessageCopySender.UrlPrefix+"._.");

            var stickerMsg = GetMessageMock(fullText, true);
            var urlPrependWithSticker = _parser.PrependContent
                (stickerMsg, MessageCopySender.UrlPrefix+"._.");
            var nonUrlPrependWithSticker = _parser.PrependContent(stickerMsg, null);

            Assert.NotEqual(fullText, urlPrepend);
            Assert.NotEqual(fullText, nonUrlPrependWithSticker);
            Assert.NotEqual(urlPrepend, urlPrependWithSticker);
            Assert.NotEqual(nonUrlPrependWithSticker, urlPrependWithSticker);
            Assert.NotEqual(fullText, urlPrependWithSticker);
        }
        [Fact]
        public void PrependContent_Ignores_InvalidUrl()
        {
            var originalMessage = "hello world";
            var content = _parser
                .PrependContent(GetMessageMock(originalMessage), "I can't believe this is not an URL!");
            Assert.Equal(content, originalMessage);
        }

        [Fact]
        public void PrependContent_DoNotAttach_OverMaximumLength()
        {
            var fullText = string.Concat(
                Enumerable.Repeat('a', MessageContentParser.MaxTextLength - 2)
            );
            var msg = GetMessageMock(fullText);
            var urlPrepend = _parser.PrependContent(msg, MessageCopySender.UrlPrefix+"._.");
            var stickerMsg = GetMessageMock(fullText, true);
            var nonUrlPrepend = _parser.PrependContent(msg, null);
            Assert.Equal(fullText, urlPrepend);
            Assert.Equal(fullText, nonUrlPrepend);
        }
        private IMessage GetMessageMock(string message, bool hasSticker = false)
        {
            var mock = new Mock<IMessage>();
            mock.Setup(m => m.Content).Returns(message);
            if (hasSticker)
            {
                var sticker = new Mock<IStickerItem>();
                sticker.Setup(s => s.Name).Returns("sticky");
                mock.Setup(m => m.Stickers).Returns([
                    sticker.Object
                    ]);
            }
            else
            {
                mock.Setup(m => m.Stickers).Returns([]);
            }
            return mock.Object;
        }
        [Fact]
        public void CopyComponents_Copy_Buttons()
        {
            var firstBuilder = new ActionRowBuilder();
            var secondBuilder = new ActionRowBuilder();
            secondBuilder.AddComponent(GetButtonComponent());
            secondBuilder.AddComponent(GetButtonComponent());
            var secondRow = secondBuilder.Build();
            firstBuilder.AddComponent(secondRow);
            var copy = _parser.CopyComponents([firstBuilder.Build()]);
            Assert.NotNull(copy);
            Assert.Equal(secondRow, firstBuilder.Components.First());
            Assert.Equal(2, secondBuilder.Components.Count());
        }
        [Fact]
        public void CopyComponents_Copy_SelectMenu()
        {
            var firstBuilder = new ActionRowBuilder();
            var secondBuilder = new ActionRowBuilder();
            secondBuilder.AddComponent(GetSelectMenuComponent());
            var secondRow = secondBuilder.Build();
            firstBuilder.AddComponent(secondRow);
            var copy = _parser.CopyComponents([firstBuilder.Build()]);
            Assert.NotNull(copy);
            Assert.Equal(secondRow, firstBuilder.Components.First());
            var menu = secondRow.Components.Single() as SelectMenuComponent;
            Assert.NotNull(menu);
            Assert.Equal(3, menu.Options.Count);
        }

        //Empty actionrow will not added, which can be normal in real situation
        [Theory]
        [InlineData(6, true)]
        [InlineData(12, false)]
        public void CopyComponents_StopCopying_AfterMaxDepth(int depth, bool inRange)
        {
            var firstBuilder = new ActionRowBuilder();
            firstBuilder.AddComponent(GetButtonComponent());
            var currentBuilder = firstBuilder; 
            for (int i = 0; i < depth; i++)
            {
                var newBuilder = new ActionRowBuilder();
                newBuilder.AddComponent(currentBuilder.Build());
                currentBuilder = newBuilder;
            }
            var copy = _parser.CopyComponents([currentBuilder.Build()]);
            Assert.NotNull(copy);
            if (inRange)
            {
                Assert.True(copy.Components.Any(), "No component in copied component");
                var currentComponent = copy.Components.FirstOrDefault();
                int depthCount = 0;
                while (currentComponent != null && currentComponent.Components.Any())
                {
                    currentComponent =
                        currentComponent.Components.FirstOrDefault() as ActionRowComponent;
                    if(currentComponent != null) depthCount++;
                }
                //last element button counts
                Assert.Equal(depth+1, depthCount);
            }
            else
            {
                Assert.False(copy.Components.Any(), "Too much depths, expected to have no content");
            }
        }
        private ButtonComponent GetButtonComponent()
            => new ButtonBuilder()
                .WithLabel("hi")
                .WithStyle(ButtonStyle.Link)
                .WithUrl("https://example.com")
                .Build();
        private SelectMenuComponent GetSelectMenuComponent()
            => new SelectMenuBuilder()
            .WithCustomId("asdfasdf")
            .WithOptions(new()
            {
                GetMenuOptionBuilder(),
                GetMenuOptionBuilder(),
                GetMenuOptionBuilder()
            })
            .Build();
        private SelectMenuOptionBuilder GetMenuOptionBuilder()
            => new SelectMenuOptionBuilder()
            .WithLabel("asdf")
            .WithValue("???");
    }
}