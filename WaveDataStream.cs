using System.IO;
using Microsoft.DirectX.DirectSound;
using System.Text;
using System;

namespace BeatBox
{
    /// <summary>
    /// Represents a stream containing the data portion of a wave.
    /// </summary>
    internal sealed class WaveDataStream : Stream
    {
        private readonly WaveFormat _format;
        private readonly MemoryStream _stream;

        private WaveDataStream(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                string riffChunk = ReadChunkString(reader);
                if (riffChunk != "RIFF")
                {
                    throw new InvalidOperationException("Invalid wave data.");
                }
                reader.ReadInt32(); // ChunkSize (size of file - 8)
                string waveFormat = ReadChunkString(reader);
                if (waveFormat != "WAVE")
                {
                    throw new InvalidOperationException("Invalid wave data.");
                }

                /* format sub-chunk */
                string fmtChunkID = ReadChunkString(reader);
                if (fmtChunkID != "fmt ")
                {
                    throw new InvalidOperationException("Invalid wave data.");
                }
                int fmtChunkSize = reader.ReadInt32();
                if (fmtChunkSize != 16)
                {
                    throw new InvalidOperationException("Invalid wave data.");
                }
                int audioFormat = reader.ReadInt16();
                short numberOfChannels = reader.ReadInt16();
                int sampleRate = reader.ReadInt32();
                int byteRate = reader.ReadInt32();
                short blockAlign = reader.ReadInt16();
                short bitsPerSample = reader.ReadInt16();

                _format = new WaveFormat()
                {
                    AverageBytesPerSecond = byteRate,
                    BitsPerSample = bitsPerSample,
                    BlockAlign = blockAlign,
                    Channels = numberOfChannels,
                    FormatTag = (WaveFormatTag)audioFormat,
                    SamplesPerSecond = sampleRate
                };

                /* note: skip the extra params section */
                while (stream.Position < stream.Length && ReadChunkString(reader) != "data")
                {
                }

                /* data sub-chunk */
                int dataLength = reader.ReadInt32();

                _stream = new MemoryStream(dataLength);
                _stream.Write(reader.ReadBytes(dataLength), 0, dataLength);
            }
        }

        private static string ReadChunkString(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            return Encoding.ASCII.GetString(bytes);
        }

        public static WaveDataStream FromFile(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                return new WaveDataStream(fs);
            }
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

        public override long Length
        {
            get { return _stream.Length; }
        }

        public override long Position
        {
            get
            {
                return _stream.Position;
            }
            set
            {
                _stream.Position = value;
            }
        }

        public WaveFormat Format
        {
            get { return _format; }
        }

        public long SampleCount
        {
            get { return _stream.Length / ((_format.BitsPerSample / 8) * _format.Channels); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new System.NotImplementedException();
        }
    }
}
