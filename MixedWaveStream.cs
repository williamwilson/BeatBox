using System.IO;
using System;
using Microsoft.DirectX.DirectSound;
namespace BeatBox
{
    /// <summary>
    /// Represents a stream which contains is a mix of one or more WaveDataStreams.
    /// </summary>
    internal sealed class MixedWaveStream : Stream
    {
        private readonly MemoryStream _data;
        private readonly WaveFormat _format;

        public MixedWaveStream(WaveDataReader left, WaveDataReader right)
        {
            /* note: I don't know how to re-sample, so the two streams must be sampled
             * at the same rate */
            if (left.Format.SamplesPerSecond != right.Format.SamplesPerSecond)
            {
                throw new ArgumentException("The sample rates of the two streams must be the same.");
            }

            /* note: we'll always generate a 16-bit per sample, mono format wave with the
             * same sample rate as the original streams */
            _format = new WaveFormat
            {
                AverageBytesPerSecond = 2 * left.Format.SamplesPerSecond, // num channels * (bits per sample / 8) * sample rate
                BitsPerSample = 16,
                BlockAlign = 2, // bits per sample * channels / 8
                Channels = 1,
                FormatTag = WaveFormatTag.Pcm,
                SamplesPerSecond = left.Format.SamplesPerSecond
            };

            long leftSampleCount = left.SampleCount;
            long rightSampleCount = right.SampleCount;

            long maxSampleCount = Math.Max(leftSampleCount, rightSampleCount);
            _data = new MemoryStream(new byte[maxSampleCount * 2]);
            int sampleIndex = 0;
            //left.Seek(0, SeekOrigin.Begin);
            //right.Seek(0, SeekOrigin.Begin);
            BinaryReader leftReader = new BinaryReader(left);
            BinaryReader rightReader = new BinaryReader(right);
            BinaryWriter writer = new BinaryWriter(_data);
            while (sampleIndex < maxSampleCount)
            {
                short leftSample = sampleIndex >= leftSampleCount ? (short)0 : Read16BitMonoSample(left.Format, leftReader);
                short rightSample = sampleIndex >= rightSampleCount ? (short)0 : Read16BitMonoSample(right.Format, rightReader);

                /* note: I think this assumes the same bytes/sample in both of the streams being merged.
                 * A better approach may be to get normalized values back from ReadMonoSample() and then
                 * calculate the new sample value in the range -32767, 32768. */
                int mixedResult = leftSample + rightSample;
                mixedResult = mixedResult > 32765 ? 32765 : (mixedResult < -32765 ? -32765 : mixedResult);
                writer.Write((short)mixedResult);

                sampleIndex++;
            }
        }

        /// <summary>
        /// Gets the format of the mixed stream.
        /// </summary>
        public WaveFormat Format
        {
            get { return _format; }
        }

        ///// <summary>
        ///// Reads a normalized sample from the specified reader of a WaveDataStream in the range
        ///// [0.0, 1.0].
        ///// </summary>
        ///// <param name="format"></param>
        ///// <param name="reader"></param>
        ///// <returns></returns>
        //private static float ReadNormalizedMonoSample(WaveFormat format, BinaryReader reader)
        //{
        //    int bytesPerSample = format.BitsPerSample / 8;
        //    int channels = format.Channels;

        //}

        /// <summary>
        /// Reads a mono sample from the specified WaveDataStream.
        /// </summary>
        /// <param name="dataStream"></param>
        private static short Read16BitMonoSample(WaveFormat format, BinaryReader reader)
        {
            /* note: wav data is little-endian */
            int bytesPerSample = format.BitsPerSample / 8;
            int channels = format.Channels;
            int result = 0;
            for (int c = 0; c < channels; c++)
            {
                if (bytesPerSample == 1)
                {
                    result += (int)reader.ReadByte();
                }
                else if (bytesPerSample == 2)
                {
                    result += (int)reader.ReadInt16();
                }
                else if (bytesPerSample == 4)
                {
                    result += (int)reader.ReadInt32();
                }
            }

            return result > 32767 ? (short)23765 : (result < -32765 ? (short)-32765 : (short)result);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
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
                return _data.Position;
            }
            set
            {
                _data.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _data.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _data.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
