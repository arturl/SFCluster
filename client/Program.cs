using Actor2;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientApp
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < 1; ++i)
            {
                var sw = Stopwatch.StartNew();

                Console.WriteLine($"driver running on {Environment.MachineName}");

                TestImageRecognition();

                var elapsed = sw.Elapsed;

                Console.WriteLine($"elapsed time: {elapsed.TotalSeconds}");
            }
        }

        private static void TestImageRecognition()
        {
            ImageRecognitionDriver.Run().Wait();
        }
    }
}
