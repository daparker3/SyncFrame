//-----------------------------------------------------------------------
// <copyright file="PooledMemoryStreamManager.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------
namespace MS.SyncFrame
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Contracts;

    internal class PooledMemoryStreamManager
    {
        private ConcurrentBag<ArraySegment<byte>> availableMemory = new ConcurrentBag<ArraySegment<byte>>();
        private int newSegmentSize = 1 << 20;

        internal int NewSegmentSize
        {
            get
            {
                return this.newSegmentSize;
            }

            set
            {
                Contract.Requires(value > 0);
                Contract.Ensures(this.newSegmentSize == value);
                this.newSegmentSize = value;
            }
        }

        internal PooledMemoryStream CreateStream()
        {
            return new PooledMemoryStream(this);
        }

        internal ArraySegment<byte> AllocateMemory(long countBytes)
        {
            Contract.Requires(countBytes > 0);
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
            Contract.Requires(memory != null);
            this.availableMemory.Add(memory);
        }

        internal void Flush()
        {
            // Rather than looping through the available memory, just reset the bag.
            this.availableMemory = new ConcurrentBag<ArraySegment<byte>>();
        }

        private ArraySegment<byte> TakeMemory(ArraySegment<byte> cur, long countBytes)
        {
            Contract.Requires(cur != null);
            Contract.Requires(countBytes > 0);

            // Take off N next bytes from cur.
            if (countBytes < cur.Count)
            {
                this.availableMemory.Add(new ArraySegment<byte>(cur.Array, cur.Offset + (int)countBytes, cur.Count - (int)countBytes));
            }

            return new ArraySegment<byte>(cur.Array, cur.Offset, (int)countBytes);
        }
    }
}
