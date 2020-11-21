using System.Threading;
using System.Threading.Tasks;

namespace TypingRealm.Hosting
{
    public interface IStartupAction
    {
        ValueTask RunAync(CancellationToken cancellationToken);
    }
}
