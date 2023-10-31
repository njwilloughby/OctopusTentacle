﻿using System;
using System.Collections.Generic;
using Halibut.Util;
using Octopus.Tentacle.Client.Retries;

namespace Octopus.Tentacle.Client
{
    public class TentacleClientOptions
    {
        public RpcRetrySettings RpcRetrySettings { get; }

        //This is internal as we retrieve it from the HalibutRuntime inside TentacleClient (however it'll be being removed soon)
        internal AsyncHalibutFeature AsyncHalibutFeature { get; set; }

        /// <summary>
        /// Disables the use of ScriptServiceV3Alpha, even if it's supported on the Tentacle
        /// </summary>
        public bool DisableScriptServiceV3Alpha { get; set; }

        public TentacleClientOptions(RpcRetrySettings rpcRetrySettings)
        {
            RpcRetrySettings = rpcRetrySettings;
        }
    }
}