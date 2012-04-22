using System.IO;
using System.Timers;
using Microsoft.DirectX.DirectSound;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BeatBox
{
    /// <summary>
    /// Represents a mixer which manages a set of tracks and produces a single mixed output stream.
    /// </summary>
    internal sealed class BeatBoxMixer : Stream
    {
        private const int BEATS_PER_TRACK = 16;

        private int _beatIndex;
        private readonly int _bpm;
        private long _byteCounter;
        private readonly int _bytesPerBeat;
        private readonly List<WaveDataReader> _readers;
        private readonly List<Track> _tracks;

        public BeatBoxMixer(int bpm, IEnumerable<Track> tracks)
        {
            _bpm = bpm;
            _tracks = new List<Track>(tracks);

            if (_tracks.Count == 0)
            {
                throw new ArgumentException("One or more tracks must be supplied.");
            }

            int samplesPerSecond = _tracks.First().Format.SamplesPerSecond;
            foreach (Track track in _tracks)
            {
                if (track.Format.SamplesPerSecond != samplesPerSecond)
                {
                    throw new ArgumentException("All tracks must have the same sample rate.");
                }
            }

            _bytesPerBeat = CalculateBytesPerBeat(bpm, samplesPerSecond);

            _readers = new List<WaveDataReader>();
            _beatIndex = 0;
            _byteCounter = 0;
            LoadReadersAtBeat(0);
        }

        /// <summary>
        /// Calculates the number of bytes in the output stream per beat.
        /// </summary>
        /// <param name="bpm">The number of beats per minute.</param>
        /// <returns>The number of bytes in the output stream per beat.</returns>
        private static int CalculateBytesPerBeat(int bpm, int samplesPerSecond)
        {
            /* note: the output of the mixer is a 16-bit mono stream */
            int bytesPerSecond = samplesPerSecond * 2;
            int bytesPerMinute = bytesPerSecond * 60;

            return bytesPerMinute / bpm;
        }

        private void LoadReadersAtBeat(int beatIndex)
        {
            foreach (Track track in _tracks)
            {
                WaveDataReader reader = track.GetReaderAtBeat(beatIndex);
                if (reader != null)
                {
                    _readers.Add(reader);
                }
            }
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
            get
            {
                /* note: really the length is infinite as read will always return the requested
                 * number of bytes */
                return 100000;
            }
        }

        public override long Position
        {
            get
            {
                return 0;
            }
            set
            {
                throw new UnsupportedException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            while (_byteCounter + count >= _bytesPerBeat)
            {
                /* get the bytes up to the next beat */
                int bytesRemainingInBeat = (int)(_bytesPerBeat - _byteCounter);
                ReadMixedBytes(buffer, offset, bytesRemainingInBeat);

                offset += bytesRemainingInBeat;
                count -= bytesRemainingInBeat;

                /* populate the readers for the next beat */
                _beatIndex++;
                _byteCounter = 0;
                if (_beatIndex == BEATS_PER_TRACK)
                {
                    _beatIndex = _beatIndex - BEATS_PER_TRACK;
                }
                LoadReadersAtBeat(_beatIndex);
            }

            ReadMixedBytes(buffer, offset, count);
            _byteCounter += count;

            return count;
        }

        private void ReadMixedBytes(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = 0;
                /* note: traverse the readers in reverse order so that we may remove
                 * them as we go if they are emptied */
                for (int r = _readers.Count - 1; r >= 0; r--)
                {
                    int read = _readers[r].ReadByte();
                    if (read == -1)
                    {
                        _readers.RemoveAt(r);
                    }
                    else
                    {
                        buffer[offset + i] = (byte)read;
                    }
                }
            }
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
