using System.IO;
using System.Timers;
using Microsoft.DirectX.DirectSound;
using System;

namespace BeatBox
{
    /// <summary>
    /// Represents a mixer which manages a set of tracks and produces a single output stream.
    /// </summary>
    internal sealed class BeatBox
    {
        private static readonly WaveDataStream _rimSample;
        private static readonly WaveDataStream _bassSample;
        private static readonly MixedWaveStream _mixedStream;

        static BeatBox()
        {
            _bassSample = WaveDataStream.FromFile(@"d:\dev\beatbox\samples\bass.wav");
            _rimSample = WaveDataStream.FromFile(@"d:\dev\beatbox\samples\open.wav");

            _mixedStream = new MixedWaveStream(_bassSample, _rimSample);
        }

        private readonly SecondaryBuffer _buffer;
        private readonly int _bufferLength;
        private readonly Device _playbackDevice;
        private readonly Timer _timer;
        private int _writeIndex;

        public BeatBox(Device playbackDevice, WaveFormat format)
        {
            _playbackDevice = playbackDevice;

            BufferDescription bufferDescription = new BufferDescription(format);
            bufferDescription.BufferBytes = format.AverageBytesPerSecond;
            bufferDescription.ControlVolume = true;
            bufferDescription.GlobalFocus = true;

            _buffer = new SecondaryBuffer(bufferDescription, playbackDevice);
            _bufferLength = _buffer.Caps.BufferBytes;

            _timer = new Timer(250);
            _timer.Elapsed += new ElapsedEventHandler(TimerElapsed);
        }

        /// <summary>
        /// Feeds the specified number of bytes into the play buffer.
        /// </summary>
        /// <param name="numBytes">The number of bytes to feed into the buffer.</param>
        private void Feed(int numBytes)
        {
            int bytesToWrite = Math.Min((int)(_mixedStream.Length - _mixedStream.Position), numBytes);
            _buffer.Write(_writeIndex, _mixedStream, bytesToWrite, LockFlag.None);
            
            if (_mixedStream.Position == _mixedStream.Length)
            {
                _mixedStream.Seek(0, SeekOrigin.Begin);
            }

            _writeIndex += bytesToWrite;
            _writeIndex = _writeIndex >= _bufferLength ? 0 : _writeIndex;
        }

        public void Play()
        {
            Stop();

            /* reset the buffer */
            _buffer.SetCurrentPosition(0);
            _writeIndex = 0;
            _mixedStream.Seek(0, SeekOrigin.Begin);

            Feed(_bufferLength);

            _timer.Start();
            _buffer.Play(0, BufferPlayFlags.Looping);
        }

        public void Stop()
        {
            _timer.Stop();
            _buffer.Stop();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            /* determine the number of bytes which have been played */
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

            Feed(played);
        }
    }
}
