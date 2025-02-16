﻿using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Octopus.Tentacle.Tests.Integration.Util.Builders.Decorators.Proxies
{
    public class MethodUsageProxyDecorator : ServiceProxy
    {
        MethodUsages usages;

        void Configure(MethodUsages usages)
        {
            this.usages = usages;
        }

        public static TService Create<TService>(TService targetService, IRecordedMethodUsages usages) where TService : class
        {
            var proxiedService = DispatchProxyAsync.Create<TService, MethodUsageProxyDecorator>();
            var proxy = (proxiedService as MethodUsageProxyDecorator)!;
            proxy!.SetTargetService(targetService);
            proxy!.Configure((MethodUsages)usages);

            return proxiedService;
        }

        protected override void OnStartingInvocation(MethodInfo targetMethod)
        {
            usages.RecordCallStart(targetMethod);
        }

        protected override Task OnStartingInvocationAsync(MethodInfo targetMethod)
        {
            usages.RecordCallStart(targetMethod);
            return Task.CompletedTask;
        }

        protected override void OnCompletingInvocation(MethodInfo targetMethod)
        {
            usages.RecordCallComplete(targetMethod);
        }

        protected override Task OnCompletingInvocationAsync(MethodInfo targetMethod)
        {
            usages.RecordCallComplete(targetMethod);
            return Task.CompletedTask;
        }

        protected override void OnInvocationException(MethodInfo targetMethod, Exception exception)
        {
            usages.RecordCallException(targetMethod, exception);
        }
    }
}