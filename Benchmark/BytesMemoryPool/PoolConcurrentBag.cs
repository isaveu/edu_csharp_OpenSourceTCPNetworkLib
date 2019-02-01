using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace BytesMemoryPool
{
    public class PoolConcurrentBag
    {
        int BufferCount = 0;
        int NumBytes;
        byte[] Buffer;
        ConcurrentBag<int> FreeIndexPool = new ConcurrentBag<int>();
        int BufferSize;

       
        public PoolConcurrentBag(int bufferCount, int bufferSize)
        {
            BufferCount = bufferCount;
            NumBytes = bufferCount * bufferSize;
            BufferSize = bufferSize;
        }

        /// <summary>
        /// Allocates buffer space used by the buffer pool
        /// </summary>
        public void InitBuffer()
        {
            // create one big large buffer and divide that out to each SocketAsyncEventArg object
            Buffer = new byte[NumBytes];

            for (int i = 0; i < BufferCount; ++i)
            {
                FreeIndexPool.Add(i);
            }
        }

        /// <summary>
        /// Assigns a buffer from the buffer pool to the specified SocketAsyncEventArgs object
        /// </summary>
        /// <returns>true if the buffer was successfully set, else false</returns>
        public (bool, int) SetBuffer()
        {
            var result = false;            
            
            if (FreeIndexPool.TryTake(out int index))
            {
                return (result, index);
            }
            
            return (result, -1);
        }

        /// <summary>
        /// Removes the buffer from a SocketAsyncEventArg object.  This frees the buffer back to the 
        /// buffer pool
        /// </summary>
        public void FreeBuffer(int index)
        {
            FreeIndexPool.Add(index);
        }
    }
}
