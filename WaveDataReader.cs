using System;
using System.IO;
using Microsoft.DirectX.DirectSound;

namespace BeatBox
{
    /// <summary>
    /// Represents a reader of wave data which exposes a stream interface.
    /// </summary>
    /// <remarks>
    /// This class was implemented in order to have a single instance of the wave data
    /// in memory and have multiple readers at different locations in that data (in case
    /// the sample is playing in multiple positions simultaneously).
    /// </remarks>
    internal sealed class WaveDataReader : Stream
    {
        private readonly WaveData _data;
        private long _position;

        /// <summary>
        /// Initializes a new WaveDataReader for the specified WaveData.
        /// </summary>
        /// <param name="data">The wave data from which bytes will be read.</param>
        public WaveDataReader(WaveData data)
        {
            _data = data;
        }

        /// <summary>
        /// Gets the wave format information for the data being read.
        /// </summary>
        public WaveFormat Format
        {
            get { return _data.Format; }
        }

        /// <summary>
        /// Gets the number of samples in the data being read.
        /// </summary>
        public long SampleCount
        {
            get { return _data.SampleCount; }
        }

        #region Stream implementation

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
        }

        public override long Length
        {
            get { return _data.Length; }
        }

        public override long Position
        {
            get
            {
                return _position;
            }
            set
            {
                throw new UnsupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesToRead = (int)Math.Min(count, _data.Length - _position);
            for (int i = 0; i < bytesToRead; i++)
            {
                buffer[offset + i] = _data.Data[_position];
                _position++;
            }

            return bytesToRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new UnsupportedException();
        }

        public override void SetLength(long value)
        {
            throw new UnsupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new UnsupportedException();
        }

        #endregion
    }
}
