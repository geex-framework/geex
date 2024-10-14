using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MongoDB.Driver;

namespace Geex.MongoDB.Entities.Core
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using LinqToAnything;

    public class AsyncCursorStream : Stream
    {
        private readonly IAsyncCursor<byte[]> _cursor;
        private Memory<byte> _currentBuffer;
        private int _currentBufferOffset;
        private long _position;
        private bool _endOfStream;
        private readonly IMemoryOwner<byte> _memoryOwner;

        public AsyncCursorStream(IAsyncCursor<byte[]> cursor, long totalSize, int chunkSize)
        {
            _cursor = cursor ?? throw new ArgumentNullException(nameof(cursor));
            Length = totalSize;
            _memoryOwner = MemoryPool<byte>.Shared.Rent(chunkSize);
            _currentBuffer = Memory<byte>.Empty;
            _currentBufferOffset = 0;
            _position = 0;
            _endOfStream = false;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        public override void Flush()
        {
            // No-op since the stream is read-only
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // Synchronous read is not supported
            throw new NotSupportedException("Use ReadAsync instead.");
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Start:
            if (_currentBufferOffset == 0)
            {
                // Load next buffer
                if (!await _cursor.MoveNextAsync(cancellationToken))
                {
                    _endOfStream = true;
                }

                var current = _cursor.Current;
                if (current == null || !current.Any())
                {
                    _endOfStream = true;
                }

                if (!_endOfStream)
                {
                    _currentBuffer = new byte[current.Sum(x => x.Length)];
                    // Assuming each byte[] in the cursor represents a chunk of data
                    int bufferWriteOffset = 0;
                    foreach (var bytes in current)
                    {
                        bytes.CopyTo(_currentBuffer[bufferWriteOffset..]);
                        bufferWriteOffset += bytes.Length;
                    }
                }
            }

            int bytesAvailable = _currentBuffer.Length - _currentBufferOffset;

            if (bytesAvailable <= 0 && _position < Length)
            {
                _currentBufferOffset = 0;
                goto Start;
            }

            int bytesToCopy = Math.Min(count, bytesAvailable);

            _currentBuffer.Slice(_currentBufferOffset, bytesToCopy).CopyTo(buffer);

            _currentBufferOffset += bytesToCopy;
            _position += bytesToCopy;

            return bytesToCopy;
        }

        // Optionally, implement other methods like Dispose to clean up resources
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cursor.Dispose();
                _memoryOwner.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    return offset;
                    break;
                case SeekOrigin.Current:
                    return _position - offset;
                    break;
                case SeekOrigin.End:
                    return Length - offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
            }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Stream does not support setting length.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Stream is read-only.");
        }
    }
}
