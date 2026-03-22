using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NAudio.Vorbis;

namespace ME2LevelEditor.Services
{
    public class AudioService : IDisposable
    {
        private WaveStream _readerStream;
        private ISampleProvider _sampleProvider;
        private WaveOutEvent _waveOut;
        private float[] _allSamples;

        public bool IsLoaded => _readerStream != null;
        public TimeSpan Duration => _readerStream?.TotalTime ?? TimeSpan.Zero;
        public TimeSpan CurrentPosition => _readerStream?.CurrentTime ?? TimeSpan.Zero;
        public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

        public float Volume
        {
            get => _waveOut?.Volume ?? 1f;
            set { if (_waveOut != null) _waveOut.Volume = Math.Max(0f, Math.Min(1f, value)); }
        }

        public float[] LoadAudio(string path)
        {
            Dispose();

            string extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension == ".ogg")
            {
                var vorbisReader = new VorbisWaveReader(path);
                _allSamples = ReadSamplesToMono(vorbisReader, vorbisReader.WaveFormat.Channels);
                vorbisReader.Dispose();

                vorbisReader = new VorbisWaveReader(path);
                _readerStream = vorbisReader;
                _sampleProvider = vorbisReader;
            }
            else if (extension == ".mp3")
            {
                var mfReader = new MediaFoundationReader(path);
                var sampleChannel = new SampleChannel(mfReader, false);
                _allSamples = ReadSamplesToMono(sampleChannel, mfReader.WaveFormat.Channels);
                mfReader.Dispose();

                _readerStream = new MediaFoundationReader(path);
                _sampleProvider = new SampleChannel(_readerStream, false);
            }
            else
            {
                var reader = new AudioFileReader(path);
                _allSamples = ReadSamplesToMono(reader, reader.WaveFormat.Channels);
                reader.Dispose();

                _readerStream = new AudioFileReader(path);
                _sampleProvider = (ISampleProvider)_readerStream;
            }

            _waveOut = new WaveOutEvent { DesiredLatency = 50, NumberOfBuffers = 3 };
            _waveOut.Init(_sampleProvider);

            return _allSamples;
        }

        private float[] ReadSamplesToMono(ISampleProvider provider, int channels)
        {
            var buffer = new float[provider.WaveFormat.SampleRate * channels];
            var allData = new List<float>();
            int samplesRead;

            while ((samplesRead = provider.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < samplesRead; i += channels)
                {
                    float mono = 0f;
                    for (int ch = 0; ch < channels && (i + ch) < samplesRead; ch++)
                        mono += buffer[i + ch];
                    mono /= channels;
                    allData.Add(mono);
                }
            }

            return allData.ToArray();
        }

        public float[][] GetPeaks(float[] samples, int peakCount)
        {
            var mins = new float[peakCount];
            var maxs = new float[peakCount];

            if (samples == null || samples.Length == 0 || peakCount <= 0)
                return new[] { mins, maxs };

            double samplesPerPeak = (double)samples.Length / peakCount;

            for (int i = 0; i < peakCount; i++)
            {
                int start = (int)(i * samplesPerPeak);
                int end = (int)((i + 1) * samplesPerPeak);
                if (end > samples.Length) end = samples.Length;
                if (start >= samples.Length) break;

                float min = float.MaxValue;
                float max = float.MinValue;

                for (int j = start; j < end; j++)
                {
                    if (samples[j] < min) min = samples[j];
                    if (samples[j] > max) max = samples[j];
                }

                mins[i] = min;
                maxs[i] = max;
            }

            return new[] { mins, maxs };
        }

        public void Play()
        {
            _waveOut?.Play();
        }

        public void Pause()
        {
            _waveOut?.Pause();
        }

        public void Stop()
        {
            _waveOut?.Stop();
            if (_readerStream != null)
                _readerStream.CurrentTime = TimeSpan.Zero;
        }

        public void Seek(TimeSpan position)
        {
            if (_readerStream != null)
                _readerStream.CurrentTime = position;
        }

        public void Dispose()
        {
            _waveOut?.Stop();
            _waveOut?.Dispose();
            _waveOut = null;
            _readerStream?.Dispose();
            _readerStream = null;
            _sampleProvider = null;
            _allSamples = null;
        }
    }
}
