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

using System.Linq;
using UnityEngine;

public class MidiMetaEvent : MidiEvent
{
    public Midi Midi;
    public MidiTrack Track;

    public uint Time;
    public int Type;
    public byte[] Bytes;

    public MidiMetaEvent(uint time, int type, byte[] bytes, Midi midi, MidiTrack track)
    {
        this.Time = time;
        this.Type = type;
        this.Bytes = bytes;

        this.Midi = midi;
        this.Track = track;
    }

    public void Execute()
    {
        switch (Type)
        {
            case 0x00:
            {
                //Sets the track's sequence number.
                Debug.Log("Track Sequence event");
            }
            break;

            case 0x01:
            {
                if (Bytes.Length > 0)
                {
                    //Text event
                    int length = Bytes[0];
                    string txt = System.Text.Encoding.Default.GetString(Bytes.Take(length).ToArray());
                }
                else
                {
                    Debug.LogWarning("Received text event with zero-bytes length.");
                }
            }
            break;

            case 0x02:
            {
                if (Bytes.Length > 0)
                {
                    //Copyright info
                    int length = Bytes[0];
                    string txt = System.Text.Encoding.Default.GetString(Bytes.Take(length).ToArray());
                }
                else
                {
                    Debug.LogWarning("Received copyright event with zero-bytes length.");
                }
            }
            break;

            case 0x03:
            {
                if (Bytes.Length > 0)
                {
                    //Sequence or Track name
                    int length = Bytes[0];
                    string name = System.Text.Encoding.Default.GetString(Bytes.Take(length).ToArray());
                    Track.Name = name;
                }
                else
                {
                    Debug.LogWarning("Received track name event with zero-bytes length.");
                }
            }
            break;

            case 0x04:
            {
                //Track instrument name
                Debug.Log("Instrument name event");
            }
            break;

            case 0x05:
            {
                //Lyric
                Debug.Log("Lyric event");
            }
            break;

            case 0x06:
            {
                //Marker
                Debug.Log("Marker event");
            }
            break;

            case 0x07:
            {
                //Cue point
                Debug.Log("Cue event");
            }
            break;

            case 0x20:
            {
                //Channel Prefix
                Debug.Log("Channel Prefix event");
            }
            break;

            case 0x21:
            {
                //MIDI Port/Cable
                Debug.Log("MIDI Port/Cable event");
            }
            break;

            case 0x2E:
            {
                //Track Loop
                Debug.Log("Track Loop event");
            }
            break;

            case 0x2F:
            {
                //This event must come at the end of each track
            }
            break;

            case 0x51:
            {
                //Set tempo
                Midi.SetTempo((Bytes[0] << 16) | (Bytes[1] << 8) | Bytes[2]);
            }
            break;

            case 0x54:
            {
                //SMPTE Offset
                Debug.Log("SMPTE Offset event");
            }
            break;

            case 0x58:
            {
                //Time Signature
                Debug.Log("Time Signature event");
            }
            break;

            case 0x59:
            {
                //Key Signature
            }
            break;

            case 0x7F:
            {
                //Sequencer specific information
            }
            break;

            default:
                Debug.LogWarning("Unknown midi meta event '" + Type + "'.");
            break;
        };
    }

    public MidiEventType GetEventType()
    {
        return MidiEventType.MetaEvent;
    }

    public uint GetTime()
    {
        return Time;
    }
}
