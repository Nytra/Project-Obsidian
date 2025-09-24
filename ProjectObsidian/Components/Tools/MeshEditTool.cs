using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepuPhysics.Constraints;
using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;

namespace Obsidian.Components.Tools;

[Category("Obsidian/Utility")]
public class EditableMeshControlPoint : Component
{
    protected readonly SyncRef<EditableMesh> _editableMesh;

    public EditableMesh EditableMesh => _editableMesh.Target;

    public void Setup(EditableMesh editableMesh)
    {
        _editableMesh.Target = editableMesh;
    }

    protected override void OnDestroying()
    {
        base.OnDestroying();
        _editableMesh.Target?.Destroy();
    }

    protected override void OnStart()
    {
        base.OnStart();
        Slot.Position_Field.OnValueChange += (field) =>
        {
            _editableMesh.Target?.MarkChangeDirty();
        };
    }
}

[Category("Obsidian/Assets/Procedural Meshes")]
public class EditableMesh : ProceduralMesh
{
    private const float EPSILON = 0.001f;

    protected readonly AssetRef<Mesh> _sourceMesh;
    protected readonly SyncRef<Slot> _controlPointsSlot;
    protected readonly SyncRef<MeshCollider> _newCollider;
    protected readonly SyncRef<ICollider> _originalCollider;
    protected readonly SyncRef<UnlitMaterial> _controlPointMaterial;

    private MeshX storedMeshX;
    private List<MergedVertexData> mergedVertexData;
    
    private class MergedVertexData
    {
        public float3 pos;
        public HashSet<Vertex> vertices = new();
    }

    private void DebugLog(string msg)
    {
        UniLog.Log($"[EditableMesh {this.ReferenceID.ToString()} {_sourceMesh.Target?.ReferenceID.ToString() ?? "NULL"}] {msg}");
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        _controlPointsSlot.Target?.Destroy();

        _controlPointMaterial.Target?.Destroy();

        // If this ProceduralMesh was baked, nothing should be referencing it anymore

        DebugLog($"[EditableMesh] OnDestroy. AssetReferenceCount: {AssetReferenceCount}");

        foreach (var reference in References)
        {
            DebugLog($"Reference: {reference.Parent.Name}");
        }

        if (AssetReferenceCount != 0)
        {
            // EditableMesh wasn't baked.

            DebugLog($"Wasn't baked.");

            _newCollider.Target?.Destroy();

            foreach (var reference in References)
            {
                reference.Target = _sourceMesh.Target;
            }

            if (_originalCollider.Target != null)
            {
                _originalCollider.Target.Enabled = true;
            }
        }
        else
        {
            // EditableMesh has most likely been baked because nothing references it

            DebugLog($"Baked.");

            _originalCollider.Target?.Destroy();

            if (_sourceMesh.Target != null)
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
            // Handle mesh changes in here?
            // Should regen control points and clear mesh data
        };

        //if (_sourceMesh.Asset != null && _controlPointsSlot.Target is null)
        //{
            //InitControlPoints();
        //}
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

    public void SetColliders(ICollider originalCollider, MeshCollider newCollider)
    {
        if (originalCollider == null)
        {
            throw new ArgumentNullException($"{nameof(originalCollider)} is null!");
        }
        if (newCollider == null)
        {
            throw new ArgumentNullException($"{nameof(newCollider)} is null!");
        }
        if (newCollider.Mesh.Target != this)
        {
            throw new ArgumentException($"{nameof(newCollider)} doesn't target this EditableMesh!");
        }
        _originalCollider.Target = originalCollider;
        _newCollider.Target = newCollider;
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
            controlPointComp.Setup(this);
        }
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
            DebugLog($"{nameof(_sourceMesh)} Asset null. Listening for updates...");
            return;
        }

        if (_sourceMesh.ListenToAssetUpdates)
        {
            DebugLog($"{nameof(_sourceMesh)} Asset loaded. No longer listening for updates.");
            _sourceMesh.ListenToAssetUpdates = false;
        }

