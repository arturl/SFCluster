using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class StreamingScheduler<ActorType> where ActorType : IActor
{
    int processorCount;

    // Taks contain the ID of the worker that will execute the task
    Task<Int64>[] tasks;

    // IDs of the workers, evenly distributed accross Int64 range
    Int64[] workerIDs;

    string applicationName;

    public StreamingScheduler(int workerCount, string applicationName)
    {
        this.processorCount = workerCount;
        this.applicationName = applicationName;

        workerIDs = new Int64[workerCount];

        Int64 current = Int64.MinValue;
        Int64 step = (Int64)(Int64.MaxValue / workerCount - Int64.MinValue / workerCount);
        for (int i = 0; i < workerCount; ++i)
        {
            workerIDs[i] = current + step / 2;
            current += (Int64)step;
        }

        // Initial allocation is round-robin. Subsequent allocation is based on the availability of the resources
        tasks = new Task<Int64>[workerCount];
        for (int i=0; i<tasks.Length; ++i)
        {
            tasks[i] = Task.FromResult(workerIDs[i % workerCount]);
        }
    }

    int elementsProcessed = 0;

    public void PushElement<Input, Output>(Input element, Func<int, Input, ActorType, Output> action, IResultSink<Output> resultSink)
    {
        // Get a free slot in the task array. Wait for one to become available
        int availableSlot = Task.WaitAny(tasks);
        Int64 workerID = tasks[availableSlot].Result;

        var thisElementNumber = elementsProcessed;
        elementsProcessed++;

        tasks[availableSlot] = Task.Run(() =>
        {
            var recoAgent = ActorProxy.Create<ActorType>(new ActorId(workerID), applicationName);
            var result = action(thisElementNumber, element, recoAgent);
            resultSink.Accept(thisElementNumber, result);
            return workerID;
        });
    }

    public void Complete()
    {
        // Join all remaining tasks
        Task.WaitAll(tasks);
        elementsProcessed = 0;
    }

    public void Process<Input, Output>(IEnumerable<Input> input, Func<int, Input, ActorType, Output> action, IResultSink<Output> resultSink)
    {
        foreach(var element in input)
        {
            PushElement(element, action, resultSink);
        }
        Complete();
    }

}