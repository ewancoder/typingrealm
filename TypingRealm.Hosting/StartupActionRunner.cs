using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace TypingRealm.Hosting
{
    public sealed class StartupActionRunner : BackgroundService
    {
        private readonly IEnumerable<IStartupAction> _startupActions;

        public StartupActionRunner(IEnumerable<IStartupAction> startupActions)
        {
            _startupActions = startupActions;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var actions = _startupActions.Select(action => action.RunAync(stoppingToken)).ToList();

            var result = AsyncHelpers.WhenAll(actions);

            if (result.IsCompletedSuccessfully)
                return Task.CompletedTask;

            return result.AsTask();
        }
    }
}
