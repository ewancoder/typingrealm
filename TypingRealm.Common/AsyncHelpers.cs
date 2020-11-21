using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TypingRealm
{
    public static class AsyncHelpers
    {
        /// <summary>
        /// Waits when all value tasks will complete.
        /// </summary>
        public static ValueTask WhenAll(IEnumerable<ValueTask> valueTasks)
        {
            // TODO: This method enumerates given IEnumerable multiple times so Task methods would be called multiple times,
            // which can lead to bugs.
            // Look at StartupActionRunner for example.
            if (valueTasks.All(vt => vt.IsCompletedSuccessfully))
                return default;

            // We cannot await Task to create ValueTask, because then we lose
            // AggregateException (InnerExceptions).
            return new ValueTask(Task.WhenAll(valueTasks
                .Where(vt => !vt.IsCompletedSuccessfully)
                .Select(vt => vt.AsTask())));
        }
    }
}
