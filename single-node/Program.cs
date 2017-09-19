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
using System.Diagnostics;

namespace SingleNodeImageRecognizer
{
    class Program
    {
        static ImageRecognizerLib.CNTKImageRecognizerWrappper recognizer;
        static DateTime lastFrameProcessed;

        static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();

            string videoFile =
                @"c:\test\SampleVideo_1280x720_10mb.mp4"; // big buck bunny
                // @"c:\test\FroggerHighway.mp4"
                //@"c:\test\desk.mp4";
                //@"c:\test\office.mp4";

            var capture = new Capture(videoFile);
            Mat sourceMat = null;
            Mat frame = new Mat();

            var modelFile = @"\\scratch2\scratch\arturl\CNTK-models\ResNet18_ImageNet_CNTK.model";
            var classifierFile = @"\\scratch2\scratch\arturl\CNTK-models\imagenet1000_clsid.txt";

            recognizer = new ImageRecognizerLib.CNTKImageRecognizerWrappper(modelFile, classifierFile);

            lastFrameProcessed = DateTime.Now;
            int frameCount = 0;
            while (true)
            {
                sourceMat = capture.QueryFrame();
                if (sourceMat == null) break;
                ProcessFrame(sourceMat, frameCount++);
            }

            var elapsed = sw.Elapsed;
            Console.WriteLine($"elapsed time: {elapsed.TotalSeconds}");

        }

        static void ProcessFrame(Mat frame, int frameCount)
        {
            CvInvoke.Resize(frame, frame, new Size((int)recognizer.GetRequiredWidth(), (int)recognizer.GetRequiredHeight()), 0, 0, Inter.Linear);

            var size = frame.Width * frame.Height * frame.ElementSize;
            byte[] dataArray = new byte[size];
            Marshal.Copy(frame.DataPointer, dataArray, 0, size);

            var objectName = recognizer.RecognizeObject(dataArray);

            var millisecondsElapsed = (DateTime.Now - lastFrameProcessed).TotalMilliseconds;
            lastFrameProcessed = DateTime.Now;
            var fps = (int)(1000.0 / millisecondsElapsed);

            Console.WriteLine($"{frameCount}: {fps} FPS --> {objectName}");
        }
    }
}
