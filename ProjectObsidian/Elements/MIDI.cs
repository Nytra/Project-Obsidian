using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Commons.Music.Midi;
using Elements.Core;
using Elements.Data;

namespace Obsidian.Elements;

public struct TimestampedMidiEvent
{
    public MidiEvent evt;
    public long timestamp;
    public TimestampedMidiEvent(MidiEvent _evt, long _timestamp)
    {
        evt = _evt;
        timestamp = _timestamp;
    }
    public override string ToString()
    {
        return $"{timestamp}, {evt}";
    }
}

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
    public IMidiInput Input;

    public List<IMidiInputListener> Listeners = new();

    private List<TimestampedMidiEvent> _eventBuffer = new();

    private const long BUFFER_TIME_MILLISECONDS = 3;

    private long _lastEventBufferStartTime = 0;

    private const bool DEBUG = false;

    public void Initialize()
    {
        Input = null;
        _eventBuffer.Clear();
        _lastEventBufferStartTime = 0;
        Listeners.Clear();
    }

    // What
    private ushort CombineBytes(byte First, byte Second)
    {
        ushort _14bit;
        _14bit = Second;
        _14bit <<= 7;
        _14bit |= First;
        return _14bit;
    }

    private bool IsCCFineMessage()
    {
        if (_eventBuffer.Count < 2) return false;
        if (_eventBuffer[0].evt.EventType == MidiEvent.CC && _eventBuffer[1].evt.EventType == MidiEvent.CC
            && _eventBuffer[0].evt.Msb == _eventBuffer[1].evt.Msb - 32)
        {
            return true;
        }
        return false;
    }

    private void FlushMessageBuffer()
    {
        if (_eventBuffer.Count == 0)
        {
            UniLog.Log("Message buffer empty.");
            return;
        }

        var batchStartTime = _eventBuffer[0].timestamp;
        if (DEBUG) UniLog.Log("Flushing message buffer from start time: " + batchStartTime.ToString());

        while (_eventBuffer.Count > 0)
        {
            // 14bit CC
            while (IsCCFineMessage())
            {
                var e1 = _eventBuffer[0];
                if (DEBUG) UniLog.Log(e1.ToString());
                var e2 = _eventBuffer[1];
                if (DEBUG) UniLog.Log(e2.ToString());
                var finalValue = CombineBytes(e2.evt.Lsb, e1.evt.Lsb);
                if (DEBUG) UniLog.Log($"CC fine. Value: " + finalValue.ToString());
                Listeners.ForEach(l => l.TriggerControl(new MIDI_CC_EventData(e1.evt.Channel, e1.evt.Msb, finalValue, _coarse: false)));
                _eventBuffer.RemoveRange(0, 2);
            }

            if (_eventBuffer.Count == 0) break;

            var e = _eventBuffer[0];
            if (DEBUG) UniLog.Log(e.ToString());
            switch (_eventBuffer[0].evt.EventType)
            {
                case MidiEvent.CC:
                    if (DEBUG) UniLog.Log("CC");
                    Listeners.ForEach(l => l.TriggerControl(new MIDI_CC_EventData(e.evt.Channel, e.evt.Msb, e.evt.Lsb, _coarse: true)));
                    break;

                // Program events are buffered because they can be sent after a CC fine message for Bank Select, one of my devices sends consecutively: CC (Bank Select) -> CC (Bank Select Lsb) -> Program for some buttons
                case MidiEvent.Program:
                    if (DEBUG) UniLog.Log("Program");
                    Listeners.ForEach(l => l.TriggerProgram(new MIDI_ProgramEventData(e.evt.Channel, e.evt.Msb)));
                    break;

                default:
                    // This should never happen
                    if (DEBUG) UniLog.Log($"Unrecognized event type! {_eventBuffer[0].evt.EventType}");
                    break;
            }
            _eventBuffer.RemoveAt(0);
        }
        if (DEBUG) UniLog.Log("Finished flushing message buffer from start time: " + batchStartTime.ToString());
    }

    public async void OnMessageReceived(object sender, MidiReceivedEventArgs args)
    {
        if (DEBUG) UniLog.FlushEveryMessage = true;

        if (DEBUG) UniLog.Log($"*** New midi message");
        if (DEBUG) UniLog.Log($"* Received {args.Length} bytes");
        if (DEBUG) UniLog.Log($"* Start: {args.Start}");

        if (DEBUG) UniLog.Log($"* Raw bytes: {string.Join(",", args.Data.Skip(args.Start).Take(args.Length).Select(b => string.Format("{0:X}", b)))}");

        long timestamp = args.Timestamp;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) // Timestamp is always zero on Linux with the Alsa Midi API
        {
            timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        if (DEBUG) UniLog.Log($"* Timestamp: {timestamp}");

        var events = MidiEvent.Convert(args.Data, args.Start, args.Length);

        // DO NOT CALL events.Count() IT BREAKS THE RUNNING STATUS ON LINUX

        foreach (var e in events)
        {
            if (DEBUG)
            {
                var str = e.ToString();
                UniLog.Log("* " + str);
            }

            bool shouldBuffer = false;
            switch (e.EventType)
            {
                // System realtime
                case MidiEvent.MidiClock:
                    if (DEBUG) UniLog.Log("* MidiClock");
                    Listeners.ForEach(l => l.TriggerMidiClock(new MIDI_SystemRealtimeEventData()));
                    break;
                case MidiEvent.MidiTick:
                    if (DEBUG) UniLog.Log("* MidiTick");
                    Listeners.ForEach(l => l.TriggerMidiTick(new MIDI_SystemRealtimeEventData()));
                    break;
                case MidiEvent.MidiStart:
                    if (DEBUG) UniLog.Log("* MidiStart");
                    Listeners.ForEach(l => l.TriggerMidiStart(new MIDI_SystemRealtimeEventData()));
                    break;
                case MidiEvent.MidiStop:
                    if (DEBUG) UniLog.Log("* MidiStop");
                    Listeners.ForEach(l => l.TriggerMidiStop(new MIDI_SystemRealtimeEventData()));
                    break;
                case MidiEvent.MidiContinue:
                    if (DEBUG) UniLog.Log("* MidiContinue");
                    Listeners.ForEach(l => l.TriggerMidiContinue(new MIDI_SystemRealtimeEventData()));
                    break;
                case MidiEvent.ActiveSense:
                    if (DEBUG) UniLog.Log("* ActiveSense");
                    Listeners.ForEach(l => l.TriggerActiveSense(new MIDI_SystemRealtimeEventData()));
                    break;
                case MidiEvent.Reset:
                    // Same as Meta
                    if (DEBUG) UniLog.Log("* Reset");
                    Listeners.ForEach(l => l.TriggerReset(new MIDI_SystemRealtimeEventData()));
                    break;

                // other types of messages: channel message (voice or channel mode), system common message, system exclusive message
                case MidiEvent.NoteOn:
                    if (DEBUG) UniLog.Log("* NoteOn");
                    if (e.Lsb == 0)
                    {
                        if (DEBUG) UniLog.Log("* Zero velocity, so it's actually a NoteOff");
                        Listeners.ForEach(l => l.TriggerNoteOff(new MIDI_NoteEventData(e.Channel, e.Msb, e.Lsb)));
                        break;
                    }
                    Listeners.ForEach(l => l.TriggerNoteOn(new MIDI_NoteEventData(e.Channel, e.Msb, e.Lsb)));
                    break;
                case MidiEvent.NoteOff:
                    if (DEBUG) UniLog.Log("* NoteOff");
                    Listeners.ForEach(l => l.TriggerNoteOff(new MIDI_NoteEventData(e.Channel, e.Msb, e.Lsb)));
                    break;
                case MidiEvent.CAf:
                    if (DEBUG) UniLog.Log("* CAf");
                    Listeners.ForEach(l => l.TriggerChannelAftertouch(new MIDI_ChannelAftertouchEventData(e.Channel, e.Msb)));
                    break;
                case MidiEvent.Pitch:
                    if (DEBUG) UniLog.Log("* Pitch");
                    Listeners.ForEach(l => l.TriggerPitchWheel(new MIDI_PitchWheelEventData(e.Channel, CombineBytes(e.Msb, e.Lsb))));
                    break;
                case MidiEvent.PAf:
                    if (DEBUG) UniLog.Log("* PAf");
                    Listeners.ForEach(l => l.TriggerPolyphonicAftertouch(new MIDI_PolyphonicAftertouchEventData(e.Channel, e.Msb, e.Lsb)));
                    break;

                // Unhandled events:

                case MidiEvent.SysEx1:
                    if (DEBUG) UniLog.Log("UnhandledEvent: SysEx1");
                    break;
                case MidiEvent.SysEx2:
                    // AKA EndSysEx
                    if (DEBUG) UniLog.Log("UnhandledEvent: SysEx2");
                    break;
                case MidiEvent.MtcQuarterFrame:
                    if (DEBUG) UniLog.Log("UnhandledEvent: MtcQuarterFrame");
                    break;
                case MidiEvent.SongPositionPointer:
                    if (DEBUG) UniLog.Log("UnhandledEvent: SongPositionPointer");
                    break;
                case MidiEvent.SongSelect:
                    if (DEBUG) UniLog.Log("UnhandledEvent: SongSelect");
                    break;
                case MidiEvent.TuneRequest:
                    if (DEBUG) UniLog.Log("UnhandledEvent: TuneRequest");
                    break;

                default:
                    shouldBuffer = true;
                    break;
            }

            if (shouldBuffer)
            {
                // buffer CC messages because consecutive ones may need to be combined
                // also buffer Program messages
                lock (_eventBuffer)
                {
                    _eventBuffer.Add(new TimestampedMidiEvent(e, timestamp));
                }
            }
        }

        int count;
        lock (_eventBuffer)
            count = _eventBuffer.Count;

        if (count > 0 && timestamp - _lastEventBufferStartTime > BUFFER_TIME_MILLISECONDS)
        {
            _lastEventBufferStartTime = timestamp;
            if (DEBUG) UniLog.Log("* New message batch created: " + timestamp.ToString());
            await Task.Delay((int)BUFFER_TIME_MILLISECONDS);
            lock (_eventBuffer)
                FlushMessageBuffer();
        }
    }
}

