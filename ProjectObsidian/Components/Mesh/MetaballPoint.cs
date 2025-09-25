using Elements.Core;
using Elements.Data;
using FrooxEngine;

namespace Obsidian.Components.Utility;

[OldTypeName("Obsidian.MetaballPoint")]
[Category(new string[] { "Obsidian/Utility" })]
public class MetaballPoint : Component
{
    public readonly Sync<float> Radius;
    public readonly Sync<float> Strength;

    protected override void OnAwake()
    {
        Radius.Value = 1f;
        Strength.Value = 1f;
    }

    public float GetValue(float3 point, Slot space = null)
    {
        float distance = MathX.Distance(point, (space ?? World.RootSlot).GlobalPointToLocal(Slot.GlobalPosition));
        if (distance == 0) return float.MaxValue;
        return Strength * (Radius * Radius) / (distance * distance);
    }
}