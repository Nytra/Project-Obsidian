using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elements.Core;
using FrooxEngine;
using Obsidian.Components.Tools;
using Obsidian.Shaders;

namespace Obsidian;

public static class ObsidianInitializer
{
    private static bool _initialized = false;
    public static void Initialize()
    {
        if (_initialized) return;

        ShaderInjection.AppendShaders();
        DevCreateNewForm.AddAction("Obsidian", "MeshEditTool", (slot) =>
        {
            if (!slot.World.Types.IsSupported(typeof(MeshEditTool)))
            {
                NotificationMessage.SpawnTextMessage("Obsidian is not enabled in this world!", colorX.Red);
                slot.Destroy();
                return;
            }
            slot.AttachComponent<MeshEditTool>();
        });

        _initialized = true;
    }
}