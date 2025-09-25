﻿using Elements.Core;
using FrooxEngine;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Obsidian.Elements;
using Elements.Data;

namespace Obsidian.Components.Devices.MIDI;

[Category(new string[] { "Obsidian/Devices/MIDI" })]
[OldTypeName("Components.Devices.MIDI.MIDI_PitchWheel_Value")]
public class MIDI_PitchWheel_Value : Component
{
    public readonly SyncRef<MIDI_InputDevice> InputDevice;

    public readonly Sync<bool> AutoMap;

    public readonly Sync<int> Channel;

    public readonly Sync<int> Value;

    public readonly Sync<float> NormalizedValue;

    private MIDI_InputDevice _device;

    protected override void OnStart()
    {
        base.OnStart();
        InputDevice.OnTargetChange += OnTargetChange;
        if (InputDevice.Target != null)
        {
            _device = InputDevice.Target;
            InputDevice.Target.PitchWheel += OnPitchWheel;
        }
    }

    protected override void OnDispose()
    {
        base.OnDispose();
        if (_device != null)
        {
            _device.PitchWheel -= OnPitchWheel;
            _device = null;
        }
    }

    private void OnPitchWheel(IMidiInputListener sender, MIDI_PitchWheelEventData eventData)
    {
        RunSynchronously(() =>
        {
            if (AutoMap.Value)
            {
                AutoMap.Value = false;
                Channel.Value = eventData.channel;
            }
            if (eventData.channel == Channel.Value)
            {
                Value.Value = eventData.value;
                NormalizedValue.Value = eventData.value == 8192 ? 0f : MathX.Remap(eventData.value, 0f, 16383f, -1f, 1f);
            }
        });
    }

    private void OnTargetChange(SyncRef<MIDI_InputDevice> syncRef)
    {
        if (syncRef.Target == null && _device != null)
        {
            _device.PitchWheel -= OnPitchWheel;
            _device = null;
        }
        else if (syncRef.Target != null)
        {
            if (_device != null)
            {
                _device.PitchWheel -= OnPitchWheel;
            }
            _device = syncRef.Target;
            _device.PitchWheel += OnPitchWheel;
        }
    }
}