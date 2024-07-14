﻿using System;
using FrooxEngine.ProtoFlux;
using Newtonsoft.Json.Linq;
using Obsidian.Elements;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Json
{
    [NodeCategory("Obsidian/Json")]
    public class JsonRemoveFromArrayNode : ObjectFunctionNode<FrooxEngineContext, JsonArray>
    {
        public readonly ObjectInput<JsonArray> Array;
        public readonly ObjectInput<int> Index;


        protected override JsonArray Compute(FrooxEngineContext context)
        {
            var array = Array.Evaluate(context);
            var index = Index.Evaluate(context);
            if (array == null || index < 0 || index >= array.Count)
                return null;

            return array.Remove(index);
        }
    }
}
