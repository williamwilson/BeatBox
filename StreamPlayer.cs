using Microsoft.DirectX.DirectSound;
using System.IO;
using System.Windows.Forms;
using System;

namespace BeatBox
{
    /// <summary>
    /// Represents an audio player which plays a stream with DirectSound.
    /// </summary>
    /// <remarks>
    /// This class uses a circular playback buffer and continually reads the data from the supplied
    /// stream into this circular buffer until the entire stream has been played.
    /// </remarks>
    internal sealed class StreamPlayer : IDisposable
    {
        private readonly SecondaryBuffer _buffer;
        private readonly int _bufferLength;
        private readonly Timer _feedTimer; 
        private readonly WaveFormat _format;
        private readonly Device _playbackDevice;
        private readonly Stream _stream;
        private int _writeIndex;

        /// <summary>
        /// Initializes a new stream player to play the specified stream containing wave data in the specified
        /// format through the specified device.
        /// </summary>
        /// <param name="playbackDevice">The device through which playback is to occur.</param>
        /// <param name="format">The format of the wave data in the stream.</param>
        /// <param name="stream">The stream of wave data to play.</param>
        public StreamPlayer(Device playbackDevice, WaveFormat format, Stream stream)
        {
            _playbackDevice = playbackDevice;
            _format = format;
            _stream = stream;

            BufferDescription bufferDescription = new BufferDescription(format);
            bufferDescription.BufferBytes = format.AverageBytesPerSecond;
            bufferDescription.ControlVolume = true;
            bufferDescription.GlobalFocus = true;

            _buffer = new SecondaryBuffer(bufferDescription, playbackDevice);
            _bufferLength = _buffer.Caps.BufferBytes;

            _feedTimer = new Timer();
            _feedTimer.Interval = 250;
            _feedTimer.Tick += new EventHandler(FeedTimer_Tick);
        }

        public void Dispose()
        {
            _buffer.Stop();
            _buffer.Dispose();

            _feedTimer.Stop();
            _feedTimer.Dispose();

            _playbackDevice.Dispose();

            _stream.Close();
            _stream.Dispose();
        }

        /// <summary>
        /// Feeds the specified number of bytes (or as many as are available in the source stream)
        /// into the playback buffer.
        /// </summary>
        /// <param name="numBytes">The maximum number of bytes to feed into the buffer.</param>
        private void Feed(int numBytes)
        {
            if (numBytes == 0)
            {
                return;
            }

            int bytesToWrite = Math.Min((int)(_stream.Length - _stream.Position), numBytes);

            if (bytesToWrite == 0)
            {
                /* note: stopping here may cut the playback short, because the play cursor hasn't yet reached
                 * the end of the source stream data... */
                Stop();
                return;
            }

            if (_buffer.Status.BufferLost)
            {
                _buffer.Restore();
            }

            if (_bufferLength - _writeIndex >= bytesToWrite)
            {
                _buffer.Write(_writeIndex, _stream, bytesToWrite, LockFlag.None);
            }
            else
            {
                /* perform a wrapping write */
                _buffer.Write(_writeIndex, _stream, _bufferLength - _writeIndex, LockFlag.None);
                bytesToWrite = bytesToWrite - (_bufferLength - _writeIndex);
                _writeIndex = 0;
                _buffer.Write(_writeIndex, _stream, bytesToWrite, LockFlag.None);
            }

            /* update the write index (handling wrap-around) */
            _writeIndex += bytesToWrite;
            _writeIndex = _writeIndex >= _bufferLength ? 0 : _writeIndex;
        }

        private void FeedTimer_Tick(object sender, EventArgs e)
        {
            /* continue to feed data from the source stream into the playback stream as space
             * becomes available in the playback stream */
            Feed(GetBytesPlayed());
        }
        
        /// <summary>
        /// Gets the number of bytes which have been played since the last write operation.
        /// </summary>
        /// <returns>The number of bytes played since the last write operation.</returns>
        private int GetBytesPlayed()
        {
            int played = 0;
            int position = _buffer.PlayPosition;
            if (position < _writeIndex)
            {
                /* the playback wrapped around to the beginning of the buffer */
                played = _bufferLength - _writeIndex + position;
            }
            else
            {
                played = position - _writeIndex;
            }

            return played;
        }

        /// <summary>
        /// Starts playback of the stream.
        /// </summary>
        public void Play()
        {
            Stop();

            /* reset the playback stream */
            _buffer.SetCurrentPosition(0);
            _writeIndex = 0;

            Feed(_bufferLength);

            _feedTimer.Start();
            _buffer.Play(0, BufferPlayFlags.Looping);
        }

        /// <summary>
        /// Stops playback of the stream.
        /// </summary>
        public void Stop()
        {
            _feedTimer.Stop();
            _buffer.Stop();
        }
    }
}
