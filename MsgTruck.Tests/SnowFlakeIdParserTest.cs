using Discord;
using Discord.WebSocket;
using Moq;

namespace MsgTruck.Tests
{
    //getting channels cannot be mocked so can't be tested (sad)
    public class SnowFlakeIdParserTest
    {
        private readonly SnowFlakeIdParser _parser;
        private const string _messageUrl = "https://discord.com/channels/111/222/12122";
        public SnowFlakeIdParserTest()
        {
            _parser = new();
        }
        [Fact]
        public async Task TryParseMessageAsync_FromUrlOrIdString_ToId()
        {
            var mockChannel = new Mock<ISocketMessageChannel>();
            var idMsg = Mock.Of<IMessage>();
            var urlMsg = Mock.Of<IMessage>();
            mockChannel.Setup(c =>
                c.GetMessageAsync(It.Is<ulong>(n => n==123123),
                It.IsAny<CacheMode>(), It.IsAny<RequestOptions>())
            ).ReturnsAsync(idMsg);
            mockChannel.Setup(c =>
                c.GetMessageAsync(It.Is<ulong>(n => n==12122),
                It.IsAny<CacheMode>(), It.IsAny<RequestOptions>())
            ).ReturnsAsync(urlMsg);
            (var resultById, var msgResultId) =
                await _parser.TryParseMessageAsync(mockChannel.Object, "123123");
            (var resultByUrl, var msgResultUrl) =
                await _parser.TryParseMessageAsync(mockChannel.Object, _messageUrl);
            Assert.Equal(ParseStatus.Success, resultById);
            Assert.Equal(ParseStatus.Success, resultByUrl);
            Assert.Equal(idMsg, msgResultId);
            Assert.Equal(urlMsg, msgResultUrl);
        }
    }
}
