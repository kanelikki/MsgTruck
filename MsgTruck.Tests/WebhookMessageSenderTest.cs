using Discord;
using Moq;

namespace MsgTruck.Tests
{
    public class WebhookMessageSenderTest
    {
        private const ulong _userId = 234;
        private const ulong _targetChannelId = 123;
        private List<Mock<IWebhook>> _verifyOnceTarget = new();
        private List<Mock<IWebhook>> _verifyZeroTarget = new();
        [Fact]
        public async Task FindWebhookAsync_Retrieves_OnlyOne()
        {
            (var sender, var guild) = GetWebhookMessageSender();
            var result = await sender.FindWebhookAsync();
            Assert.NotNull(result);
            Assert.Equal(_targetChannelId, result.ChannelId);
            Assert.Equal(_userId, result.ApplicationId);
            foreach (var verifyTarget in _verifyOnceTarget)
            {
                verifyTarget.Verify(w => w.DeleteAsync(It.IsAny<RequestOptions>()),
                    Times.Once());
            }
            foreach (var verifyTarget in _verifyZeroTarget)
            {
                verifyTarget.Verify(w => w.DeleteAsync(It.IsAny<RequestOptions>()),
                    Times.Never());
            }
        }
        private (WebhookMessageSender sender, IGuild guild) GetWebhookMessageSender()
        {
            var userMock = new Mock<IGuildUser>();
            userMock.Setup(u => u.Id).Returns(_userId);
            var user = userMock.Object;
            var guildMock = new Mock<IGuild>();
            guildMock.Setup(g => g.GetCurrentUserAsync(
                It.IsAny<CacheMode>(), It.IsAny<RequestOptions>()))
                .ReturnsAsync(user);
            guildMock.Setup(g => g.GetWebhooksAsync(It.IsAny<RequestOptions>()))
                .ReturnsAsync(GetSampleTargets());
            var channelMock = new Mock<IIntegrationChannel>();
            channelMock.SetupGet(c => c.Id).Returns(_targetChannelId);
            var guild = guildMock.Object;
            var sender = new WebhookMessageSender
                (guild, channelMock.Object, Mock.Of<IMessageCopySender>());
            return (sender, guild);
        }
        private IReadOnlyCollection<IWebhook> GetSampleTargets() =>
            [
                //test target
                GetSampleWebhook(_targetChannelId, _userId),
                GetSampleWebhook(_targetChannelId, _userId, true),
                GetSampleWebhook(_targetChannelId, _userId, true),
                //-- must be excluded
                GetSampleWebhook(_targetChannelId+1, _userId),
                GetSampleWebhook(_targetChannelId+1, _userId+1),
                GetSampleWebhook(_targetChannelId, _userId+2),
            ];
        private IWebhook GetSampleWebhook(ulong channelId, ulong appId, bool deleteTarget = false)
        {
            var mock = new Mock<IWebhook>();
            mock.SetupGet(w => w.ChannelId).Returns(channelId);
            mock.SetupGet(w => w.ApplicationId).Returns(appId);
            if (deleteTarget)
            {
                _verifyOnceTarget.Add(mock);
            }
            else
            {
                _verifyZeroTarget.Add(mock);
            }
            return mock.Object;
        }
    }
}
