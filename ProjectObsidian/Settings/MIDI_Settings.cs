﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux.CoreNodes;
using RtMidi.Core;
using RtMidi.Core.Devices.Infos;

namespace Obsidian;

[SettingCategory("Obsidian")]
public class MIDI_Settings : SettingComponent<MIDI_Settings>
{
    public override bool UserspaceOnly => true;

    public class MIDI_Device : SyncObject
    {
        [SettingIndicatorProperty(null, null, null, null, false, 0L)]
        public readonly Sync<string> DeviceName;

        // Can't use the NonPersistent attribute until this is fixed: https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/5248
        // This could be worked around by using a publicizer / IgnoresAccessChecksTo attribute, but it isn't a huge deal 
        //[NonPersistent]
        [SettingIndicatorProperty(null, null, null, null, false, 0L)]
        public readonly Sync<bool> DeviceFound;

        [SettingProperty(null, null, null, false, 0L, null, null)]
        public readonly Sync<bool> AllowConnections;

        public IMidiDeviceInfo Details { get; internal set; }

        public bool IsOutput => this.Parent.Name == "OutputDevices";

        protected override void OnAwake()
        {
            base.OnAwake();
        }

        [SettingProperty(null, null, null, false, 0L, null, null)]
        [SyncMethod(typeof(Action), new string[] { })]
        public void CopyName()
        {
            InputInterface.Clipboard.SetText(DeviceName.Value);
        }

        [SettingProperty(null, null, null, false, 0L, null, null)]
        [SyncMethod(typeof(Action), new string[] { })]
        public void Remove()
        {
            this.FindNearestParent<SyncList<MIDI_Device>>().Remove(this);
        }
    }

    [SettingSubcategoryList("DeviceToItem", null, null, null, null, null)]
    public readonly SyncList<MIDI_Device> InputDevices;

    [SettingSubcategoryList("DeviceToItem", null, null, null, null, null)]
    public readonly SyncList<MIDI_Device> OutputDevices;

    private DataFeedItem DeviceToItem(ISyncMember item)
    {
        MIDI_Device device = (MIDI_Device)item;
        DataFeedGroup dataFeedGroup = new DataFeedGroup();
        List<DataFeedItem> list = new List<DataFeedItem>();
        var subcat = device.IsOutput ? "OutputDevices" : "InputDevices";
        var getter = device.IsOutput ? "GetOutputDeviceForSubsetting" : "GetInputDeviceForSubsetting";
        foreach (DataFeedItem item2 in SettingsDataFeed.EnumerateSettingProperties(typeof(MIDI_Device), null, typeof(MIDI_Settings), subcat, getter, device.DeviceName.Value))
        {
            // Simplify locale key
            var parts = item2.ItemKey.Split('.');
            var newLocaleKey = "Settings." + string.Join(".", parts.Take(2));
            item2.InitBase(item2.ItemKey, null, null, newLocaleKey.AsLocaleKey());
            list.Add(item2);
        }
        dataFeedGroup.InitBase(device.DeviceName.Value, null, null, device.DeviceName.Value, null, null, null, list);
        return dataFeedGroup;
    }

    [SyncMethod(typeof(SubsettingGetter), new string[] { })]
    public SyncObject GetInputDeviceForSubsetting(string key)
    {
        return InputDevices.FirstOrDefault((d) => d.DeviceName.Value == key);
    }

    [SyncMethod(typeof(SubsettingGetter), new string[] { })]
    public SyncObject GetOutputDeviceForSubsetting(string key)
    {
        return OutputDevices.FirstOrDefault((d) => d.DeviceName.Value == key);
    }

    protected override void OnStart()
    {
        RefreshDeviceLists();
    }

    [SettingProperty(null, null, null, false, 0L, null, null)]
    [SyncMethod(typeof(Action), new string[] { })]
    public void RefreshDeviceLists()
    {
        UniLog.Log("Refreshing MIDI device lists!");
        foreach(var device in InputDevices.Concat(OutputDevices)) 
        {
            device.DeviceFound.Value = false;
        }
        var access = MidiDeviceManager.Default;
        foreach (var input in access.InputDevices)
        {
            RegisterInputDevice(input);
        }
        foreach (var output in access.OutputDevices)
        {
            RegisterOutputDevice(output);
        }
    }

    private void RegisterInputDevice(IMidiInputDeviceInfo details)
    {
        if (string.IsNullOrEmpty(details.Name))
        {
            return;
        }
        MIDI_Device device = InputDevices.FirstOrDefault((d) => d.DeviceName.Value == details.Name);
        if (device == null)
        {
            device = InputDevices.Add();
            device.DeviceName.Value = details.Name;
        }
        device.Details = details;
        device.DeviceFound.Value = true;
    }

    private void RegisterOutputDevice(IMidiOutputDeviceInfo details)
    {
        if (string.IsNullOrEmpty(details.Name))
        {
            return;
        }
        MIDI_Device device = OutputDevices.FirstOrDefault((d) => d.DeviceName.Value == details.Name);
        if (device == null)
        {
            device = OutputDevices.Add();
            device.DeviceName.Value = details.Name;
        }
        device.Details = details;
        device.DeviceFound.Value = true;
    }
}