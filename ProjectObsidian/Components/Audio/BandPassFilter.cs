﻿using System;
using FrooxEngine;
using Elements.Assets;
using System.Net;
using ProtoFlux.Runtimes.Execution;

namespace Obsidian.Components.Audio
{
    [Category(new string[] { "Obsidian/Audio" })]
    public class BandPassFilter : Component, IAudioSource, IWorldElement
    {
        [Range(0.1f, 1.41f, "0.00")]
        public readonly Sync<float> Resonance;

        [Range(20f, 20000f, "0.00")]
        public readonly Sync<float> LowFrequency;

        [Range(20f, 20000f, "0.00")]
        public readonly Sync<float> HighFrequency;

        public readonly SyncRef<IAudioSource> Source;

        private double lastTime;

        private object lowFilter = null;
        private object highFilter = null;

        public bool IsActive
        {
            get => Source.Target != null && Source.Target.IsActive;
        }

        public int ChannelCount => Source.Target?.ChannelCount ?? 0;

        public void Read<S>(Span<S> buffer) where S : unmanaged, IAudioSample<S>
        {
            if (!IsActive)
            {
                return;
            }

            Span<S> tempBuffer = stackalloc S[buffer.Length];
            tempBuffer = buffer;
            Source.Target.Read(tempBuffer);

            if (lowFilter == null)
            {
                lowFilter = new ButterworthFilter.FilterButterworth<S>();
            }
            if (highFilter == null)
            {
                highFilter = new ButterworthFilter.FilterButterworth<S>();
            }

            ((ButterworthFilter.FilterButterworth<S>)lowFilter).UpdateCoefficients(HighFrequency, (int)(tempBuffer.Length / (Engine.Current.AudioSystem.DSPTime - lastTime)), ButterworthFilter.FilterButterworth<S>.PassType.Lowpass, Resonance);
            ((ButterworthFilter.FilterButterworth<S>)highFilter).UpdateCoefficients(LowFrequency, (int)(tempBuffer.Length / (Engine.Current.AudioSystem.DSPTime - lastTime)), ButterworthFilter.FilterButterworth<S>.PassType.Highpass, Resonance);

            for (int i = 0; i < tempBuffer.Length; i++)
            {
                ((ButterworthFilter.FilterButterworth<S>)lowFilter).Update(ref tempBuffer[i]);
                ((ButterworthFilter.FilterButterworth<S>)highFilter).Update(ref tempBuffer[i]);
            }

            lastTime = Engine.Current.AudioSystem.DSPTime;
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            Resonance.Value = 1.41f;
            LowFrequency.Value = 20f;
            HighFrequency.Value = 20000f;
            lastTime = Engine.Current.AudioSystem.DSPTime;
        }

        protected override void OnChanges()
        {
            base.OnChanges();
            if (Source.GetWasChangedAndClear())
            {
                lowFilter = null;
                highFilter = null;
            }
        }
    }
}