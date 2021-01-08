using System;
using System.Linq;
using Octopus.Diagnostics;

namespace Octopus.Shared.Diagnostics
{
    public class SystemLog : Log, ISystemLog
    {
        public SystemLog(string[]? sensitiveValues = null) : base("system/" + Environment.MachineName, sensitiveValues)
        {
        }

        public ISystemLog ChildContext(string[] sensitiveValues)
        {
            // creates a child context that will mask the given values.
            return new SystemLog(SensitiveValueMasker.SensitiveValues.Concat(sensitiveValues).ToArray());
        }
    }
}