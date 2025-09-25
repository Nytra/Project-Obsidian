using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elements.Core;
using Elements.Data;
using FrooxEngine;
using Obsidian.Components.Devices.MIDI;
using RtMidi;
using RtMidi.Core;
using RtMidi.Core.Devices;
using RtMidi.Core.Devices.Infos;
using RtMidi.Core.Messages;

namespace Obsidian.Elements;

//public struct TimestampedMidiEvent
//{
//    public MidiEvent midiEvent;
//    public long timestamp;
//    public TimestampedMidiEvent(MidiEvent _midiEvent, long _timestamp)
//    {
//        midiEvent = _midiEvent;
//        timestamp = _timestamp;
//    }
//}

public interface IMidiInputListener
{
    public event MIDI_NoteEventHandler NoteOn;
    public event MIDI_NoteEventHandler NoteOff;

    // Pressure for whole keyboard
    public event MIDI_ChannelAftertouchEventHandler ChannelAftertouch;

    // Pressure for individual notes (polyphonic)
    public event MIDI_PolyphonicAftertouchEventHandler PolyphonicAftertouch;
    public event MIDI_CC_EventHandler Control;
    public event MIDI_PitchWheelEventHandler PitchWheel;
    public event MIDI_ProgramEventHandler Program;
    public event MIDI_SystemRealtimeEventHandler MidiClock;
    public event MIDI_SystemRealtimeEventHandler MidiTick;
    public event MIDI_SystemRealtimeEventHandler MidiStart;
    public event MIDI_SystemRealtimeEventHandler MidiStop;
    public event MIDI_SystemRealtimeEventHandler MidiContinue;
    public event MIDI_SystemRealtimeEventHandler ActiveSense;
    public event MIDI_SystemRealtimeEventHandler Reset;

    public void TriggerNoteOn(MIDI_NoteEventData eventData);
    public void TriggerNoteOff(MIDI_NoteEventData eventData);
    public void TriggerChannelAftertouch(MIDI_ChannelAftertouchEventData eventData);
    public void TriggerPolyphonicAftertouch(MIDI_PolyphonicAftertouchEventData eventData);
    public void TriggerControl(MIDI_CC_EventData eventData);
    public void TriggerPitchWheel(MIDI_PitchWheelEventData eventData);
    public void TriggerProgram(MIDI_ProgramEventData eventData);
    public void TriggerMidiClock(MIDI_SystemRealtimeEventData eventData);
    public void TriggerMidiTick(MIDI_SystemRealtimeEventData eventData);
    public void TriggerMidiStart(MIDI_SystemRealtimeEventData eventData);
    public void TriggerMidiStop(MIDI_SystemRealtimeEventData eventData);
    public void TriggerMidiContinue(MIDI_SystemRealtimeEventData eventData);
    public void TriggerActiveSense(MIDI_SystemRealtimeEventData eventData);
    public void TriggerReset(MIDI_SystemRealtimeEventData eventData);
}

public class MidiInputConnection
{
    public IMidiInputDevice Input;

    public List<IMidiInputListener> Listeners = new();

    //event NoteOffMessageHandler NoteOff;

    //event NoteOnMessageHandler NoteOn;

    //event PolyphonicKeyPressureMessageHandler PolyphonicKeyPressure;

    //event ControlChangeMessageHandler ControlChange;

    //event ProgramChangeMessageHandler ProgramChange;

    //event ChannelPressureMessageHandler ChannelPressure;

    //event PitchBendMessageHandler PitchBend;

    //event NrpnMessageHandler Nrpn;

    //event SysExMessageHandler SysEx;

    //event MidiTimeCodeQuarterFrameHandler MidiTimeCodeQuarterFrame;

    //event SongPositionPointerHandler SongPositionPointer;

    //event SongSelectHandler SongSelect;

    //event TuneRequestHandler TuneRequest;

