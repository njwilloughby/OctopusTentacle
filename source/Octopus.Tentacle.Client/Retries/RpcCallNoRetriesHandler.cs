using System;
using System.Threading;
using System.Threading.Tasks;

namespace Octopus.Tentacle.Client.Retries
{
    public class RpcCallNoRetriesHandler
    {
        public async Task ExecuteWithNoRetries(
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken)
        {
            await action(cancellationToken);
        }

        public async Task<T> ExecuteWithNoRetries<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken)
        {
            return await action(cancellationToken);
        }
    }
}
