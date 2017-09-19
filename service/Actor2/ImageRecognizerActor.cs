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

namespace Actor2
{
    public interface IImageRecognizer : IActor
    {
        Task<string> RecognizeImage(byte[] imageData);
    }

    internal class ImageRecognizerActor : Actor, IImageRecognizer
    {
        public ImageRecognizerActor(ActorService actorService, ActorId actorId)
            : base(actorService, actorId)
        {
        }

        async Task<string> RecognizeImageWorker(byte[] imageData, int loggingLevel)
        {
            MyActorService myservice = (MyActorService)this.ActorService;

            if (myservice.modelLoadSucceeded)
            {
                lock (myservice.lockObj)
                {
                    return myservice.recognizer.RecognizeObject(imageData, loggingLevel);
                }
            }
            else
            {
                return "model load failed!";
            }
        }

        public async Task<string> RecognizeImage(byte[] imageData)
        {
            var configAgent = ActorProxy.Create<Actor2.IConfigurator>(new ActorId(0), "fabric:/SFActorApp2");
            int loggingLevel = await configAgent.GetLoggingLevel();

            string result = "";
            try
            {
                result = await RecognizeImageWorker(imageData, loggingLevel);
            }
            catch(Exception ex)
            {
                result = ex.Message;
            }

            switch (loggingLevel)
            {
                case 1:
                    return $"{Environment.MachineName.PadRight(16)} '{result}'";
                case 2:
                    var process = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    return $"'{result}'" + $" (reported by {Environment.MachineName} running on '{process}')";
                case 0:
                default:
                    return result;
            }
            
        }
    }
}
