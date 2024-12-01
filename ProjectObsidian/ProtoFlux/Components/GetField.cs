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
public class GetField<T> : ObjectFunctionNode<ExecutionContext, IField<T>> where T : unmanaged
{
    public static bool IsValidGenericType => Coder<T>.IsEnginePrimitive;

    public ObjectInput<Component> Target;

    public ObjectInput<string> FieldName;

    protected override IField<T> Compute(ExecutionContext context)
    {
        Component comp = Target.Evaluate(context);
        if (comp == null || comp.IsRemoved) return null;
        if (comp.GetType() == typeof(SimpleAvatarProtection)) return null;
        var fieldName = FieldName.Evaluate(context);
        if (fieldName == null) return null;
        var field = comp.TryGetField(fieldName);
        if (field == null || field.ValueType != typeof(T)) return null;
        return (IField<T>)field;
    }
}