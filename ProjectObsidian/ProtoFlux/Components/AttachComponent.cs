using System;
using FrooxEngine;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution;
using FrooxEngine.ProtoFlux;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Components;

[NodeCategory("Obsidian/Components")]
public class AttachComponent : ActionNode<ExecutionContext>
{
    public ObjectInput<Slot> Target;

    public ObjectInput<Type> Type;

    public readonly ObjectOutput<Component> AttachedComponent;

    public Continuation OnAttached;

    public Continuation OnFail;

    protected override IOperation Run(ExecutionContext context)
    {
        Slot slot = Target.Evaluate(context);
        if (slot == null || slot.IsRemoved)
        {
            return OnFail.Target;
        }
        Type type = Type.Evaluate(context);
        if (type == null || !typeof(Component).IsAssignableFrom(type))
        {
            return OnFail.Target;
        }
        var attached = slot.AttachComponent(type);
        if (attached == null)
        {
            return OnFail.Target;
        }
        AttachedComponent.Write(attached, context);
        return OnAttached.Target;
    }

    public AttachComponent()
    {
        AttachedComponent = new ObjectOutput<Component>(this);
    }
}