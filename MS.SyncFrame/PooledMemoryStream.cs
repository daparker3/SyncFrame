//-----------------------------------------------------------------------
// <copyright file="PooledMemoryStream.cs" company="MS">
//     Copyright (c) 2016 MS
// </copyright>
//-----------------------------------------------------------------------
namespace MS.SyncFrame
{
    using System;
    using System.Diagnostics.Contracts;
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
            Contract.Requires(manager != null);
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
                Contract.Requires(value >= 0);
                Contract.Requires(value < this.Length);
                this.position = value;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Contract.Requires(buffer != null);
            Contract.Requires(offset > 0);
            Contract.Requires(offset < buffer.Length);
            Contract.Requires(count < buffer.Length - offset);
            if (count > 0)
            {
                Contract.Ensures(this.Position > 0);
            }

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
            Contract.Ensures(this.Position >= 0);
            Contract.Ensures(this.Position < this.Length);
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
                    Contract.Assert(false);
                    break;
            }

            this.Position = newPos;
            return this.Position;
        }

        public override void SetLength(long value)
        {
            Contract.Requires(value >= 0);
            Contract.Ensures(this.Length == value);
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
            Contract.Requires(buffer != null);
            Contract.Requires(offset > 0);
            Contract.Requires(offset < buffer.Length);
            Contract.Requires(count < buffer.Length - offset);
            if (count > 0)
            {
                Contract.Ensures(this.Position > 0);
            }

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
            Contract.Ensures(this.disposed == true);
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
