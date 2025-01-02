using System;
using FrooxEngine;
using Elements.Assets;
using Elements.Core;
using System.Runtime.InteropServices;

namespace Obsidian.Components.Audio
{
    [Category(new string[] { "Obsidian/Audio" })]
    public class FFT_Data : Component
    {
        public readonly SyncRef<IAudioSource> Source;
        public readonly Sync<int> NumBands;
        public readonly SyncList<RawOutput<float>> Bands;
        private RealtimeAudioFFT fft = null;
        private RealtimeAudioFFT.FFTResult lastResult = null;

        public bool IsActive
        {
            get
            {
                return Source.Target != null &&
                       Source.Target.IsActive;
            }
        }

        public int ChannelCount
        {
            get
            {
                return Source.Target?.ChannelCount ?? 0;
            }
        }

        protected override void OnCommonUpdate()
        {
            if (lastResult == null) return;

            int count = NumBands.Value;

            if (count == 0) return;

            var bands = fft.GetFrequencyBands(lastResult, count);
            Bands.EnsureExactCount(count);
            int i = 0;
            foreach ( var band in Bands.Elements )
            {
                band.Value = 20f * (float)MathX.Log10(bands[i]);
                i++;
            }
        }

        protected override void OnStart()
        {
            if (Source.Target != null) 
                fft = new RealtimeAudioFFT(sampleRate: Engine.AudioSystem.SampleRate);
            Source.OnTargetChange += (SyncRef<IAudioSource> syncRef) => 
            {
                fft = syncRef.Target != null ? new RealtimeAudioFFT(sampleRate: Engine.AudioSystem.SampleRate) : null;
            };
        }

        protected override void OnAudioUpdate()
        {
            if (!IsActive) return;
            if (ChannelCount == 0) return;
            Span<MonoSample> buf = stackalloc MonoSample[Engine.AudioSystem.SampleRate];
            Source.Target.Read(buf);
            fft.ProcessBuffer(MemoryMarshal.Cast<MonoSample, float>(buf).ToArray());
            lastResult = fft.ComputeCurrentFFT();
        }
    }
}