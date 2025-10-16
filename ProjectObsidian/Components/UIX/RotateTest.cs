using System;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;

namespace Obsidian.Components.UIX;

[Category("Obsidian/UIX")]
public class RotateTest : Graphic
{
    public readonly SyncRef<Graphic> Target;

    public readonly Sync<floatQ> Rotation;

    public override bool RequiresPreGraphicsCompute => false;

    public override void ComputeGraphic(GraphicsChunk.RenderData renderData)
    {
        if (Target.Target is null) return;

        var newData = new GraphicsChunk.RenderData(renderData.Chunk, GraphicsChunk.RenderType.Content);
        Target.Target.ComputeGraphic(newData);

        float3 maxExtent = float3.Zero;

        foreach (var pos in newData.Mesh.RawPositions)
        {
            if (pos.x > maxExtent.x)
                maxExtent = maxExtent.SetX(pos.x);
            if (pos.y > maxExtent.y)
                maxExtent = maxExtent.SetY(pos.y);
        }

        renderData.Mesh.Rotate(Rotation.Value);
        renderData.Mesh.Translate(maxExtent * -1);

        Target.Target.ComputeGraphic(renderData);

        renderData.Mesh.Translate(maxExtent);
        renderData.Mesh.Rotate(Rotation.Value.Inverted);
    }

    public override bool IsPointInside(in float2 point)
    {
        return true;
    }

    public override ValueTask PreGraphicsCompute()
    {
        throw new NotSupportedException();
    }

    public override void PrepareCompute()
    {
        
    }

    protected override void FlagChanges(RectTransform rect)
    {
        rect.MarkChangeDirty();
    }
}