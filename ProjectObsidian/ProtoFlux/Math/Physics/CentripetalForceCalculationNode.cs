using Elements.Core;
using FrooxEngine;
using ProtoFlux.Core;
using ProtoFlux.Runtimes.Execution;
using ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Math.Physics;
using System;
using FrooxEngine.ProtoFlux;

namespace ProtoFlux.Runtimes.Execution.Nodes.Obsidian.Math.Physics
{
    [NodeCategory("Obsidian/Math/Physics")]
    public class CentripetalForceCalculationNode : ValueFunctionNode<FrooxEngineContext, float>
    {
        public ValueInput<float> Mass;
        public ValueInput<float> Velocity;
        public ValueInput<float> Radius;

        protected override float Compute(FrooxEngineContext context)
        {
            try
            {
                float m = Mass.Evaluate(context);
                float v = Velocity.Evaluate(context);
                float r = Radius.Evaluate(context);

                float result = (m * v * v) / r;

                return result;
            }
            catch (Exception ex)
            {
                // Log any exceptions if needed
                throw new Exception($"Error in CentripetalForceCalculationNode.Compute: {ex.Message}");
            }
        }
    }
}

