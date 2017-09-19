using System;
using System.Diagnostics;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Fabric.Description;

namespace Actor2
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                //ActorRuntime.RegisterActorAsync<MapperActor>((context, actorType) => new MyActorService(context, actorType)).GetAwaiter().GetResult();
                //ActorRuntime.RegisterActorAsync<ReducerActor>((context, actorType) => new MyActorService(context, actorType)).GetAwaiter().GetResult();

                ActorRuntime.RegisterActorAsync<ImageRecognizerActor>((context, actorType) => new MyActorService(context, actorType)).GetAwaiter().GetResult();
                ActorRuntime.RegisterActorAsync<ConfiguratorActor>().GetAwaiter().GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }

    class MyActorService : ActorService
    {
        public ImageRecognizerLib.CNTKImageRecognizerWrappper recognizer;
        public bool modelLoadSucceeded;

        public object lockObj = new object();

        void Init()
        {
            var modelFile = @"\\scratch2\scratch\arturl\CNTK-models\ResNet18_ImageNet_CNTK.model";
            var classifierFile = @"\\scratch2\scratch\arturl\CNTK-models\imagenet1000_clsid.txt";
            recognizer = new ImageRecognizerLib.CNTKImageRecognizerWrappper(modelFile, classifierFile);
        }

        public MyActorService(StatefulServiceContext context, ActorTypeInformation typeInfo, Func<ActorService, ActorId, ActorBase> newActor = null)
            : base(context, typeInfo, newActor)
        {
            modelLoadSucceeded = false;
            try
            {
                lock (lockObj)
                {
                    Init();
                }
            }
            catch
            {
                return;
            }
            modelLoadSucceeded = true;
        }

        protected async override Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);
        }

#if false
        private async Task<string> TestMapReduce()
        {
            int forkIndex = 0;
            string name = "c:\\test\\test.txt";

            var reducer = ActorProxy.Create<IReducerActor>(new ActorId(name));

            List<Task> forks = new List<Task>();
            using (var fs = new FileStream(name, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            using (var reader = new StreamReader(fs))
            {
                var sb = new StringBuilder();
                while (!reader.EndOfStream)
                {
                    for (int i = 0; i < 100 && !reader.EndOfStream; i++)
                    {
                        var line = await reader.ReadLineAsync();
                        sb.AppendLine(line);

                    }

                    Task subProcess = ProcessSubResultAsync(name, forkIndex++, reducer, sb.ToString());
                    forks.Add(subProcess);
                }
            }

            await Task.WhenAll(forks);
            var result = await reducer.GetResult();
            return result.ToString();
        }

        private async Task ProcessSubResultAsync(
            string idSuffix,
            int index,
            IReducerActor proxy,
            string partialData)
        {
            var id = new ActorId($"{idSuffix}=>{index}");
            var mapper = ActorProxy.Create<IMapperActor>(id);
            Pairs subResult = await mapper.MapAsync(partialData);
            await proxy.Reduce(subResult);
        }
#endif
#if false
        private void TestFib()
        {
            var tasks = new List<Task>();

            for (int i = 0; i < 16; ++i)
            {
                var proxy = ActorProxy.Create<INumberCruncher>(new ActorId(i));
                var task = proxy.GetFibonacciNumberAsync(42);
                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());
        }
#endif
    }
}
