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
        static int actorCount = 6;

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

            var workers = new Task<String>[actorCount];

            var configAgent = ActorProxy.Create<Actor2.IConfigurator>(new ActorId(0), "fabric:/SFActorApp2");
            await configAgent.SetLoggingLevel(1);

            lastFrame = DateTime.Now;

            var resultSink = new SortedResultSink<string>(PrintResult);
            var scheduler = new StreamingScheduler<IImageRecognizer>(actorCount, "fabric:/SFActorApp2");

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

        static string ProcessFrame(int i, Mat frame, IImageRecognizer recognizer)
        {
            string objectName;
            try
            {
                CvInvoke.Resize(frame, frame, new Size((int)224, (int)224), 0, 0, Inter.Linear);

                var size = frame.Width * frame.Height * frame.ElementSize;
                byte[] dataArray = new byte[size];
                Marshal.Copy(frame.DataPointer, dataArray, 0, size);

                objectName = recognizer.RecognizeImage(dataArray).Result;
            }
            catch
            {
                objectName = "*** error in cluster ***";
            }

            return objectName;
        }

        const int Max_FPS_frames_count = 10; // how many frames to average over when calculating FPS
        static double averageElapsedTimePerFrame = 0.0;
        static DateTime lastFrame = DateTime.Now;

        static void PrintResult(int i, string objectName)
        {
            var now = DateTime.Now;
            int N = i == 0 ? 1 : 
                    i < Max_FPS_frames_count ? i : Max_FPS_frames_count;

            lock (outputLock)
            {
                averageElapsedTimePerFrame -= averageElapsedTimePerFrame / N;

                var delta = (now - lastFrame).TotalMilliseconds;
                lastFrame = now;
                averageElapsedTimePerFrame += delta / N;

                string fps = "?";
                if (averageElapsedTimePerFrame >= 0)
                {
                    fps = ((int)(1000.0 / averageElapsedTimePerFrame)).ToString();
                }

                Console.WriteLine($"{i}: {fps} FPS --> {objectName}");
            }
        }
    }
}