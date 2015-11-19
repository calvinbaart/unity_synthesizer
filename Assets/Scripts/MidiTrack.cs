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

public class MidiTrack
{
    private List<MidiEvent> _events = new List<MidiEvent>();
    private uint _tick;
    private Midi _midi;
    private int _index;
    public string Name;

    public MidiTrack(Midi midi)
    {
        _midi = midi;
        Name = "Track" + GetHashCode();
    }

    /// <summary>
    ///  Called by Midi to add an event to the MidiTrack.
    /// </summary>
    /// <param name="evt">The event to add</param>
    public void AddEvent(MidiEvent evt)
    {
        _events.Add(evt);
    }

    /// <summary>
    ///  Called by Midi to tick the MidiTrack.
    /// </summary>
    /// <returns>Whether the tick succeeded or failed</returns>
    public bool Tick()
    {
        _tick++;

        while (_index < _events.Count && _events[_index].GetTime() <= _tick)
        {
            _tick -= _events[_index].GetTime();
            _events[_index].Execute();
            _index++;
        }

        return _index < _events.Count;
    }
}
