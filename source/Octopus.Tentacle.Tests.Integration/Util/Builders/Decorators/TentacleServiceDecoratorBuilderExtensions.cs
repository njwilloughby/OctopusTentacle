using Octopus.Tentacle.Tests.Integration.Util.Builders.Decorators.Proxies;

namespace Octopus.Tentacle.Tests.Integration.Util.Builders.Decorators
{
    public static class TentacleServiceDecoratorBuilderExtensions
    {
        public static TentacleServiceDecoratorBuilder RecordMethodUsages<TService>(this TentacleServiceDecoratorBuilder builder, out IRecordedMethodUsages recordedUsages)
            where TService : class
        {
            var localTracingStats = new MethodUsages();
            recordedUsages = localTracingStats;

            return builder.RegisterProxyDecorator<TService>(service => MethodUsageProxyDecorator.Create(service, localTracingStats));
        }

        public static TentacleServiceDecoratorBuilder TraceService(this TentacleServiceDecoratorBuilder builder, IEnumerable<Type> latestScriptServiceTypes, out IRecordedMethodTracingStats recordedTracingStats)
        {
            var localTracingStats = new MethodTracingStats();
            recordedTracingStats = localTracingStats;

            foreach (var scriptServiceType in latestScriptServiceTypes)
            {
                builder.RegisterProxyDecorator(scriptServiceType, service => MethodTracingProxyDecorator.Create(scriptServiceType, service, localTracingStats));
            }

            return builder;
        }

        public static TentacleServiceDecoratorBuilder HookServiceMethod<TService>(this TentacleServiceDecoratorBuilder builder, string methodName, MethodInvocationHook<TService>? preInvocation) where TService : class
            => HookServiceMethod(builder, methodName, preInvocation, null);

        public static TentacleServiceDecoratorBuilder HookServiceMethod<TService>(this TentacleServiceDecoratorBuilder builder, string methodName, MethodInvocationHook<TService>? preInvocation, MethodInvocationHook<TService>? postInvocation) where TService : class
        {
            return builder.RegisterProxyDecorator<TService>(service => MethodInvocationHookProxyDecorator<TService>.Create(service, methodName, preInvocation, postInvocation));
        }
    }
}