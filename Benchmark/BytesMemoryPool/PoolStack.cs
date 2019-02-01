using System;
using System.Collections.Generic;
using System.Text;

namespace BytesMemoryPool
{
    public class PoolStack
    {
        int BufferCount = 0;
        int NumBytes;
        byte[] Buffer;
        Stack<int> FreeIndexPool = new Stack<int>();
        int BufferSize;

        object LockObj = new object();


        public PoolStack(int bufferCount, int bufferSize)
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

            for(int i = 0; i < BufferCount; ++i)
            {
                FreeIndexPool.Push(i);
            }
        }

        /// <summary>
        /// Assigns a buffer from the buffer pool to the specified SocketAsyncEventArgs object
        /// </summary>
        /// <returns>true if the buffer was successfully set, else false</returns>
        public (bool, int) SetBuffer()
        {
            var result = false;
            int index = -1;

            lock (LockObj)

            if (FreeIndexPool.Count < 1)
            {
                return (result, index);
            }                
            index = FreeIndexPool.Pop();
            return (result, index);
        }

        /// <summary>
        /// Removes the buffer from a SocketAsyncEventArg object.  This frees the buffer back to the 
        /// buffer pool
        /// </summary>
        public void FreeBuffer(int index)
        {
            lock (LockObj)

            FreeIndexPool.Push(index);            
        }

       
    }
}
