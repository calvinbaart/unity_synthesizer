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

public class MidiSoundPack : MonoBehaviour
{
    public AudioClip[] C;
    public AudioClip[] D;
    public AudioClip[] E;
    public AudioClip[] F;
    public AudioClip[] G;
    public AudioClip[] A;
    public AudioClip[] B;

    public AudioClip[] BB;
    public AudioClip[] DB;
    public AudioClip[] EB;
    public AudioClip[] GB;
    public AudioClip[] AB;

    private AudioClip[][] Notes;
    private float[][][] SampleData;
    private int[][] SampleStopIndex;

    /// <summary>
    ///  Called by Unity on Start of the Component. Load and cache the processed sample data.
    /// </summary>
    public void Start()
    {
        Notes = new AudioClip[][] { C, DB, D, EB, E, F, GB, G, AB, A, BB, B };
        SampleData = new float[][][] { null, null, null, null, null, null, null, null, null, null, null, null };
        SampleStopIndex = new int[][] { null, null, null, null, null, null, null, null, null, null, null, null };
 
        for (int k = 0; k < Notes.Length; k++)
        {
            SampleData[k] = new float[][] { null, null, null, null, null, null, null, null, null };
            SampleStopIndex[k] = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 }; 

            for (int l = 0; l < Notes[k].Length; l++)
            {
                AudioClip clip = Notes[k][l];
                if (clip == null)
                    continue;
                
                clip.LoadAudioData();

                float[] data = new float[clip.samples * clip.channels];
                clip.GetData(data, 0);

                float highestValue = 0.0f;
                int stopIndex = 0;

                for (int i = 0; i < data.Length; i++)
                {
                    if (Mathf.Abs(data[i]) > highestValue)
                    {
                        highestValue = Mathf.Abs(data[i]);
                        stopIndex = i;
                    }
                }

                SampleData[k][l] = data;
                SampleStopIndex[k][l] = stopIndex;
            }
        }
    }

    /// <summary>
    ///  Called by the application when a sound sample for a specific note & octave is requested.
    /// </summary>
    /// <param name="note">The note to request the data for</param>
    /// <param name="octave">The octave to request the data for</param>
    /// <param name="channel">The channel on which this sample will be active</param>
    /// <param name="velocity">The velocity of the key press for this sample</param>
    /// <returns>a new ClipInfo instance containing the correct data or null</returns>
    public ClipInfo GetSampleData(int note, int octave, int channel, float velocity)
    {
        if (SampleData[note].Length <= octave)
        {
            Debug.Log("Octave out of range '" + octave + "' for note '" + note + "'.");
            return null;
        }

        ClipInfo info = new ClipInfo();
        info.Data = SampleData[note][octave];
        info.Index = 0;
        info.Channel = channel;
        info.Velocity = velocity;
        info.Damping = 1.0f;
        info.StopIndex = SampleStopIndex[note][octave];

        return info;
    }
}