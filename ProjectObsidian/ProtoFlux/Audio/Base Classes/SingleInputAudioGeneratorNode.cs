using System;
using Awwdio;
using Elements.Assets;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Core;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Audio
{
    public abstract class SingleInputAudioGeneratorNodeProxy : AudioGeneratorNodeProxy
    {
        public IWorldAudioDataSource AudioInput;
    }
    [NodeCategory("Obsidian/Audio")]
    public abstract class SingleInputAudioGeneratorNode<P> : AudioGeneratorNode<P> where P : SingleInputAudioGeneratorNodeProxy, new()
    {
        [ChangeListener]
        public readonly ObjectInput<IWorldAudioDataSource> AudioInput;

        public override void Changed(FrooxEngineContext context)
        {
            P proxy = GetProxy(context);
            if (proxy == null)
            {
                return;
            }
            base.Changed(context);
            proxy.AudioInput = AudioInput.Evaluate(context);
        }
    }
}