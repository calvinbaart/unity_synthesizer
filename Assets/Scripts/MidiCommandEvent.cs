/*
The MIT License (MIT)

Copyright (c) 2015 Calvin Baart

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using UnityEngine;

public class MidiCommandEvent : MidiEvent
{
    public uint Time;
    public int Command;
    public int Channel;
    public byte[] Args;

    public Midi Midi;
    public MidiTrack Track;

    public MidiCommandEvent(uint time, int command, int channel, byte[] args, Midi midi, MidiTrack track)
    {
        int note = args[0] % 12;
        int octave = (args[0] - note) / 11;

        this.Time = time;
        this.Command = command;
        this.Channel = channel;
        this.Args = args;

        this.Midi = midi;
        this.Track = track;
    }

    public MidiEventType GetEventType()
    {
        return MidiEventType.CommandEvent;
    }

    public uint GetTime()
    {
        return Time;
    }

    public void Execute()
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        switch (Command)
        {
            //Note off
            //Note on
            case 0x80:
            case 0x90:
            {
                int note = Args[0] % 12;
                int octave = ((Args[0] - note) / 11) - 1;

                float velocity = ((float)Args[1]) / 0xF8;

                bool on = (Command != 0x80);
                if (Midi.KeysPressed[note][octave] && on)
                    on = false;

                Midi.KeysPressed[note][octave] = on;
                Midi.GetSynthesizer().SetNote(Channel, note, octave, velocity, on);
            }
            break;

            //Polyphonic Key Pressure
            case 0xA0:
            {
                Debug.Log("Polyphonic Key Pressure");
            }
            break;

            //Controller Change
            case 0xB0:
            {
                int controller = Args[0];
                int value = Args[1];

                switch (controller)
                {
                    //Bank Select
                    case 0x0:
                    case 0x20:
                    {
                        Debug.Log("Bank Select: " + value);
                    }
                    break;

                    //Modulation wheel
                    case 0x01:
                    {
                        Debug.Log("Modulation wheel: " + value);
                    }
                    break;

                    //Data Entry
                    case 0x06:
                    case 0x26:
                    {
                        Debug.Log("Data Entry: " + value);
                    }
                    break;

                    //Coarse Volume
                    case 0x07:
                    case 0x27:
                    {
                        Midi.GetSynthesizer().SetVolume(Channel, value, controller == 0x27);
                    }
                    break;

                    //Coarse Pan
                    case 0x0A:
                    case 0x2A:
                    {
                        Midi.GetSynthesizer().SetPan(Channel, value, controller == 0x2A);
                    }
                    break;

                    //Coarse Expression
                    case 0x0B:
                    case 0x2B:
                    {
                        Midi.GetSynthesizer().SetExpression(Channel, value, controller == 0x2B);
                    }
                    break;

                    //Sustain Pedal
                    case 0x40:
                    {
                        Midi.GetSynthesizer().SetSustain(Channel, value >= 64);
                    }
                    break;

                    //Brightness
                    case 0x4A:
                    {
                        Debug.Log("Brightness: " + value);
                    }
                    break;

                    //Effects 1 Depth / Reverb Send Level
                    case 0x5B:
                    {
                        Midi.GetSynthesizer().SetReverb(Channel, value);
                    }
                    break;

                    //Effects 2 Depth (formerly Tremolo Depth)
                    case 0x5C:
                    {
                        Debug.Log("Effects 2 Depth: " + value);
                    }
                    break;

                    //Effects 3 Depth / Chorus Send Level
                    case 0x5D:
                    {
                        Debug.Log("Effects 3 Depth: " + value);
                    }
                    break;

                    //Effects 4 Depth (formerly Celeste [Detune] Depth)
                    case 0x5E:
                    {
                        Debug.Log("Effects 4 Depth: " + value);
                    }
                    break;

                    //Effects 5 Depth (formerly Phaser Depth)
                    case 0x5F:
                    {
                        Debug.Log("Effects 5 Depth: " + value);
                    }
                    break;

                    //Non-Registered Parameter Number (NRPN)
                    case 0x62:
                    case 0x63:
                    {
                        Debug.Log("Non-Registered Parameter Number: " + value);
                    }
                    break;

                    //Registered Parameter Number
                    case 0x64:
                    case 0x65:
                    {
                        Debug.Log("Registered Parameter Number: " + value);
                    }
                    break;

                    //All controllers off
                    case 0x79:
                    {
                        Debug.Log("All controllers off: " + value);
                    }
                    break;

                    //All notes off
                    case 0x7B:
                    {
                        Debug.Log("All notes off: " + value);
                    }
                    break;

                    default:
                        Debug.LogError("Unknown controller: '" + controller + "' (Value = '" + value + "')");
                    break;
                };
            }
            break;

            //Program Change
            case 0xC0:
            {
                Instrument instrument = (Instrument)(Args[0] + 1);
                Midi.GetSynthesizer().SetInstrument(Channel, instrument);
            }
            break;

            //Channel Key Pressure
            case 0xD0:
            {
                Debug.Log("Channel Key Pressure");
            }
            break;

            //Pitch Bend
            case 0xE0:
            {
                int val = (Args[1] << 7) | Args[0];

                Midi.GetSynthesizer().SetPitchBend(Channel, val);
            }
            break;

            default:
            {
                Debug.Log("Unknown midi command: '" + Command + "'.");
            }
            break;
        }
    }
}
