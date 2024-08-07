using Elements.Core;
using FrooxEngine;
using ProtoFlux.Core;
using System.Threading.Tasks;
using FrooxEngine.ProtoFlux;
using ProtoFlux.Runtimes.Execution;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Utility
{
    [DataModelType]
    public enum LogSeverity
    {
        Log,
        Warning,
        Error
    }

    [NodeCategory("Obsidian/Utility/WriteToLog")]
    public class WriteToLogNode : AsyncActionNode<FrooxEngineContext>
    {
        public ObjectInput<string> Value;
        public ValueInput<LogSeverity> Severity;
        public ObjectInput<string> Tag;
        public ObjectInput<User> HandlingUser;

        public AsyncCall OnWriteStart;
        public Continuation OnWriteComplete;

        protected override async Task<IOperation> RunAsync(FrooxEngineContext context)
        {
            User user = HandlingUser.Evaluate(context, context.LocalUser);
            if (user != null)
            {
                await OnWriteStart.ExecuteAsync(context);
                switch (Severity.Evaluate(context))
                {
                    case LogSeverity.Log:
                        UniLog.Log(Tag.Evaluate(context) + Value.Evaluate(context)?.ToString());
                        break;
                    case LogSeverity.Warning:
                        UniLog.Warning(Tag.Evaluate(context) + Value.Evaluate(context)?.ToString());
                        break;
                    case LogSeverity.Error:
                        UniLog.Error(Tag.Evaluate(context) + Value.Evaluate(context)?.ToString());
                        break;
                }
                return OnWriteComplete.Target;
            }
            return null;
        }
    }
}