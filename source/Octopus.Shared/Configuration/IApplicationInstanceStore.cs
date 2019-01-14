using System;
using System.Collections.Generic;

namespace Octopus.Shared.Configuration
{
    public interface IApplicationInstanceStore
    {
        IList<ApplicationInstanceRecord> ListInstances(ApplicationName name);
        void SaveInstance(ApplicationInstanceRecord instanceRecord);
        void DeleteInstance(ApplicationInstanceRecord instanceRecord);
    }
}