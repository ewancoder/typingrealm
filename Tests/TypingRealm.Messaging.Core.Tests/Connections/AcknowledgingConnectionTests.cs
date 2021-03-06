﻿using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Moq;
using TypingRealm.Messaging.Connections;
using TypingRealm.Testing;
using Xunit;

namespace TypingRealm.Messaging.Tests.Connections
{
    public class AcknowledgingConnectionTests : TestsBase
    {
        [Theory, AutoMoqData]
        public async Task ShouldSendAsUsual(
            object message,
            [Frozen]Mock<IConnection> connection,
            AcknowledgingConnection sut)
        {
            await sut.SendAsync(message, Cts.Token);

            connection.Verify(x => x.SendAsync(message, Cts.Token));
        }

        [Theory, AutoMoqData]
        public async Task ShouldNotSendAcknowledgeReceived_WhenMessageIsNotOfMessageType(
            [Frozen]Mock<IConnection> connection,
            AcknowledgingConnection sut)
        {
            await sut.ReceiveAsync(Cts.Token);

            connection.Verify(x => x.SendAsync(It.IsAny<object>(), Cts.Token), Times.Never);
        }

        [Theory, AutoMoqData]
        public async Task ShouldNotSendAcknowledgeReceived_WhenMessageIdIsNotSet(
            MessagesTests.TestAbstractMessage message,
            [Frozen]Mock<IConnection> connection,
            AcknowledgingConnection sut)
        {
            connection.Setup(x => x.ReceiveAsync(Cts.Token))
                .ReturnsAsync(message);

            message.MessageId = null;

            await sut.ReceiveAsync(Cts.Token);

            connection.Verify(x => x.SendAsync(It.IsAny<object>(), Cts.Token), Times.Never);
        }

        [Theory, AutoMoqData]
        public async Task ShouldSendAcknowledgeReceived_WhenMessageHasId(
            MessagesTests.TestAbstractMessage message,
            [Frozen]Mock<IConnection> connection,
            AcknowledgingConnection sut)
        {
            connection.Setup(x => x.ReceiveAsync(Cts.Token))
                .ReturnsAsync(message);

            await sut.ReceiveAsync(Cts.Token);

            connection.Verify(x => x.SendAsync(
                It.Is<AcknowledgeReceived>(y => y.MessageId == message.MessageId),
                Cts.Token));
        }
    }
}
