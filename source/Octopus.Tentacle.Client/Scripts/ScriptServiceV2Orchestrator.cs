using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Halibut;
using Halibut.ServiceModel;
using Octopus.Tentacle.Client.Execution;
using Octopus.Tentacle.Client.Observability;
using Octopus.Tentacle.Contracts;
using Octopus.Tentacle.Contracts.ClientServices;
using Octopus.Tentacle.Contracts.Observability;
using Octopus.Tentacle.Contracts.ScriptServiceV2;
using ILog = Octopus.Diagnostics.ILog;

namespace Octopus.Tentacle.Client.Scripts
{
    class ScriptServiceV2Orchestrator : ObservingScriptOrchestrator<StartScriptCommandV2, ScriptStatusResponseV2>
    {
        readonly IAsyncClientScriptServiceV2 clientScriptServiceV2;
        readonly RpcCallExecutor rpcCallExecutor;
        readonly ClientOperationMetricsBuilder clientOperationMetricsBuilder;
        readonly TimeSpan onCancellationAbandonCompleteScriptAfter;
        readonly ILog logger;

        public ScriptServiceV2Orchestrator(
            IAsyncClientScriptServiceV2 clientScriptServiceV2,
            IScriptObserverBackoffStrategy scriptObserverBackOffStrategy,
            RpcCallExecutor rpcCallExecutor,
            ClientOperationMetricsBuilder clientOperationMetricsBuilder,
            OnScriptStatusResponseReceived onScriptStatusResponseReceived,
            OnScriptCompleted onScriptCompleted,
            TimeSpan onCancellationAbandonCompleteScriptAfter,
            TentacleClientOptions clientOptions,
            ILog logger)
            : base(scriptObserverBackOffStrategy,
                onScriptStatusResponseReceived,
                onScriptCompleted,
                clientOptions)
        {
            this.clientScriptServiceV2 = clientScriptServiceV2;
            this.rpcCallExecutor = rpcCallExecutor;
            this.clientOperationMetricsBuilder = clientOperationMetricsBuilder;
            this.onCancellationAbandonCompleteScriptAfter = onCancellationAbandonCompleteScriptAfter;
            this.logger = logger;
        }

        protected override StartScriptCommandV2 Map(StartScriptCommandV2 command)
        {
            return command;
        }

        protected override ScriptExecutionStatus MapToStatus(ScriptStatusResponseV2 response)
        {
            return new ScriptExecutionStatus(response.Logs);
        }

        protected override ScriptExecutionResult MapToResult(ScriptStatusResponseV2 response)
        {
            return new ScriptExecutionResult(response.State, response.ExitCode);
        }

        protected override ProcessState GetState(ScriptStatusResponseV2 response)
        {
            return response.State;
        }

        protected override async Task<ScriptStatusResponseV2> StartScript(StartScriptCommandV2 command, CancellationToken scriptExecutionCancellationToken)
        {
            ScriptStatusResponseV2 scriptStatusResponse;
            var startScriptCallCount = 0;
            try
            {
                async Task<ScriptStatusResponseV2> StartScriptAction(CancellationToken ct)
                {
                    ++startScriptCallCount;

                    var result = await clientScriptServiceV2.StartScriptAsync(command, new HalibutProxyRequestOptions(ct, ct));

                    return result;
                }

                // If we are cancelling script execution we can abandon a call to start script
                // If we manage to cancel the start script call we can walk away
                // If we do abandon the start script call we have to assume the script is running so need
                // to call CancelScript and CompleteScript
                scriptStatusResponse = await rpcCallExecutor.Execute(
                    retriesEnabled: ClientOptions.RpcRetrySettings.RetriesEnabled,
                    RpcCall.Create<IScriptServiceV2>(nameof(IScriptServiceV2.StartScript)),
                    StartScriptAction,
                    logger,
                    clientOperationMetricsBuilder,
                    scriptExecutionCancellationToken);
            }
            catch (Exception e) when (
                (e is OperationCanceledException || IsHalibutWrappedOperationCancelledException(e as HalibutClientException)) && 
                scriptExecutionCancellationToken.IsCancellationRequested)
            {
                // If we are not retrying and we managed to cancel execution while connecting it means the request was never sent so we can safely walk away from it.                
                if (!IsHalibutWrappedOperationCancelledException(e as HalibutClientException) && startScriptCallCount <= 1)
                {
                    throw;
                }

                // Otherwise we have to assume the script started executing and call CancelScript and CompleteScript
                // We don't have a response so we need to create one to continue the execution flow
                scriptStatusResponse = new ScriptStatusResponseV2(
                    command.ScriptTicket,
                    ProcessState.Pending,
                    ScriptExitCodes.RunningExitCode,
                    new List<ProcessOutput>(),
                    0);

                await ObserveUntilCompleteThenFinish(scriptStatusResponse, scriptExecutionCancellationToken);

                // Throw an error so the caller knows that execution of the script was cancelled
                throw new OperationCanceledException("Script execution was cancelled");
            }

            return scriptStatusResponse;
        }