        if (storedMeshX != meshx)
        {
            DebugLog($"Storing new MeshX data.");
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
    protected readonly SyncRef<EditableMesh> _currentEditableMesh;

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

    public override void GenerateMenuItems(InteractionHandler tool, ContextMenu menu)
    {
        base.GenerateMenuItems(tool, menu);
        if (_currentEditableMesh.Target != null)
        {
            menu.AddItem("Restore", OfficialAssets.Common.Icons.Rubbish, new colorX?(colorX.Red), OnRestore);
            menu.AddItem("Bake", OfficialAssets.Graphics.Icons.General.Save, new colorX?(colorX.Green), OnBake);
        }
    }

    [SyncMethod(typeof(Delegate), null)]
    private void OnRestore(IButton button, ButtonEventData eventData)
    {
        Restore();
        base.ActiveHandler?.CloseContextMenu();
    }

    [SyncMethod(typeof(Delegate), null)]
    private void OnBake(IButton button, ButtonEventData eventData)
    {
        Bake();
        base.ActiveHandler?.CloseContextMenu();
    }

    private void Restore()
    {
        _currentEditableMesh.Target?.Destroy();
        _currentEditableMesh.Target = null;
    }

    private void Bake()
    {
        _currentEditableMesh.Target?.BakeMesh();
        _currentEditableMesh.Target = null;
    }

    public override void OnPrimaryPress()
    {
        if (_currentEditableMesh.Target != null)
        {
            return;
        }

        RaycastHit? potentialHit = GetHit();
        if (potentialHit.HasValue)
        {
            RaycastHit hit = potentialHit.Value;
            Slot targetSlot = hit.Collider.Slot;
            if (targetSlot.GetComponent<MeshRenderer>() is MeshRenderer meshRenderer)
            {
                if (targetSlot.GetComponent<EditableMeshControlPoint>() is EditableMeshControlPoint controlPoint)
                {
                    if (controlPoint.EditableMesh?.Slot is Slot editableMeshSlot)
                        targetSlot = editableMeshSlot;
                    else
                        return;
                }

                if (targetSlot.GetComponent<EditableMesh>() is EditableMesh existingEditableMesh)
                {
                    _currentEditableMesh.Target = existingEditableMesh;
                    HighlightHelper.FlashHighlight(targetSlot, highlightable => highlightable == meshRenderer, colorX.Green);
                    return;
                }

                var hitMesh = meshRenderer.Mesh.Target;
                if (hitMesh?.Asset?.Data != null)
                {
                    if (hitMesh is StaticMesh)
                    {
                        var editableMesh = targetSlot.AttachComponent<EditableMesh>();

                        meshRenderer.Mesh.Target = editableMesh;

                        if (hit.Collider is MeshCollider existingMeshCollider && existingMeshCollider.Mesh.Target == hitMesh)
                        {
                            existingMeshCollider.Mesh.Target = editableMesh;
                        }
                        else
                        {
                            var newCollider = targetSlot.AttachComponent<MeshCollider>();
                            newCollider.Type.Value = hit.Collider.ColliderType;
                            newCollider.CharacterCollider.Value = hit.Collider.CharacterCollider;
                            newCollider.Mesh.Target = editableMesh;

                            hit.Collider.Enabled = false;

                            editableMesh.SetColliders(hit.Collider, newCollider);
                        }

                        editableMesh.Setup(hitMesh);
                        _currentEditableMesh.Target = editableMesh;
                        HighlightHelper.FlashHighlight(targetSlot, highlightable => highlightable == meshRenderer, colorX.Green);
                    }
                    else
                    {
                        HighlightHelper.FlashHighlight(targetSlot, highlightable => highlightable == meshRenderer, colorX.Red);
                        UserRoot userroot = World.LocalUser.Root;
                        NotificationMessage.SpawnTextMessage(World, userroot.HeadPosition + userroot.HeadSlot.Forward * 0.5f, "The target must be a StaticMesh!", colorX.Red);
                    }
                }
            }
        }
    }
}
