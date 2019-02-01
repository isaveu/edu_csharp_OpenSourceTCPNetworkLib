using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace BytesMemoryPool
{
    public class PoolArrayPool
    {
        int BufferCount = 0;
        int NumBytes;
        ArrayPool<byte> Pool;
        int BufferSize;


        public PoolArrayPool(int bufferCount, int bufferSize)
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
            Pool = ArrayPool<byte>.Create(BufferSize, 1);

            var temp = new List<byte[]>();
            for (int i = 0; i < BufferCount; ++i)
            {
                temp.Add(Pool.Rent(BufferSize));
            }

            for (int i = 0; i < BufferCount; ++i)
            {
                Pool.Return(temp[i]);
            }
        }

        /// <summary>
        /// Assigns a buffer from the buffer pool to the specified SocketAsyncEventArgs object
        /// </summary>
        /// <returns>true if the buffer was successfully set, else false</returns>
        public byte[] SetBuffer()
        {
            return Pool.Rent(BufferSize);
        }

        /// <summary>
        /// Removes the buffer from a SocketAsyncEventArg object.  This frees the buffer back to the 
        /// buffer pool
        /// </summary>
        public void FreeBuffer(byte[] buf)
        {
            Pool.Return(buf);
        }
    }
}
