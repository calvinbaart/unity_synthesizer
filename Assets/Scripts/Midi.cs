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

using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Midi
{
    public enum Format
    {
        SingleTrack,
        MultiTrackAsync,
        MultiTrackSync
    };

    public enum TimeFormat
    {
        FramesPerSecond,
        TicksPerBeat
    };

    private BinaryReader _reader;
    private bool _isVerified;
    private bool _verificationSuccessfull;
    private Format _format;
    private TimeFormat _timeFormat;
    private List<MidiTrack> _tracks = new List<MidiTrack>();
    private int _currentTrack;

    private int _division;
    private int _tempo = 500000;
    private double _time;
    private double _ppqnClock;

    private Synthesizer _synthesizer;
    public readonly bool[][] KeysPressed = new bool[][] { new bool[9], new bool[9], new bool[9], new bool[9], new bool[9], new bool[9], new bool[9], new bool[9], new bool[9], new bool[9], new bool[9], new bool[9], new bool[9], new bool[9], new bool[9], new bool[9] };

    public Midi(BinaryReader reader, Synthesizer synth)
    {
        _reader = reader;
        _isVerified = false;
        _verificationSuccessfull = false;
        _synthesizer = synth;
    }

    /// <summary>
    ///  Verifies the midi header.
    /// </summary>
    /// <returns>Whether the midi header is valid or not.</returns>
    public bool Verify()
    {
        if (_isVerified)
            return _verificationSuccessfull;

        byte[] header = _reader.ReadBytes(4);
        if (header[0] != 'M')
        {
            _isVerified = true;
            return false;
        }

        if (header[1] != 'T')
        {
            _isVerified = true;
            return false;
        }

        if (header[2] != 'h')
        {
            _isVerified = true;
            return false;
        }

        if (header[3] != 'd')
        {
            _isVerified = true;
            return false;
        }

        _verificationSuccessfull = true;
        _isVerified = true;
        return true;
    }

    /// <summary>
    ///  Load the midi file.
    /// </summary>
    /// <returns>Whether the midi file loaded.</returns>
    public bool Load()
    {
        if (!Verify())
            return false;

        int headerChunkLength = ReadInt32();
        short format = ReadInt16();
        short tracks = ReadInt16();
        short division = ReadInt16();

        switch (format)
        {
            case 0:
                _format = Format.SingleTrack;
                break;

            case 1:
                _format = Format.MultiTrackAsync;
                break;

            case 2:
                _format = Format.MultiTrackSync;
                break;

            default:
                Debug.LogError("Unknown Midi format '" + format + "'.");
                break;
        }

        _division = division & 0x7FFF;
        _timeFormat = ((division & 0x8000) > 0) ? TimeFormat.FramesPerSecond : TimeFormat.TicksPerBeat;
        _ppqnClock = (((double)_tempo) / division) * 0.000001;

        while (_reader.BaseStream.Position != _reader.BaseStream.Length)
        {
            string header = new string(_reader.ReadChars(4));
            if (header == "MTrk")
            {
                MidiTrack track = LoadTrackChunk();
                _tracks.Add(track);
            }
            else
            {
                Debug.Log("Unknown header type '" + header + "'.");
            }
        }

        return true;
    }

    /// <summary>
    ///  Step the midi file.
    /// </summary>
    /// <returns>Whether the midi file continued without errors.</returns>
    public bool Tick()
    {
        if (_format == Format.MultiTrackSync)
        {
            if (_currentTrack >= _tracks.Count)
                return false;

            if (!_tracks[_currentTrack].Tick())
                _currentTrack++;
        }
        else
        {
            bool found = false;
            foreach (MidiTrack track in _tracks)
            {
                if (track.Tick())
                    found = true;
            }

            if (!found)
                return false;
        }

        return true;
    }

    /// <summary>
    ///  Tick the midi file for dt time.
    /// </summary>
    /// <param name="dt">DeltaTime between this call and the previous call.</param>
    /// <returns>Whether the update succeeded</returns>
    public bool Update(double dt)
    {
        _time += dt;

        while (_time >= _ppqnClock)
        {
            if (!Tick())
                return false;

            _time -= _ppqnClock;
        }

        return true;
    }

    /// <summary>
    ///  Set the tempo of the playback.
    /// </summary>
    /// <param name="tempo">Tempo to change to</param>
    public void SetTempo(int tempo)
    {
        _tempo = tempo;
        _ppqnClock = (((double)tempo) / _division) * 0.000001;
    }

    /// <summary>
    ///  Get the Synthesizer
    /// </summary>
    /// <returns>The Synthesizer</returns>
    public Synthesizer GetSynthesizer()
    {
        return _synthesizer;
    }

    //http://www.midi.org/techspecs/midimessages.php
    /// <summary>
    ///  Loads the current MidiTrack from the Midi file.
    /// </summary>
    /// <returns>The loaded MidiTrack</returns>
    private MidiTrack LoadTrackChunk()
    {
        MidiTrack track = new MidiTrack(this);

        int length = ReadInt32();
        int command = 0;
        int midiChannel = 0;

        while (length > 0)
        {
            int numBytes = 0;
            uint vtime = ReadVariableLengthValue(out numBytes);
            length -= numBytes;
            byte eventType = _reader.ReadByte();
            length--;

            if (eventType >= 0xF0) //outside of normal event type range
            {
                switch (eventType)
                {
                    case 0xFF: //Meta Event
                    {
                        byte type = _reader.ReadByte();
                        length--;
                        uint eventLength = ReadVariableLengthValue(out numBytes);
                        length -= numBytes;
                        byte[] bytes = _reader.ReadBytes((int)eventLength);
                        length -= (int)eventLength;

                        track.AddEvent(new MidiMetaEvent(vtime, type, bytes, this, track));
                    }
                    break;

                    case 0xF0:
                    case 0xF7: //SysEx Event
                    {
                        uint sysexLength = ReadVariableLengthValue(out numBytes);
                        length -= numBytes;

                        byte[] bytes = _reader.ReadBytes((int)sysexLength);
                        length -= (int)sysexLength;

                        Debug.Log("Sysex message of length '" + sysexLength + "'.");
                    }
                    break;

                    default:
                        Debug.Log("Unknown event type '" + eventType + "'.");
                    break;
                }
            }
            else
            {
                if ((eventType & 0xF0) >= 0x80)
                {
                    //Channel specific midi command
                    command = (eventType & 0xF0);
                    midiChannel = (eventType & 0x0F);
                }
                else
                {
                    //Control Change command
                    _reader.BaseStream.Position--;
                    length++;
                }

                byte[] args;
                if (command != 0xC0 && command != 0xD0) //Program Change and Channel Key Pressure only have 1 argument instead of the default 2
                {
                    args = _reader.ReadBytes(2);
                    length -= 2;
                }
                else
                {
                    args = _reader.ReadBytes(1);
                    length -= 1;
                }

                track.AddEvent(new MidiCommandEvent(vtime, command, midiChannel, args, this, track));
            }
        }

        return track;
    }

    /// <summary>
    ///  Read a variable length value from the midi file.
    /// </summary>
    /// <param name="numBytes">number of bytes read for the variable length value.</param>
    /// <returns>the variable length value</returns>
    private uint ReadVariableLengthValue(out int numBytes)
    {
        uint ret = 0;
        numBytes = 0;

        while(numBytes < 4)
        {
            byte byteIn = _reader.ReadByte();
            numBytes++;

            ret = (ret << 7) | ((uint)(byteIn & 0x7f));
            if ((byteIn & 0x80) == 0)
                return ret;
        }

        return ret;
    }

    /// <summary>
    ///  Reads a 32-bit integer from the midi file.
    /// </summary>
    /// <returns>The value read</returns>
    private int ReadInt32()
    {
        return Util.ConvertToLittle(_reader.ReadInt32());
    }

    /// <summary>
    ///  Reads a 16-bit integer from the midi file.
    /// </summary>
    /// <returns>The value read</returns>
    private short ReadInt16()
    {
        return Util.ConvertToLittle(_reader.ReadInt16());
    }
}