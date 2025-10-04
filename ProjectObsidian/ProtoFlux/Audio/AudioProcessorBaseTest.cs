using System;
using Awwdio;
using Elements.Assets;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Core;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Audio
{
    public abstract class AudioProcessorNodeProxyBase : AudioGeneratorNodeProxyBase
    {
        public IWorldAudioDataSource AudioInput;
    }
    [NodeCategory("Obsidian/Audio")]
    public abstract class AudioProcessorNodeBase<P> : AudioGeneratorNodeBase<P> where P : AudioProcessorNodeProxyBase, new()
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
            proxy.AudioInput = AudioInput.Evaluate(context);
        }
    }
}