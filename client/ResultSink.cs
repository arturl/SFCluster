using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

interface IResultSink<T>
{
    void Accept(int key, T value);
}

/// <summary>
/// Spits out results as they arrive
/// </summary>
/// <typeparam name="T">accepted input type</typeparam>
class BasicResultSink<T> : IResultSink<T>
{
    Action<int,T> action;

    public BasicResultSink(Action<int,T> action)
    {
        this.action = action;
    }

    public void Accept(int key, T value)
    {
        action(key,value);
    }
}

/// <summary>
/// Buffers the input and produces sorted output
/// </summary>
/// <typeparam name="T">accepted input type</typeparam>
class SortedResultSink<T> : IResultSink<T>
{
    int min = 0;
    object resultSinkLock = new object();
    SortedList<int, T> list = new SortedList<int, T>();
    Action<int, T> action;

    public SortedResultSink(Action<int,T> action)
    {
        this.action = action;
    }

    public void Accept(int key, T value)
    {
        lock (resultSinkLock)
        {
            if (!list.Any())
            {
                // Empty list
                if (key == min)
                {
                    action(key, value);
                    min++;
                }
                else
                {
                    // Need to wait for min
                    list.Add(key, value);
                }
            }
            else
            {
                // list not empty
                if (key == min)
                {
                    action(key, value);
                    int i;
                    for (i = min + 1; list.ContainsKey(i); ++i)
                    {
                        action(i, list[i]);
                        list.Remove(i);
                    }
                    min = i;
                }
                else
                {
                    list.Add(key, value);
                }
            }
        }
    }
}
