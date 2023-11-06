using System;
using System.Text;
using Octopus.Tentacle.Tests.Integration.Support.Legacy;

namespace Octopus.Tentacle.Tests.Integration.Support
{
    public class TentacleConfigurationTestCase
    {
        public TentacleType TentacleType { get; }
        public TentacleRuntime TentacleRuntime { get; }
        public Version? Version { get; }
        /// <summary>
        /// An array of <see cref="Type">Types</see> of the latest script service available on the tentacle version
        /// </summary>
        public Type[] LatestScriptServiceTypes { get; }

        public TentacleConfigurationTestCase(
            TentacleType tentacleType,
            TentacleRuntime tentacleRuntime,
            Version? version,
            Type[] latestScriptServiceTypes)
        {
            TentacleType = tentacleType;
            TentacleRuntime = tentacleRuntime;
            Version = version;
            LatestScriptServiceTypes = latestScriptServiceTypes;
        }

        internal ClientAndTentacleBuilder CreateBuilder()
        {
            return new ClientAndTentacleBuilder(TentacleType)
                .WithTentacleVersion(Version)
                .WithTentacleRuntime(TentacleRuntime);
        }

        internal LegacyClientAndTentacleBuilder CreateLegacyBuilder()
        {
            return new LegacyClientAndTentacleBuilder(TentacleType)
                .WithTentacleVersion(Version)
                .WithTentacleRuntime(TentacleRuntime);
        }

        public override string ToString()
        {
            StringBuilder builder = new();

            builder.Append($"{TentacleType},");
            string version = Version?.ToString() ?? "Latest";
            builder.Append($"{version}");

            var tentacleRuntimeDescription = TentacleRuntime.GetDescription();
            var currentRuntime = RuntimeDetection.GetCurrentRuntime();
            if (tentacleRuntimeDescription != currentRuntime)
            {
                builder.Append($",Cl:{currentRuntime},Svc:{tentacleRuntimeDescription}");
            }

            return builder.ToString();
        }
    }
}
