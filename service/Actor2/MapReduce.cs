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

#if false

namespace Actor2
{
    [DataContract]
    public class Pair
    {
        public Pair(string key, int value)
        {
            Key = key;
            Value = value;
        }

        [DataMember]
        public readonly string Key;
        [DataMember]
        public int Value { get; set; }

        public override string ToString() => $"{Key}: {Value}";
    }

    public class Pairs : KeyedCollection<string, Pair>
    {
        public Pairs() { }
        public Pairs(IEnumerable<Pair> items)
        {
            foreach (var item in items)
            {
                base.Add(item);
            }
        }

        protected override string GetKeyForItem(Pair item) => item.Key;

        public override string ToString() => string.Join("\r\n", this);
    }

    public interface IMapperActor : IActor
    {
        Task<Pairs> MapAsync(string document);
    }
    public interface IReducerActor : IActor
    {
        Task Reduce(Pairs subResults);
        Task<Pairs> GetResult();
    }

    internal class MapperActor : Actor, IMapperActor
    {
        public MapperActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        public Task<Pairs> MapAsync(string document)
        {
            var items = document
                            .Split(new char[] { ' ', ',', ':', '.', ';', '-' }, StringSplitOptions.RemoveEmptyEntries)
                            .GroupBy(w => w.ToLower(), (key, g) => new Pair(key, g.Count()));

            var result = new Pairs(items);
            return Task.FromResult(result);
        }
    }

    [StatePersistence(StatePersistence.None)]
    internal class ReducerActor : Actor, IReducerActor
    {
        Pairs State;

        public ReducerActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        protected async override Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            if (State == null)
                State = new Pairs();
        }

        public Task<Pairs> GetResult() => Task.FromResult(State);

        public Task Reduce(Pairs subResults)
        {
            foreach (var item in subResults)
            {
                if (State.Contains(item.Key))
                {
                    var val = State[item.Key];
                    val.Value += item.Value;
                }
                else
                    State.Add(item);
            }

            return Task.FromResult(true);
        }
    }
}

#endif