//-----------------------------------------------------------------------
// <copyright file="PooledMemoryStream.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------
namespace MS.SyncFrame
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using EnsureThat;

    internal class PooledMemoryStream : Stream
    {
        private PooledMemoryStreamManager manager;
        private ArraySegment<byte> root;
        private long position = 0;
        private bool disposed = false;

        internal PooledMemoryStream(PooledMemoryStreamManager manager)
        {
            this.manager = manager;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        public override long Length
        {
            get
            {
                if (this.root != null)
                {
                    return this.root.Count;
                }

                return 0;
            }
        }

        public override long Position
        {
            get
            {
                return this.position;
            }

            set
            {
                Ensure.That(value, "value").IsGte(0).And().IsLte(this.root.Count);
                this.position = value;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long toRead = this.Length - this.Position;
            if (toRead > count)
            {
                toRead = count;
            }

            Buffer.BlockCopy(this.root.Array, this.root.Offset + (int)this.Position, buffer, offset, (int)toRead);
            this.Position += toRead;
            return (int)toRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPos = -1;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPos = offset;
                    break;
                case SeekOrigin.Current:
                    newPos = this.Position + offset;
                    break;
                case SeekOrigin.End:
                    newPos = this.Length - offset;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            this.Position = newPos;
            return this.Position;
        }

        public override void SetLength(long value)
        {
            ArraySegment<byte> newRoot = this.manager.AllocateMemory(value);
            if (this.root != null)
            {
                int toCopy = this.root.Count;
                if (toCopy > value)
                {
                    toCopy = (int)value;
                }

                Buffer.BlockCopy(this.root.Array, this.root.Offset, newRoot.Array, newRoot.Offset, toCopy);
            }

            this.root = newRoot;
            if (this.Position > this.Length)
            {
                this.Position = this.Length;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            long toWrite = this.Length - this.Position;
            if (toWrite > count)
            {
                toWrite = count;
            }

            Buffer.BlockCopy(buffer, offset, this.root.Array, this.root.Offset + (int)this.Position, (int)toWrite);
            this.Position += toWrite;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!this.disposed)
            {
                this.disposed = true;
                if (disposing)
                {
                    if (this.root != null)
                    {
                        this.manager.FreeMemory(this.root);
                    }
                }
            }
        }
    }
}
