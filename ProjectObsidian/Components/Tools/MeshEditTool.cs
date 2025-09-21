using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;

namespace Obsidian.Components.Tools;

public class MeshEditTool : Tool
{
    private Mesh currentMesh;
    protected readonly DriveRef<OverlayFresnelMaterial> _material;

    protected override void OnAttach()
    {
        base.OnAttach();
        Slot visual = base.Slot.AddSlot("Visual");
        visual.AttachComponent<SphereCollider>().Radius.Value = 0.02f;
        visual.LocalRotation = floatQ.Euler(90f, 0f, 0f);
        visual.LocalPosition += float3.Forward * 0.05f;
        _material.Target = visual.AttachComponent<OverlayFresnelMaterial>();
        ConeMesh coneMesh = visual.AttachMesh<ConeMesh>(_material.Target);
        coneMesh.RadiusTop.Value = 0.0025f;
        coneMesh.RadiusBase.Value = 0.015f;
        coneMesh.Height.Value = 0.05f;
    }

    public override void OnSecondaryPress()
    {
        RaycastHit? potentialHit = GetHit();
        if (potentialHit.HasValue)
        {
            RaycastHit hit = potentialHit.Value;
            if (hit.Collider.Slot.GetComponent<MeshRenderer>() is MeshRenderer meshRenderer)
            {
                currentMesh = meshRenderer.Mesh.Asset;
            }
        }
    }
}
