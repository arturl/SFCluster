using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.Structure;
using System.Drawing;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Runtime.InteropServices;
using Microsoft.ServiceFabric.Actors.Client;
using Actor2;
using Microsoft.ServiceFabric.Actors;
using System.Threading;

namespace ClientApp
{
    class ImageRecognitionDriver
    {
        static DateTime lastFrameBatchProcessed;
        static int actorCount = 3;

        static object outputLock = new object();

        public static async Task Run()
        {
            string videoFile =
                @"c:\test\SampleVideo_1280x720_10mb.mp4"; // big buck bunny
                // @"c:\test\FroggerHighway.mp4"
                //@"c:\test\desk.mp4";
                //@"c:\test\office.mp4";

            var capture = new Capture(videoFile);
            Mat sourceMat = null;
            Mat frame = new Mat();

            lastFrameBatchProcessed = DateTime.Now;
            var workers = new Task<String>[actorCount];

            var configAgent = ActorProxy.Create<Actor2.IConfigurator>(new ActorId(0), "fabric:/SFActorApp2");
            await configAgent.SetLoggingLevel(1);

            lastFrameBatchProcessed = DateTime.Now;
            frameTimings.Add(DateTime.Now);

            var resultSink = new SortedResultSink<string>(PrintResult);
            var scheduler = new StreamingScheduler(actorCount);

            while (true)
            {
                sourceMat = capture.QueryFrame();
                if (sourceMat == null)
                {
                    scheduler.Complete();
                    break;
                }

                scheduler.PushElement(sourceMat, ProcessFrame, resultSink);
            }
        }

        static string ProcessFrame(int i, Mat frame, Int64 actorID)
        {
            string objectName;
            try
            {
                CvInvoke.Resize(frame, frame, new Size((int)224, (int)224), 0, 0, Inter.Linear);

                var size = frame.Width * frame.Height * frame.ElementSize;
                byte[] dataArray = new byte[size];
                Marshal.Copy(frame.DataPointer, dataArray, 0, size);

                var recoAgent = ActorProxy.Create<IImageRecognizer>(new ActorId(actorID), "fabric:/SFActorApp2");

                objectName = recoAgent.RecognizeImage(dataArray).Result;
            }
            catch
            {
                objectName = "*** error in cluster ***";
            }

            return objectName;
        }

        const int Max_FPS_frames_count = 15; // how many frames to average over when calculating FPS
        static List<DateTime> frameTimings = new List<DateTime>();

        static void PrintResult(int i, string objectName)
        {
            lock (outputLock)
            {
                if (frameTimings.Count == Max_FPS_frames_count)
                {
                    frameTimings.RemoveAt(0);
                }
                frameTimings.Add(DateTime.Now);

                var millisecondsElapsed = (frameTimings[frameTimings.Count - 1] - frameTimings[0]).TotalMilliseconds;

                int fps = (int)(1000.0 * (frameTimings.Count-1) / millisecondsElapsed);

                double ms = 0;
                if(frameTimings.Count > 1)
                {
                    ms = (frameTimings[frameTimings.Count - 1] - frameTimings[frameTimings.Count - 2]).TotalMilliseconds;
                }

                Console.WriteLine($"{i}: {fps} FPS --> {objectName}, {ms} ms");
            }
        }
    }
}