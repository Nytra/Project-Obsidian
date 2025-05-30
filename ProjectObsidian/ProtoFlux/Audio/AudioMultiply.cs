﻿using System;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution;
using FrooxEngine.ProtoFlux;
using FrooxEngine;
using Elements.Assets;
using Awwdio;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Audio
{
    public class AudioMultiplyProxy : ProtoFluxEngineProxy, Awwdio.IAudioDataSource, IWorldAudioDataSource
    {
        public IWorldAudioDataSource AudioInput;

        public float Value;

        public bool Active;

        public bool IsActive => Active;

        public int ChannelCount => AudioInput?.ChannelCount ?? 0;

        public void Read<S>(Span<S> buffer, AudioSimulator simulator) where S : unmanaged, IAudioSample<S>
        {
            if (!IsActive || AudioInput == null)
            {
                buffer.Fill(default(S));
                return;
            }

            Span<S> buffer1s = stackalloc S[buffer.Length];
            buffer1s.Fill(default);
            if (AudioInput != null)
            {
                AudioInput.Read(buffer1s, simulator);
            }

            //AudioInput.Read(buffer);

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = buffer1s[i].Multiply(Value);
            }
        }
    }
    [NodeCategory("Obsidian/Audio")]
    public class AudioMultiply : ProxyVoidNode<FrooxEngineContext, AudioMultiplyProxy>, IExecutionChangeListener<FrooxEngineContext>
    {
        [ChangeListener]
        public readonly ObjectInput<IWorldAudioDataSource> AudioInput;

        [ChangeListener]
        public readonly ValueInput<float> Value;

        public readonly ObjectOutput<IWorldAudioDataSource> AudioOutput;

        private ObjectStore<Action<IChangeable>> _enabledChangedHandler;

        private ObjectStore<SlotEvent> _activeChangedHandler;

        public bool ValueListensToChanges { get; private set; }

        private bool ShouldListen(AudioMultiplyProxy proxy)
        {
            if (proxy.Enabled)
            {
                return proxy.Slot.IsActive;
            }
            return false;
        }

        protected override void ProxyAdded(AudioMultiplyProxy proxy, FrooxEngineContext context)
        {
            base.ProxyAdded(proxy, context);
            NodeContextPath path = context.CaptureContextPath();
            ProtoFluxNodeGroup group = context.Group;
            context.GetEventDispatcher(out var dispatcher);
            Action<IChangeable> enabledHandler = delegate
            {
                dispatcher.ScheduleEvent(path, delegate (FrooxEngineContext c)
                {
                    UpdateListenerState(c);
                });
            };
            SlotEvent activeHandler = delegate
            {
                dispatcher.ScheduleEvent(path, delegate (FrooxEngineContext c)
                {
                    UpdateListenerState(c);
                });
            };
            proxy.EnabledField.Changed += enabledHandler;
            proxy.Slot.ActiveChanged += activeHandler;
            _enabledChangedHandler.Write(enabledHandler, context);
            _activeChangedHandler.Write(activeHandler, context);
            ValueListensToChanges = ShouldListen(proxy);
            proxy.Active = ValueListensToChanges;
        }

        protected override void ProxyRemoved(AudioMultiplyProxy proxy, FrooxEngineContext context, bool inUseByAnotherInstance)
        {
            if (!inUseByAnotherInstance)
            {
                proxy.EnabledField.Changed -= _enabledChangedHandler.Read(context);
                proxy.Slot.ActiveChanged -= _activeChangedHandler.Read(context);
                _enabledChangedHandler.Clear(context);
                _activeChangedHandler.Clear(context);
                proxy.Active = false;
            }
        }

        protected void UpdateListenerState(FrooxEngineContext context)
        {
            AudioMultiplyProxy proxy = GetProxy(context);
            if (proxy != null)
            {
                bool shouldListen = ShouldListen(proxy);
                if (shouldListen != ValueListensToChanges)
                {
                    ValueListensToChanges = shouldListen;
                    context.Group.MarkChangeTrackingDirty();
                    proxy.Active = shouldListen;
                }
            }
        }

        public void Changed(FrooxEngineContext context)
        {
            AudioMultiplyProxy proxy = GetProxy(context);
            if (proxy == null)
            {
                return;
            }
            proxy.AudioInput = AudioInput.Evaluate(context);
            proxy.Value = Value.Evaluate(context);
        }

        protected override void ComputeOutputs(FrooxEngineContext context)
        {
            AudioMultiplyProxy proxy = GetProxy(context);
            AudioOutput.Write(proxy, context);
        }

        public AudioMultiply()
        {
            AudioOutput = new ObjectOutput<IWorldAudioDataSource>(this);
        }
    }
}