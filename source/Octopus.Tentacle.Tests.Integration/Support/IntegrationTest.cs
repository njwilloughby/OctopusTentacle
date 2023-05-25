﻿using System;
using System.Threading;
using NUnit.Framework;
using Octopus.Tentacle.Tests.Integration.Util;
using Serilog;

namespace Octopus.Tentacle.Tests.Integration.Support
{
    public abstract class IntegrationTest
    {
        CancellationTokenSource? cancellationTokenSource;
        public CancellationToken CancellationToken { get; private set; }
        public ILogger Logger { get; private set; } = null!;

        [SetUp]
        public void SetUp()
        {
            cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            CancellationToken = cancellationTokenSource.Token;
            Logger = new SerilogLoggerBuilder().Build();
        }

        [TearDown]
        public void TearDown()
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }
        }
    }
}
