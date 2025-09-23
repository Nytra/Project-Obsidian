using System;
using System.Collections.Generic;
using System.Linq;
using Elements.Assets;
using Elements.Core;
using FrooxEngine;

namespace Obsidian.Components.Tools;

//public class EditableMeshPoint : Component
//{
//    public readonly Sync<int> MergedVertIndex;
//    public readonly SyncRef<EditableMesh> EditableMesh;

//    protected override void OnStart()
//    {
//        base.OnStart();
//        if (EditableMesh != null)
//        {
//            Slot.Position_Field.OnValueChange += (field) => { };
//        }
//    }
//}

public class EditableMesh : ProceduralMesh
{
    private const float EPSILON = 0.001f;

    public readonly SyncRefList<Slot> Points;
    //public readonly SyncArray<float3> Positions;
    protected readonly AssetRef<Mesh> _sourceMesh;

    //public readonly Sync<float> ProportionalEditingRadius;
    //public readonly Sync<float> ProportionalEditingStrength;
    //private static bool recurseUpdates = true;

    private MeshX localMeshData;
    private UnlitMaterial mat;
    
    private class MergedVertexData
    {
        public float3 pos;
        public HashSet<Vertex> vertices = new();
    }

    protected override void OnPrepareDestroy()
    {
        base.OnPrepareDestroy();
        foreach (var slot in Points)
        {
            if (slot.FilterWorldElement() != null)
            {
                slot.Destroy();
            }
        }
    }

    protected override void OnStart()
    {
        //_sourceMesh.OnTargetChange += (syncRef) =>
        //{
        //    foreach (var slot in Points)
        //    {
        //        if (slot != null)
        //            slot.Destroy();
        //    }
        //    if (_sourceMesh.Asset != null)
        //    {
        //        Init();
        //    }
        //    //MarkChangeDirty(); // not sure if this is needed?
        //};
        
        if (_sourceMesh.Asset != null)
        {
            Init();
        }
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        mat = Slot.GetComponentOrAttach<UnlitMaterial>();
        mat.TintColor.Value = colorX.Green;
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
        CreateCollider();
        Init();
    }

    private void Init()
    {
        localMeshData = new();

        localMeshData.Copy(_sourceMesh.Asset.Data);

        var mergedVertexData = CollectMergedVertexData(localMeshData);

        Points.EnsureExactCount(mergedVertexData.Count);

        for (int i = 0; i < mergedVertexData.Count; i++)
        {
            var data = mergedVertexData[i];

            Slot vertSlot;
            if (Points[i].FilterWorldElement() is null)
            {
                vertSlot = Slot.AddSlot($"MergedVert{i}");
                vertSlot.LocalPosition = data.pos;
                vertSlot.AttachSphere(0.01f, mat);
                vertSlot.AttachComponent<Slider>();
                Points[i] = vertSlot;
            }

            Update(Points[i].Position_Field);
            Points[i].Position_Field.OnValueChange += Update;
            void Update(SyncField<float3> field)
            {
                if (localMeshData is null) return;
                foreach (var vert in data.vertices)
                {
                    localMeshData.SetVertex(vert.Index, field.Value);
                }

                //if (!recurseUpdates) return;
                //recurseUpdates = false;
                //foreach (var otherSlot in slot.Parent.Children.Where(s => s != slot))
                //{
                //    var dist = MathX.Distance(otherSlot.LocalPosition, slot.LocalPosition);
                //    if (dist < ProportionalEditingRadius.Value)
                //    {
                //        var res = MathX.Remap(dist, 0, ProportionalEditingRadius.Value, 1 - ProportionalEditingStrength.Value, 1);
                //        if (res == 0f)
                //        {
                //            res = float.MinValue;
                //        }
                //        otherSlot.LocalPosition = MathX.Lerp(field.Value, otherSlot.LocalPosition, res);
                //    }
                //}
                //recurseUpdates = true;

                MarkChangeDirty();
            }
        }

        MarkChangeDirty();
    }

    protected override void ClearMeshData()
    {
        localMeshData?.Clear();
        localMeshData = null;
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
        if (localMeshData is null)
        {
            meshx.Clear();
            return;
        }

        meshx.Copy(localMeshData);
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
                var hitMesh = meshRenderer.Mesh.Asset;
                if (hitMesh != null)
                {
                    //var editableMesh = LocalUserSpace.AddSlot("EditableMesh").AttachComponent<EditableMesh>();
                    var editableMesh = hit.Collider.Slot.AttachComponent<EditableMesh>();
                    //editableMesh.Slot.CopyTransform(hit.Collider.Slot);
                    editableMesh.Setup(meshRenderer.Mesh.Target);
                    meshRenderer.Mesh.Target = editableMesh;
                    var newCol = editableMesh.Slot.GetComponent<MeshCollider>(col => col.Mesh.Target == editableMesh);
                    newCol.Type.Value = hit.Collider.ColliderType;
                    newCol.CharacterCollider.Value = hit.Collider.CharacterCollider;
                    hit.Collider.Destroy();
                }
            }
        }
    }
}
