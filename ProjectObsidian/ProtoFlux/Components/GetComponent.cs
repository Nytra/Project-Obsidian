using System;
using FrooxEngine;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution;
using FrooxEngine.ProtoFlux;
using Obsidian.Elements;
using Elements.Core;
using FrooxEngine.Undo;
using System.Security.Permissions;
using FrooxEngine.CommonAvatar;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Components;

[NodeCategory("Obsidian/Components")]
[GenericTypes(GenericTypesAttribute.Group.EnginePrimitivesAndEnums)]
public class GetComponent : ObjectFunctionNode<ExecutionContext, Component>
{
    public ObjectInput<Slot> Target;

    public ObjectInput<Type> ComponentType;

    protected override Component Compute(ExecutionContext context)
    {
        var target = Target.Evaluate(context);
        if (target == null || target.IsRemoved) return null;
        var type = ComponentType.Evaluate(context);
        if (type == null) return null;
        var comp = target.GetComponent(type);
        if (comp == null || comp.GetType() == typeof(SimpleAvatarProtection)) return null;
        return comp;
    }
}