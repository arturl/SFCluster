using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.ServiceFabric.Data;

namespace Actor2
{
    public interface IConfigurator : IActor
    {
        Task SetLoggingLevel(int loggingLevel);
        Task<int> GetLoggingLevel();
    }

    internal class ConfiguratorActor : Actor, IConfigurator
    {
        public ConfiguratorActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public async Task SetLoggingLevel(int loggingLevel)
        {
            await this.StateManager.SetStateAsync<int>("loggingLevel", loggingLevel);
        }

        public async Task<int> GetLoggingLevel()
        {
            var result = await this.StateManager.GetStateAsync<int>("loggingLevel");
            return result;
        }
    }
}
