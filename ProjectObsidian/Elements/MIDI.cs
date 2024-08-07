﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Components.Devices.MIDI;
using Elements.Core;
using FrooxEngine;

namespace Obsidian.Elements;

[DataModelType]
public enum MIDI_CC_Definition
{
    UNDEFINED = 999,
    BankSelect = 0,
    Modulation = 1,
    Breath = 2,
    Foot = 4,
    PortamentoTime = 5,
    DteMsb = 6,
    Volume = 7,
    Balance = 8,
    Pan = 10,
    Expression = 11,
    EffectControl1 = 12,
    EffectControl2 = 13,
    General1 = 16,
    General2 = 17,
    General3 = 18,
    General4 = 19,
    BankSelectLsb = 32,
    ModulationLsb = 33,
    BreathLsb = 34,
    FootLsb = 36,
    PortamentoTimeLsb = 37,
    DteLsb = 38,
    VolumeLsb = 39,
    BalanceLsb = 40,
    PanLsb = 42,
    ExpressionLsb = 43,
    Effect1Lsb = 44,
    Effect2Lsb = 45,
    General1Lsb = 48,
    General2Lsb = 49,
    General3Lsb = 50,
    General4Lsb = 51,
    Hold = 64,
    PortamentoSwitch = 65,
    Sostenuto = 66,
    SoftPedal = 67,
    Legato = 68,
    Hold2 = 69,
    SoundController1 = 70,
    SoundController2 = 71,
    SoundController3 = 72,
    SoundController4 = 73,
    SoundController5 = 74,
    SoundController6 = 75,
    SoundController7 = 76,
    SoundController8 = 77,
    SoundController9 = 78,
    SoundController10 = 79,
    General5 = 80,
    General6 = 81,
    General7 = 82,
    General8 = 83,
    PortamentoControl = 84,
    Rsd = 91,
    Effect1 = 91,
    Tremolo = 92,
    Effect2 = 92,
    Csd = 93,
    Effect3 = 93,
    Celeste = 94, //detune
    Effect4 = 94,
    Phaser = 95,
    Effect5 = 95,
    DteIncrement = 96,
    DteDecrement = 97,
    NrpnLsb = 98,
    NrpnMsb = 99,
    RpnLsb = 100,
    RpnMsb = 101,
    AllSoundOff = 120,
    ResetAllControllers = 121,
    LocalControl = 122,
    AllNotesOff = 123,
    OmniModeOff = 124,
    OmniModeOn = 125,
    PolyModeOnOff = 126,
    PolyModeOn = 127
}

[DataModelType]
public readonly struct MIDI_SystemRealtimeEventData
{
    public MIDI_SystemRealtimeEventData()
    { 
        // owo
    }
}

[DataModelType]
public readonly struct MIDI_ProgramEventData
{
    public readonly int channel;

    public readonly int program;

    public MIDI_ProgramEventData(in int _channel, in int _program)
    {
        channel = _channel;
        program = _program;
    }
}

[DataModelType]
public readonly struct MIDI_PitchWheelEventData
{
    public readonly int channel;

    public readonly int value;

    public readonly float normalizedValue => value == 8192 ? 0f : MathX.Remap(value, 0f, 16383f, -1f, 1f);

    public MIDI_PitchWheelEventData(in int _channel, in int _value)
    {
        channel = _channel;
        value = _value;
    }
}

[DataModelType]
public readonly struct MIDI_NoteEventData
{
    public readonly int channel;

    public readonly int note;

    public readonly int velocity;

    public readonly float normalizedVelocity => velocity / 127f;

    public MIDI_NoteEventData(in int _channel, in int _note, in int _velocity)
    {
        channel = _channel;
        note = _note;
        velocity = _velocity;
    }
}

[DataModelType]
public readonly struct MIDI_ChannelAftertouchEventData
{
    public readonly int channel;

    public readonly int pressure;

    public readonly float normalizedPressure => pressure / 127f;

    public MIDI_ChannelAftertouchEventData(in int _channel, in int _pressure)
    {
        channel = _channel;
        pressure = _pressure;
    }
}

[DataModelType]
public readonly struct MIDI_PolyphonicAftertouchEventData
{
    public readonly int channel;

    public readonly int note;

    public readonly int pressure;

    public readonly float normalizedPressure => pressure / 127f;

    public MIDI_PolyphonicAftertouchEventData(in int _channel, in int _note, in int _pressure)
    {
        channel = _channel;
        note = _note;
        pressure = _pressure;
    }
}

[DataModelType]
public readonly struct MIDI_CC_EventData
{
    public readonly int channel;

    public readonly int controller;

    public readonly int value;

    public readonly bool coarse; // is it 7bit (coarse) or 14bit (fine) value?

    public readonly float normalizedValue => coarse ? value / 127f : value / 16383f;

    public MIDI_CC_EventData(in int _channel, in int _controller, in int _value, in bool _coarse)
    {
        channel = _channel;
        controller = _controller;
        value = _value;
        coarse = _coarse;
    }
}

[DataModelType]
public delegate void MIDI_NoteEventHandler(MIDI_InputDevice device, MIDI_NoteEventData eventData);

[DataModelType]
public delegate void MIDI_ChannelAftertouchEventHandler(MIDI_InputDevice device, MIDI_ChannelAftertouchEventData eventData);

[DataModelType]
public delegate void MIDI_PolyphonicAftertouchEventHandler(MIDI_InputDevice device, MIDI_PolyphonicAftertouchEventData eventData);

[DataModelType]
public delegate void MIDI_CC_EventHandler(MIDI_InputDevice device, MIDI_CC_EventData eventData);

[DataModelType]
public delegate void MIDI_PitchWheelEventHandler(MIDI_InputDevice device, MIDI_PitchWheelEventData eventData);

[DataModelType]
public delegate void MIDI_ProgramEventHandler(MIDI_InputDevice device, MIDI_ProgramEventData eventData);

[DataModelType]
public delegate void MIDI_SystemRealtimeEventHandler(MIDI_InputDevice device, MIDI_SystemRealtimeEventData eventData);