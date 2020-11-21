using System.Threading;
using System.Threading.Tasks;
using TypingRealm.Messaging.Broker.Client;

namespace TypingRealm.Hosting
{
    public sealed class ListenToMessageBrokerStartupAction : IStartupAction
    {
        private readonly IBrokerConnectionListener _listener;

        public ListenToMessageBrokerStartupAction(IBrokerConnectionListener listener)
        {
            _listener = listener;
        }

        public async ValueTask RunAync(CancellationToken cancellationToken)
        {
            await _listener.ListenAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
