using System;
using System.Collections.Generic;
using System.Linq;
using BepuPhysics.Constraints;
using Elements.Assets;
using Elements.Core;
using FrooxEngine;

namespace Obsidian.Components.Tools;

public class EditableMeshControlPoint : Component
{
    public readonly SyncRef<EditableMesh> EditableMesh;

    protected override void OnStart()
    {
        base.OnStart();
        if (EditableMesh.Target != null)
        {
            Slot.Position_Field.OnValueChange += (field) => 
            { 
                EditableMesh.Target.MarkChangeDirty();
            };
        }
    }
}

public class EditableMesh : ProceduralMesh
{
    private const float EPSILON = 0.001f;

    protected readonly AssetRef<Mesh> _sourceMesh;
    protected readonly SyncRef<Slot> _controlPointsSlot;
    public readonly SyncRef<MeshCollider> _newCollider;
    public readonly SyncRef<ICollider> _originalCollider;
    protected readonly SyncRef<UnlitMaterial> _controlPointMaterial;

    private MeshX storedMeshX;
    private List<MergedVertexData> mergedVertexData;
    
    private class MergedVertexData
    {
        public float3 pos;
        public HashSet<Vertex> vertices = new();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_controlPointsSlot.Target.FilterWorldElement() != null)
        {
            _controlPointsSlot.Target.Destroy();
        }

        if (_controlPointMaterial.Target.FilterWorldElement() != null)
        {
            _controlPointMaterial.Target.Destroy();
        }

        // If this ProceduralMesh was baked, nothing should be referencing it anymore

        UniLog.Log($"AssetReferenceCount: {AssetReferenceCount}");

        foreach (var reference in References)
        {
            UniLog.Log($"Reference: {reference.Parent.Name}");
        }

