using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepuPhysics.Constraints;
using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using Obsidian.Assets.ProceduralMeshes;
using Obsidian.Components.Utility;

namespace Obsidian.Components.Tools;

[Category("Obsidian/Tools")]
public class MeshEditTool : Tool
{
    protected readonly DriveRef<PBS_Metallic> _material;
    protected readonly DestroyRelayRef<EditableMesh> _currentEditableMesh;
    protected readonly SyncRef<TextRenderer> _label;

    public override bool UsesLaser => true;

    protected override void OnAttach()
    {
        base.OnAttach();

        Slot tip = base.Slot.AddSlot("TipReference");
        tip.LocalPosition = float3.Forward * 0.075f;
        TipReference.Target = tip;

        Slot visual = base.Slot.AddSlot("Visual");
        visual.AttachComponent<SphereCollider>().Radius.Value = 0.02f;
        visual.LocalRotation = floatQ.Euler(90f, 0f, 0f);
        visual.LocalPosition += float3.Forward * 0.05f;
        _material.Target = visual.AttachComponent<PBS_Metallic>();
        ConeMesh coneMesh = visual.AttachMesh<ConeMesh>(_material.Target);
        coneMesh.RadiusTop.Value = 0.0025f;
        coneMesh.RadiusBase.Value = 0.015f;
        coneMesh.Height.Value = 0.05f;

        Slot labelPivot = Slot.AddSlot("Label Pivot");
        labelPivot.LocalRotation = floatQ.Euler(0f, 0f, 45f);

        Slot slot2 = labelPivot.AddSlot("Label");
        TextRenderer label = slot2.AttachComponent<TextRenderer>();
        TextUnlitMaterial fontMaterial = slot2.AttachComponent<TextUnlitMaterial>();
        fontMaterial.FaceDilate.Value = 0.2f;
        fontMaterial.OutlineThickness.Value = 0.2f;
        fontMaterial.OutlineColor.Value = colorX.Black;
        label.Size.Value *= 0.15f;
        label.Material.Target = fontMaterial;
        slot2.LocalPosition += float3.Up * 0.05f;
        label.Text.Value = "---";
        _label.Target = label;
    }

    protected override void OnChanges()
    {
        base.OnChanges();
        if (_currentEditableMesh.Target != null)
        {
            _label.Target.Text.Value = $"Editing: {_currentEditableMesh.Target.Slot.Name}";
            _label.Target.Color.Value = colorX.White;
            _material.Target.AlbedoColor.Value = colorX.Green;
        }
        else
        {
            _label.Target.Text.Value = $"---";
            _label.Target.Color.Value = colorX.White;
            _material.Target.AlbedoColor.Value = colorX.White;
        }
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
            _label.Target.Text.Value = "Already editing!";
            _label.Target.Color.Value = colorX.Orange;
            RunInSeconds(2.5f, MarkChangeDirty);
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
