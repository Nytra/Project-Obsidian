using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution;
using System.Threading.Tasks;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Flow;

[NodeCategory("Obsidian/Flow")]
public class AsyncImpulseDemultiplexer : VoidNode<ExecutionContext>
{
    public readonly AsyncOperationList Operations;

    public AsyncCall OnTriggered;

    public Call OnDone;

    public readonly ValueOutput<int> Index;

    public override bool CanBeEvaluated => false;

    public async Task<IOperation> DoOperationsAsync(ExecutionContext context, int index)
    {
        Index.Write(index, context);
        await OnTriggered.ExecuteAsync(context);
        return OnDone.Target;
    }

    public AsyncImpulseDemultiplexer()
    {
        Operations = new AsyncOperationList(this, 0);
        Index = new ValueOutput<int>(this);
    }
}