        if (AssetReferenceCount != 0 && _sourceMesh.Target.FilterWorldElement() != null)
        {
            // EditableMesh wasn't baked.

            UniLog.Log($"Wasn't baked.");

            if (_newCollider.Target.FilterWorldElement() != null)
            {
                _newCollider.Target.Destroy();
            }

            //base.World.ReplaceReferenceTargets(this, _sourceMesh.Target, nullIfIncompatible: false);

            foreach (var reference in References)
            {
                reference.Target = _sourceMesh.Target;
            }

            if (_originalCollider.Target.FilterWorldElement() != null)
            {
                _originalCollider.Target.Enabled = true;
            }
        }
        else
        {
            // EditableMesh has most likely been baked because nothing references it

            UniLog.Log($"Most likely baked.");

            if (_originalCollider.Target.FilterWorldElement() != null)
            {
                _originalCollider.Target.Destroy();
            }

            if (_sourceMesh.Target.FilterWorldElement() != null)
            {
                var assetSlot = World.AssetsSlot.AddSlot("EditableMesh Original Mesh");
                assetSlot.MoveComponent((Component)_sourceMesh.Target);
            }
        }
    }

    protected override void OnStart()
    {
        _sourceMesh.OnTargetChange += (syncRef) => 
        { 
            // If the mesh changes, the control points should regen
            // does the collider need to exist???

            //if (_controlPointsSlot.Target.FilterWorldElement() != null)
            //{
            //    _controlPointsSlot.Target.Destroy();
            //    InitControlPoints();
            //}
        };
        if (_sourceMesh.Asset != null && _controlPointsSlot.Target is null)
        {
            InitControlPoints();
        }
    }

    public void Setup(IAssetProvider<Mesh> meshProvider)
    {
        if (meshProvider == null)
        {
            throw new ArgumentNullException(nameof(meshProvider));
        }
        if (meshProvider.Asset == null)
        {
            throw new ArgumentException("Provided mesh asset is null!");
        }
        _sourceMesh.Target = meshProvider;
        _controlPointMaterial.Target = Slot.AttachComponent<UnlitMaterial>();
        _controlPointMaterial.Target.TintColor.Value = colorX.Green;
        InitControlPoints();
    }

    private void InitControlPoints()
    {
        var mergedVertexData = CollectMergedVertexData(_sourceMesh.Asset.Data);

        _controlPointsSlot.Target = Slot.AddSlot("EditableMesh Control Points");

        for (int i = 0; i < mergedVertexData.Count; i++)
        {
            var data = mergedVertexData[i];

            var vertSlot = _controlPointsSlot.Target.AddSlot($"ControlPoint{i}");
            vertSlot.ChildIndex = i;
            vertSlot.LocalPosition = data.pos;
            vertSlot.AttachSphere(0.01f, _controlPointMaterial.Target);
            vertSlot.AttachComponent<Slider>();
            var controlPointComp = vertSlot.AttachComponent<EditableMeshControlPoint>();
            controlPointComp.EditableMesh.Target = this;
        }

        //MarkChangeDirty();
    }

    protected override void ClearMeshData()
    {
        storedMeshX?.Clear();
        storedMeshX = null;
        mergedVertexData?.Clear();
        mergedVertexData = null;
    }

    private static bool ShouldMerge(float3 p1, float3 p2)
    {
        return MathX.Approximately(p1, p2, EPSILON);
    }

    private static List<MergedVertexData> CollectMergedVertexData(MeshX mesh)
    {
        var result = new List<MergedVertexData>();
        for (int i = 0; i < mesh.VertexCount; i++)
        {
            var vert = mesh.GetVertex(i);
            MergedVertexData data = result.FirstOrDefault(d => ShouldMerge(d.pos, vert.Position));
            if (data == null)
            {
                data = new MergedVertexData();
                data.pos = vert.Position;
                result.Add(data);
            }
            else
            {
                continue;
            }
            data.vertices.Add(vert);
            for (int j = 0; j < mesh.VertexCount; j++)
            {
                var vert2 = mesh.GetVertex(j);
                if (vert.Index == vert2.Index) continue;
                if (ShouldMerge(vert.Position, vert2.Position))
                {
                    data.vertices.Add(vert2);
                }
            }
        }
        return result;
    }

    protected override void UpdateMeshData(MeshX meshx)
    {
        if (_sourceMesh.Asset?.Data is null)
        {
            meshx.Clear();
            _sourceMesh.ListenToAssetUpdates = true;
            UniLog.Log($"{nameof(_sourceMesh)} Asset null. Listening for updates...");
            return;
        }

        if (_sourceMesh.ListenToAssetUpdates)
        {
            UniLog.Log($"{nameof(_sourceMesh)} Asset loaded. No longer listening for updates.");
            _sourceMesh.ListenToAssetUpdates = false;
        }

        if (storedMeshX != meshx)
        {
            UniLog.Log($"Storing new MeshX data.");
            storedMeshX = meshx;
            storedMeshX.Copy(_sourceMesh.Asset.Data);
            mergedVertexData = CollectMergedVertexData(storedMeshX);
        }

        for (int i = 0; i < mergedVertexData.Count; i++)
        {
            var data = mergedVertexData[i];

            Slot vertSlot = _controlPointsSlot.Target[i]; // Could throw null ref exception but it's probably okay

            foreach (var vert in data.vertices)
            {
                storedMeshX.SetVertex(vert.Index, vertSlot.LocalPosition);
            }
        }
    }
}

[Category("Obsidian/Tools")]
public class MeshEditTool : Tool
{
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
                var hitMesh = meshRenderer.Mesh.Target;
                if (hitMesh?.Asset?.Data != null && hitMesh is StaticMesh && hit.Collider.Slot.GetComponent<EditableMesh>() is null)
                {
                    var editableMesh = hit.Collider.Slot.AttachComponent<EditableMesh>();
                    editableMesh.Setup(hitMesh);
                    meshRenderer.Mesh.Target = editableMesh;
                    
                    MeshCollider meshCollider;
                    if (hit.Collider is MeshCollider existingMeshCollider && existingMeshCollider.Mesh.Target == hitMesh)
                    {
                        meshCollider = existingMeshCollider;
                    }
                    else
                    {
                        meshCollider = hit.Collider.Slot.AttachComponent<MeshCollider>();
                        meshCollider.Type.Value = hit.Collider.ColliderType;
                        meshCollider.CharacterCollider.Value = hit.Collider.CharacterCollider;
                        editableMesh._newCollider.Target = meshCollider;

                        hit.Collider.Enabled = false;
                        editableMesh._originalCollider.Target = hit.Collider;
                    }

                    meshCollider.Mesh.Target = editableMesh;
                }
            }
        }
    }
}
