using System;
using System.Threading.Tasks;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Flow
{
    [NodeCategory("Obsidian/Flow")]
    [NodeName("Fork", false)]
    public class ForkNode : ActionNode<FrooxEngineContext>
    {
        public Call Fork;

        public Continuation OnDone;

        protected override IOperation Run(FrooxEngineContext context)
        {
            context.Group.ExecuteImmediatelly(default(NodeContextPath), (c) => 
            { 
                Fork.Execute(c);
            });
            return OnDone.Target;
        }
    }
}