using System.IO;
using Microsoft.DirectX.DirectSound;
using System.Text;
using System;

namespace BeatBox
{
    /// <summary>
    /// Represents the data portion of a wave.
    /// </summary>
    internal sealed class WaveData
    {
        private readonly WaveFormat _format;
        private readonly byte[] _data;

        /// <summary>
        /// Initializes a new WaveData from the specified Stream containing a full wave file.
        /// </summary>
        /// <param name="stream">The Stream from which the wave data is read.</param>
        private WaveData(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                _format = ReadFormat(reader);
                
                /* data sub-chunk */
                int dataLength = reader.ReadInt32();
                _data = reader.ReadBytes(dataLength);
            }
        }

        /// <summary>
        /// Reads the format (header) information from the wave file and advances the reader
        /// to the data sub-chunk of the file.
        /// </summary>
        /// <param name="reader">A BinaryReader around the wave file stream.</param>
        /// <returns>The WaveFormat read from the wave file header.</returns>
        private static WaveFormat ReadFormat(BinaryReader reader)
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

            WaveFormat format = new WaveFormat()
            {
                AverageBytesPerSecond = byteRate,
                BitsPerSample = bitsPerSample,
                BlockAlign = blockAlign,
                Channels = numberOfChannels,
                FormatTag = (WaveFormatTag)audioFormat,
                SamplesPerSecond = sampleRate
            };

            /* note: skip the extra params section in order to advance the reader to the
             * data section of the file */
            while (reader.BaseStream.Position < reader.BaseStream.Length && ReadChunkString(reader) != "data")
            {
            }

            return format;
        }

        /// <summary>
        /// Reads a 4-byte chunk from the specified reader and interprets it as an ASCII-encoded
        /// string of characters.
        /// </summary>
        /// <param name="reader">The reader from which the chunk is to be read.</param>
        /// <returns>The string of characters in the chunk.</returns>
        private static string ReadChunkString(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(4);
            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// Populates a WaveData instance from the specified wave file.
        /// </summary>
        /// <param name="filename">The path of the file from which wave data is to be read.</param>
        /// <returns>A WaveData instance initialized with the data from the specified file.</returns>
        public static WaveData FromFile(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                return new WaveData(fs);
            }
        }

        /// <summary>
        /// Gets the raw byte data of this wave.
        /// </summary>
        public byte[] Data
        {
            get { return _data; }
        }

        /// <summary>
        /// Gets the wave format information for this data.
        /// </summary>
        public WaveFormat Format
        {
            get { return _format; }
        }

        /// <summary>
        /// Gets the length of this data in bytes.
        /// </summary>
        public long Length
        {
            get { return _data.Length; }
        }

        /// <summary>
        /// Gets the number of samples in this data.
        /// </summary>
        public long SampleCount
        {
            get { return _data.Length / ((_format.BitsPerSample / 8) * _format.Channels); }
        }
    }
}
