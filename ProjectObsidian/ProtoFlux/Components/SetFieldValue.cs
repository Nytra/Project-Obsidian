using System;
using FrooxEngine;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution;
using FrooxEngine.ProtoFlux;
using Obsidian.Elements;
using Elements.Core;
using FrooxEngine.Undo;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Components;

[NodeCategory("Obsidian/Components")]
[GenericTypes(GenericTypesAttribute.Group.EnginePrimitivesAndEnums)]
public class SetFieldValue<T> : ActionNode<ExecutionContext> where T : unmanaged
{
    public static bool IsValidGenericType => Coder<T>.IsEnginePrimitive;

    public ObjectInput<IField<T>> Target;

    public ValueInput<T> Value;

    public ValueInput<bool> Undoable;

    public Continuation OnSuccess;

    public Continuation OnFail;

    protected override IOperation Run(ExecutionContext context)
    {
        IField<T> field = Target.Evaluate(context);
        if (field == null || field.IsRemoved)
        {
            return OnFail.Target;
        }
        if (Undoable.Evaluate(context))
        {
            field.UndoableSet(Value.Evaluate(context));
        }
        else
        {
            field.Value = Value.Evaluate(context);
        }
        return OnSuccess.Target;
    }

    public SetFieldValue()
    {
    }
}