        static readonly Regex OperationCancelledRegex = new("The [a-zA-Z]* operation was cancelled", RegexOptions.Compiled);

        static bool IsHalibutWrappedOperationCancelledException(HalibutClientException? ex)
        {
            if (ex is null)
            {
                return false;
            }

            if (OperationCancelledRegex.IsMatch(ex.Message))
            {
                return true;
            }

            return false;
        }

        protected override async Task<ScriptStatusResponseV2> GetStatus(ScriptStatusResponseV2 lastStatusResponse, CancellationToken scriptExecutionCancellationToken)
        {
            try
            {
                async Task<ScriptStatusResponseV2> GetStatusAction(CancellationToken ct)
                {
                    var request = new ScriptStatusRequestV2(lastStatusResponse.Ticket, lastStatusResponse.NextLogSequence);
                    var result = await clientScriptServiceV2.GetStatusAsync(request, new HalibutProxyRequestOptions(ct, ct));

                    return result;
                }

                return await rpcCallExecutor.Execute(
                    retriesEnabled: ClientOptions.RpcRetrySettings.RetriesEnabled,
                    RpcCall.Create<IScriptServiceV2>(nameof(IScriptServiceV2.GetStatus)),
                    GetStatusAction,
                    logger,
                    clientOperationMetricsBuilder,
                    scriptExecutionCancellationToken);
            }
            catch (Exception e) when (e is OperationCanceledException && scriptExecutionCancellationToken.IsCancellationRequested)
            {
                // Return the last known response without logs when cancellation occurs and let the script execution go into the CancelScript and CompleteScript flow
                return new ScriptStatusResponseV2(lastStatusResponse.Ticket, lastStatusResponse.State, lastStatusResponse.ExitCode, new List<ProcessOutput>(), lastStatusResponse.NextLogSequence);
            }
        }

        protected override async Task<ScriptStatusResponseV2> Cancel(ScriptStatusResponseV2 lastStatusResponse, CancellationToken scriptExecutionCancellationToken)
        {
            async Task<ScriptStatusResponseV2> CancelScriptAction(CancellationToken ct)
            {
                var request = new CancelScriptCommandV2(lastStatusResponse.Ticket, lastStatusResponse.NextLogSequence);
                var result = await clientScriptServiceV2.CancelScriptAsync(request, new HalibutProxyRequestOptions(ct, ct));

                return result;
            }

            // TODO: SaST - This could be optimized for the failure scenario.
            // If script execution is already triggering RPC Retries and then the script execution is cancelled there is a high chance that the cancel RPC call will fail as well and go into RPC retries.
            // We could potentially reduce the time to failure by not retrying the cancel RPC Call if the previous RPC call was already triggering RPC Retries.

            return await rpcCallExecutor.Execute(
                retriesEnabled: ClientOptions.RpcRetrySettings.RetriesEnabled,
                RpcCall.Create<IScriptServiceV2>(nameof(IScriptServiceV2.CancelScript)),
                CancelScriptAction,
                logger,
                clientOperationMetricsBuilder,
                // We don't want to cancel this operation as it is responsible for stopping the script executing on the Tentacle
                CancellationToken.None);
        }

        protected override async Task<ScriptStatusResponseV2> Finish(ScriptStatusResponseV2 lastStatusResponse, CancellationToken scriptExecutionCancellationToken)
        {
            try
            {
                // Finish performs a best effort cleanup of the Workspace on Tentacle
                // If we are cancelling script execution we abandon a call to complete script after a period of time
                using var completeScriptCancellationTokenSource = new CancellationTokenSource(onCancellationAbandonCompleteScriptAfter);

                await rpcCallExecutor.ExecuteWithNoRetries(
                        RpcCall.Create<IScriptServiceV2>(nameof(IScriptServiceV2.CompleteScript)),
                        async ct =>
                        {
                            var request = new CompleteScriptCommandV2(lastStatusResponse.Ticket);
                            await clientScriptServiceV2.CompleteScriptAsync(request, new HalibutProxyRequestOptions(ct, ct));
                        },
                        logger,
                        clientOperationMetricsBuilder,
                        completeScriptCancellationTokenSource.Token); 
            }
            catch (Exception ex) when (ex is HalibutClientException or OperationCanceledException)
            {
                logger.Warn("Failed to cleanup the script working directory on Tentacle");
                logger.Verbose(ex);
            }

            return lastStatusResponse;
        }
    }
}