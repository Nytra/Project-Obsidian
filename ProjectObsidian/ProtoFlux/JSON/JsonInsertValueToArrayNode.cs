using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using Obsidian.Elements;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Json;

[NodeName("Insert To Array")]
[NodeCategory("Obsidian/Json")]
[GenericTypes(typeof(string), typeof(Uri), typeof(IJsonToken), typeof(JsonObject), typeof(JsonArray))]
public class JsonInsertValueToArrayNode<T> : ObjectFunctionNode<FrooxEngineContext, JsonArray>
{
    public readonly ObjectInput<JsonArray> Array;
    public readonly ObjectInput<T> Object;
    public readonly ValueInput<int> Index;
    public static bool IsValidGenericType => JsonTypeHelper.ValidValueTypes.Contains(typeof(T));
    protected override JsonArray Compute(FrooxEngineContext context)
    {
        var array = Array.Evaluate(context);
        var obj = Object.Evaluate(context);
        var index = Index.Evaluate(context);
        if (array == null || index < 0 || index > array.Count)
            return null;

        return array.Insert(index, obj);
    }
}
