using System;
using FrooxEngine;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution;
using FrooxEngine.ProtoFlux;
using Obsidian.Elements;
using Elements.Core;
using FrooxEngine.Undo;
using System.Security.Permissions;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Components;

[NodeCategory("Obsidian/Components")]
[GenericTypes(GenericTypesAttribute.Group.EnginePrimitivesAndEnums)]
public class GetFieldValue<T> : ValueFunctionNode<ExecutionContext, T> where T : unmanaged
{
    public static bool IsValidGenericType => Coder<T>.IsEnginePrimitive;

    public ObjectInput<IField<T>> Target;

    protected override T Compute(ExecutionContext context)
    {
        IField<T> field = Target.Evaluate(context);
        if (field == null || field.IsRemoved) return default;
        return field.Value;
    }
}