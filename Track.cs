using Microsoft.DirectX.DirectSound;
namespace BeatBox
{
    /// <summary>
    /// Represents a sequence for a given sample (patch).
    /// </summary>
    internal sealed class Track
    {
        private readonly bool[] _pattern;
        private readonly WaveData _sample;

        /// <summary>
        /// Initializes a new track with the specified sample and beat pattern.
        /// </summary>
        /// <param name="sample">The sample for this track.</param>
        /// <param name="pattern">The pattern in which the sample is played in this track.</param>
        public Track(WaveData sample, bool[] pattern)
        {
            _sample = sample;
            _pattern = pattern;
        }

        /// <summary>
        /// Gets the format of the sample in this track.
        /// </summary>
        public WaveFormat Format
        {
            get { return _sample.Format; }
        }

        /// <summary>
        /// Gets a new wave data reader for the sample at the specified beat index or null
        /// if the sample is not played at the specified beat.
        /// </summary>
        /// <param name="beatIndex">The index of the beat to retrieve.</param>
        /// <returns>A WaveDataReader for the sample at the specified beat or null if the sample
        /// is not played at the specified beat.</returns>
        public WaveDataReader GetReaderAtBeat(int beatIndex)
        {
            if (_pattern[beatIndex])
            {
                return new WaveDataReader(_sample);
            }

            return null;
        }
    }
}
