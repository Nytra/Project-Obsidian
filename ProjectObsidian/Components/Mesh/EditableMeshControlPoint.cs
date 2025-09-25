using Elements.Data;
using FrooxEngine;
using Obsidian.Assets.ProceduralMeshes;

namespace Obsidian.Components.Utility;

[OldTypeName("Obsidian.Components.Tools.EditableMeshControlPoint")]
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