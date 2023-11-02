using System;
using Octopus.Tentacle.Client.Retries;
using Octopus.Tentacle.Contracts.Observability;

namespace Octopus.Tentacle.Client.Execution
{
    internal static class RpcCallExecutorFactory
    {
        internal static RpcCallExecutor Create(TimeSpan retryDuration, ITentacleClientObserver tentacleClientObserver)
        {
            var rpcCallRetryHandler = new RpcCallRetryHandler(retryDuration);
            var rpcCallNoRetriesHandler = new RpcCallNoRetriesHandler();
            var rpcCallExecutor = new RpcCallExecutor(rpcCallRetryHandler, rpcCallNoRetriesHandler, tentacleClientObserver);

            return rpcCallExecutor;
        }
    }
}
