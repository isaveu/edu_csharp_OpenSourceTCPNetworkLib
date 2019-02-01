using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace BytesMemoryPool
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<PoolTest>();
        }
    }

    public class PoolTest
    {  
        int ThreadCount = 8;
        int DoCount = 8;
        List<List<bool>> AllocFreePattern = new List<List<bool>>();

        public PoolTest()
        {                          
            for(int i = 0; i < ThreadCount; ++i)
            {
                var rnd = new Random();
                AllocFreePattern.Add(new List<bool>());

                for (int j = 0; j < DoCount; ++j)
                {
                    AllocFreePattern[i].Add(rnd.Next(0, 2) == 0);
                }
            }
        }

        [Benchmark]
        public void UsePoolStack()
        {
            PoolStack StackPool = new PoolStack(200, 1000);
            StackPool.InitBuffer();

            var runNum = -1;
            var taskList = new List<Task>();
            for (int i = 0; i < ThreadCount; ++i)
            {                
                var task = Task.Run(() =>
                {
                    var row = Interlocked.Increment(ref runNum);

                    var usedIndexs = new Stack<int>();

                    for(int j = 0; j < DoCount; ++j)
                    {
                        if(AllocFreePattern[row][j])
                        {
                            (_, var index) = StackPool.SetBuffer();
                            usedIndexs.Push(index);
                        }
                        else
                        {
                            if(usedIndexs.Count > 0)
                            {
                                var index = usedIndexs.Pop();
                                StackPool.FreeBuffer(index);
                            }
                        }
                    }
                });

                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());
        }


        [Benchmark]
        public void UsePoolConcurrentBag()
        {
            PoolConcurrentBag ConcurrentPool = new PoolConcurrentBag(200, 1000);
            ConcurrentPool.InitBuffer();

            var runNum = -1;
            var taskList = new List<Task>();
            for (int i = 0; i < ThreadCount; ++i)
            {
                var task = Task.Run(() =>
                {
                    var row = Interlocked.Increment(ref runNum);
                    var usedIndexs = new Stack<int>();

                    for (int j = 0; j < DoCount; ++j)
                    {
                        if (AllocFreePattern[runNum][j])
                        {
                            (_, var index) = ConcurrentPool.SetBuffer();
                            usedIndexs.Push(index);
                        }
                        else
                        {
                            if (usedIndexs.Count > 0)
                            {
                                var index = usedIndexs.Pop();
                                ConcurrentPool.FreeBuffer(index);
                            }
                        }
                    }
                });

                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());
        }


        [Benchmark]
        public void UsePoolArrayPool()
        {
            PoolArrayPool ArrPool = new PoolArrayPool(200, 1000);
            ArrPool.InitBuffer();

            var runNum = -1;
            var taskList = new List<Task>();
            for (int i = 0; i < ThreadCount; ++i)
            {
                var row = Interlocked.Increment(ref runNum);

                var task = Task.Run(() =>
                {
                    var usedIndexs = new Stack<byte[]>();

                    for (int j = 0; j < DoCount; ++j)
                    {
                        if (AllocFreePattern[row][j])
                        {
                            var index= ArrPool.SetBuffer();
                            usedIndexs.Push(index);
                        }
                        else
                        {
                            if (usedIndexs.Count > 0)
                            {
                                var index = usedIndexs.Pop();
                                ArrPool.FreeBuffer(index);
                            }
                        }
                    }
                });

                taskList.Add(task);
            }

            Task.WaitAll(taskList.ToArray());
        }


    }
}