public static class MidiDeviceConnectionManager
{
    private static Dictionary<string, MidiInputConnection> _deviceConnectionMap = new();

    private static Dictionary<IMidiInputListener, MidiInputConnection> _listenerConnectionMap = new();

    public static MidiInputConnection RegisterInputListener(IMidiInputListener listener, IMidiPortDetails details)
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
                UniLog.Log("No more listeners. Releasing input device connection. Device name: " + conn.Input.Details.Name);
                Task.Run(() => ReleaseInputConnection(conn));
            }
        }
    }

    private static async Task ReleaseInputConnection(MidiInputConnection conn)
    {
        UniLog.Log("Releasing input device...");
        await Task.WhenAny(conn.Input.CloseAsync(), Task.Delay(5000));
        if (conn.Input.Connection == MidiPortConnectionState.Closed)
            UniLog.Log("Device released.");
        else
            UniLog.Log("Timed out. Device may not be released.");
        _deviceConnectionMap.Remove(conn.Input.Details.Name);
        conn.Initialize();
        Pool<MidiInputConnection>.ReturnCleaned(ref conn);
    }

    private static MidiInputConnection CreateInputConnection(IMidiPortDetails details)
    {
        var input = MidiAccessManager.Default.OpenInputAsync(details.Id).Result;
        var conn = Pool<MidiInputConnection>.Borrow();
        conn.Input = input;
        input.MessageReceived += conn.OnMessageReceived;
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