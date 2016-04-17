//-----------------------------------------------------------------------
// <copyright file="PooledMemoryStreamManager.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------
namespace MS.SyncFrame
{
    using System;
    using System.Collections.Concurrent;

    internal class PooledMemoryStreamManager
    {
        private ConcurrentBag<ArraySegment<byte>> availableMemory = new ConcurrentBag<ArraySegment<byte>>();

        internal PooledMemoryStreamManager()
        {
            this.NewSegmentSize = 1 << 20;
        }

        internal int NewSegmentSize
        {
            get;
            set;
        }

        internal PooledMemoryStream CreateStream()
        {
            return new PooledMemoryStream(this);
        }

        internal ArraySegment<byte> AllocateMemory(long countBytes)
        {
            ArraySegment<byte> cur;
            if (this.availableMemory.TryTake(out cur))
            {
                if (countBytes <= cur.Count)
                {
                    return this.TakeMemory(cur, countBytes);
                }
            }

            long needed = this.NewSegmentSize;
            if (countBytes > needed)
            {
                needed = countBytes;
            }

            return this.TakeMemory(new ArraySegment<byte>(new byte[needed]), countBytes);
        }

        internal void FreeMemory(ArraySegment<byte> memory)
        {
            this.availableMemory.Add(memory);
        }

        internal void Flush()
        {
            // Rather than looping through the available memory, just reset the bag.
            this.availableMemory = new ConcurrentBag<ArraySegment<byte>>();
        }

        private ArraySegment<byte> TakeMemory(ArraySegment<byte> cur, long countBytes)
        {
            // Take off N next bytes from cur.
            if (countBytes < cur.Count)
            {
                this.availableMemory.Add(new ArraySegment<byte>(cur.Array, cur.Offset + (int)countBytes, cur.Count - (int)countBytes));
            }

            return new ArraySegment<byte>(cur.Array, cur.Offset, (int)countBytes);
        }
    }
}
