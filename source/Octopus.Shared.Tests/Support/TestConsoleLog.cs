using System;
using System.Threading;
using Octopus.Diagnostics;
using Octopus.Shared.Diagnostics;

namespace Octopus.Shared.Tests.Support
{
    public class TestConsoleLog : AbstractLog
    {
        public override bool IsEnabled(LogCategory category)
            => true;

        protected override void WriteEvent(LogEvent logEvent)
        {
            Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {logEvent.Category} {logEvent.MessageText}");
            if (logEvent.Error != null) Console.WriteLine(logEvent.Error);
        }

        public override void Flush()
        {
        }
    }
}