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
using UnityEngine;

public class AudioChannel
{
    public static int NumChannels = 2;
    public static int SampleRate = 44100;

    public int ChannelId;
    public List<ClipInfo> Clips;

    //State Info
    public Instrument instrument;
    public int coarseVolume;
    public int coarseExpression;
    public int coarsePan;
    public int fineVolume;
    public int fineExpression;
    public int finePan;
    public bool sustain;

    public float volume;
    public float[] data;

    public float pitchBend;
    public float rvt;
    public bool changed;

    public AudioChannel(int channelID)
    {
        SampleRate = AudioSettings.outputSampleRate;

        this.ChannelId = channelID;
        this.Clips = new List<ClipInfo>();

        Reset();
    }

    public void Reset()
    {
        this.instrument = Instrument.AcousticGrandPiano;

        this.volume = 1.0f;
        this.coarseVolume = 127; // int(0.5 * 0xFE)
        this.coarseExpression = 127;
        this.coarsePan = 63; //int(0.5 * 0x7F)
        this.finePan = 63;
        this.fineExpression = 127; //int(0x7F)
        this.fineVolume = 127;

        this.data = new float[2048];

        this.sustain = false;
        this.pitchBend = 0.5f;
        this.changed = false;

        SetReverb(0.0f);
    }

    public void ResetData()
    {
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = 0.0f;
        }

        changed = true;
    }

    public void SetReverb(float rvt)
    {
        Debug.Log("Setting reverb: '" + rvt + "'.");
        
        this.rvt = rvt;
    }

    public void Apply()

    {
        if (!changed)
            return;

        //todo: implement filters and apply them here (for reverb etc.)

        changed = false;
        
        var targetSampleRate = (SampleRate - 18000) + (36000 * pitchBend);
        if (!Mathf.Approximately(targetSampleRate, SampleRate))
            Debug.LogWarning("TODO: Implement pitch bend. Target sample rate = '" + targetSampleRate + "'.");
    }
};
