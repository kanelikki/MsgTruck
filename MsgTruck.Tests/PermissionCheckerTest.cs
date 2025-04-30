using Discord;
using Moq;

namespace MsgTruck.Tests
{
    public class PermissionCheckerTest
    {
        private readonly PermissionChecker _permissionChecker;
        public PermissionCheckerTest()
        {
            _permissionChecker = new PermissionChecker();
        }
        [Theory]
        [InlineData(ChannelPermission.ViewChannel | ChannelPermission.SendMessages, 1)]
        [InlineData(ChannelPermission.ViewChannel, 2)]
        public void PermissionChecker_Reject_BasicPermissions
            (ChannelPermission permissions, int missedCount)
        {
            var userMock = new Mock<IGuildUser>();
            var channel = SetupChannel();
            SetupPermissions(userMock, channel, permissions);
            var permCheck = _permissionChecker
                .HasPermission(userMock.Object,channel, out var missed);

            Assert.False(permCheck);
            Assert.Equal(missedCount, missed.Count());
        }
        [Fact]
        public void PermissionChecker_Reject_ThreadCreation()
        {
            var userMock = new Mock<IGuildUser>();
            var channel = SetupChannel();

            SetupPermissions(userMock, channel,
                ChannelPermission.ViewChannel | ChannelPermission.SendMessages
                | ChannelPermission.ManageWebhooks | ChannelPermission.CreatePublicThreads );

            var publicPermCheck = _permissionChecker
                .HasNewThreadPermission(userMock.Object, channel, out var missed, true);
            var privatePermCheck = _permissionChecker
                .HasNewThreadPermission(userMock.Object, channel, out var missed2, false);

            Assert.True(publicPermCheck);
            Assert.False(privatePermCheck);
            Assert.Single(missed2);
        }
        private Mock<IGuildUser> SetupPermissions (Mock<IGuildUser> mock,
            IGuildChannel? channel, ChannelPermission permissions)
        {
            if (channel == null) throw new NullReferenceException();
            mock.Setup(m => m.GetPermissions(channel))
                .Returns(new ChannelPermissions((ulong)permissions));
            return mock;
        }
        private IGuildChannel SetupChannel()
        {
            var mock = new Mock<IGuildChannel>();
            mock.SetupGet(m => m.ChannelType).Returns(ChannelType.Text);
            return mock.Object;
        }
    }
}
