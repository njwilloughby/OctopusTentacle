using System;
using System.Threading.Tasks;
using Halibut.ServiceModel;
using Octopus.Tentacle.Contracts.ClientServices;
using Octopus.Tentacle.Contracts.ScriptServiceV3Alpha;

namespace Octopus.Tentacle.Client.Decorators
{
    class ScriptServiceV3AlphaHalibutExceptionTentacleServiceDecorator : HalibutExceptionTentacleServiceDecorator, IAsyncClientScriptServiceV3Alpha
    {
        readonly IAsyncClientScriptServiceV3Alpha service;

        public ScriptServiceV3AlphaHalibutExceptionTentacleServiceDecorator(IAsyncClientScriptServiceV3Alpha service)
        {
            this.service = service;
        }

        public async Task<ScriptStatusResponseV3Alpha> StartScriptAsync(StartScriptCommandV3Alpha command, HalibutProxyRequestOptions proxyRequestOptions)
        {
            return await HandleCancellationException(async () =>  await service.StartScriptAsync(command, proxyRequestOptions));
        }

        public async Task<ScriptStatusResponseV3Alpha> GetStatusAsync(ScriptStatusRequestV3Alpha request, HalibutProxyRequestOptions proxyRequestOptions)
        {
            return await HandleCancellationException(async () => await service.GetStatusAsync(request, proxyRequestOptions));
        }

        public async Task<ScriptStatusResponseV3Alpha> CancelScriptAsync(CancelScriptCommandV3Alpha command, HalibutProxyRequestOptions proxyRequestOptions)
        {
            return await HandleCancellationException(async () =>  await service.CancelScriptAsync(command, proxyRequestOptions));
        }

        public async Task CompleteScriptAsync(CompleteScriptCommandV3Alpha command, HalibutProxyRequestOptions proxyRequestOptions)
        {
            await HandleCancellationException(async () =>  await service.CompleteScriptAsync(command, proxyRequestOptions));
        }
    }
}