    public void OnNoteOff(IMidiInputDevice sender, in NoteOffMessage msg)
    {
        if (DEBUG) UniLog.Log("* NoteOff");
        var data = new MIDI_NoteEventData((int)msg.Channel, (int)msg.Key, msg.Velocity);
        Listeners.ForEach(l => l.TriggerNoteOff(data));
    }

    public void OnNoteOn(IMidiInputDevice sender, in NoteOnMessage msg)
    {
        if (DEBUG) UniLog.Log("* NoteOn");
        var data = new MIDI_NoteEventData((int)msg.Channel, (int)msg.Key, msg.Velocity);
        Listeners.ForEach(l => l.TriggerNoteOn(data));
    }

    private const bool DEBUG = true;

    public void Initialize()
    {
        Listeners.Clear();
    }
}

public static class MidiDeviceConnectionManager
{
    private static Dictionary<string, MidiInputConnection> _deviceConnectionMap = new();

    private static Dictionary<IMidiInputListener, MidiInputConnection> _listenerConnectionMap = new();

    public static MidiInputConnection RegisterInputListener(IMidiInputListener listener, IMidiInputDeviceInfo details)
    {
        if (_deviceConnectionMap.TryGetValue(details.Name, out MidiInputConnection conn))
        {
            conn.Listeners.Add(listener);
            return conn;
        }
        var newConn = CreateInputConnection(details);
        newConn.Listeners.Add(listener);
        _listenerConnectionMap.Add(listener, newConn);
        return newConn;
    }

    public static void UnregisterInputListener(IMidiInputListener listener)
    {
        if (_listenerConnectionMap.TryGetValue(listener, out MidiInputConnection conn))
        {
            conn.Listeners.Remove(listener);
            _listenerConnectionMap.Remove(listener);
            if (conn.Listeners.Count == 0)
            {
                UniLog.Log("No more listeners. Releasing input device connection. Device name: " + conn.Input.Name);
                ReleaseInputConnection(conn);
            }
        }
    }

    private static void ReleaseInputConnection(MidiInputConnection conn)
    {
        UniLog.Log("Releasing input device...");

        var input = conn.Input;
        input.NoteOn -= conn.OnNoteOn;
        input.NoteOff -= conn.OnNoteOff;
        input.Dispose();

        UniLog.Log("Device released.");
        _deviceConnectionMap.Remove(input.Name);
        conn.Initialize();
        Pool<MidiInputConnection>.ReturnCleaned(ref conn);
    }

    private static MidiInputConnection CreateInputConnection(IMidiInputDeviceInfo details)
    {
        var input = details.CreateDevice();
        var conn = Pool<MidiInputConnection>.Borrow();
        conn.Input = input;
        input.NoteOn += conn.OnNoteOn;
        input.NoteOff += conn.OnNoteOff;
        input.Open();
        _deviceConnectionMap.Add(details.Name, conn);
        return conn;
    }
}

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
public delegate void MIDI_NoteEventHandler(IMidiInputListener sender, MIDI_NoteEventData eventData);

[DataModelType]
public delegate void MIDI_ChannelAftertouchEventHandler(IMidiInputListener sender, MIDI_ChannelAftertouchEventData eventData);

[DataModelType]
public delegate void MIDI_PolyphonicAftertouchEventHandler(IMidiInputListener sender, MIDI_PolyphonicAftertouchEventData eventData);

[DataModelType]
public delegate void MIDI_CC_EventHandler(IMidiInputListener sender, MIDI_CC_EventData eventData);

[DataModelType]
public delegate void MIDI_PitchWheelEventHandler(IMidiInputListener sender, MIDI_PitchWheelEventData eventData);

[DataModelType]
public delegate void MIDI_ProgramEventHandler(IMidiInputListener sender, MIDI_ProgramEventData eventData);

[DataModelType]
public delegate void MIDI_SystemRealtimeEventHandler(IMidiInputListener sender, MIDI_SystemRealtimeEventData eventData);