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
using System.IO;
using System;
using System.Collections.Generic;

public class Synthesizer : MonoBehaviour
{
    #region Constants
    public static double Threshold = 0.5;
    public static float SoundThreshold = 0.00005f;
    public static float DampSpeed = 0.87f;
    private const double e = 2.7182818284590452353602874713526624977572470936999595;
    #endregion

    #region Public Variables
    public Midi midi;
    public MidiSoundPack soundPack;
    public GUIContent[] comboBoxList;
    #endregion

    #region Callbacks
    public delegate void OnNotePressedDelegate(int note, int octave, float velocity);
    public delegate void OnNoteReleasedDelegate(int note, int octave);
    public delegate void OnStartedPlayingDelegate();
    public delegate void OnStoppedPlayingDelegate();

    public OnNotePressedDelegate OnNotePressed;
    public OnNoteReleasedDelegate OnNoteReleased;
    public OnStartedPlayingDelegate OnStartedPlaying;
    public OnStoppedPlayingDelegate OnStoppedPlaying;
    #endregion

    #region Private Variables
    private double dspTime = 0.0;
    private List<ClipInfo> clips;
    private bool useMixing = true;

    private List<Instrument> instruments = new List<Instrument>(new Instrument[]{
        Instrument.AcousticGrandPiano
    });

    private AudioChannel[] channels;
    
    private GUIStyle listStyle = new GUIStyle();
    private ComboBox comboBoxControl = new ComboBox();
    #endregion

    #region Unity Built-ins
    /// <summary>
    ///  Called by Unity when the Component is enabled. This function initializes the AudioChannels and sets up the UI style.
    /// </summary>
    void OnEnable()
    {
        clips = new List<ClipInfo>();

        listStyle.normal.textColor = Color.white;
        listStyle.onHover.background = listStyle.hover.background = new Texture2D(2, 2);
        listStyle.padding.left = listStyle.padding.right = listStyle.padding.top = listStyle.padding.bottom = 4;

        channels = new AudioChannel[16]{
            new AudioChannel(1),
            new AudioChannel(2),
            new AudioChannel(3),
            new AudioChannel(4),
            new AudioChannel(5),
            new AudioChannel(6),
            new AudioChannel(7),
            new AudioChannel(8),
            new AudioChannel(9),
            new AudioChannel(10),
            new AudioChannel(11),
            new AudioChannel(12),
            new AudioChannel(13),
            new AudioChannel(14),
            new AudioChannel(15),
            new AudioChannel(16)
        };
	}

    /// <summary>
    ///  Called by Unity when the Component should render the UI.
    /// </summary>
    protected void OnGUI()
    {
        float x = 10;

        int selectedItemIndex = comboBoxControl.GetSelectedItemIndex();
        selectedItemIndex = comboBoxControl.List(new Rect(x, 0, 300, 32), comboBoxList[selectedItemIndex].text, comboBoxList, listStyle);
        x += 310;

        if (GUI.Button(new Rect(x, 0, 100, 32), (midi == null) ? "Play" : "Stop"))
        {
            if (midi != null)
            {
                StopPlaying();
            }
            else
            {
                string fileName = comboBoxList[selectedItemIndex].text;

                TextAsset asset = Resources.Load<TextAsset>("Midis/" + fileName.Substring(0, fileName.LastIndexOf(".")));
                Stream s = new MemoryStream(asset.bytes);
                BinaryReader br = new BinaryReader(s);

                midi = new Midi(br, this);
                if (!midi.Load())
                {
                    midi = null;
                }

                StartPlaying();
            }
        }
        x += 110;

        useMixing = GUI.Toggle(new Rect(x, 0, 100, 32), useMixing, "Use Mixing");
        x += 110;
    }
    #endregion

    #region Setters
    /// <summary>
    ///  Called by the application to set the properties of a specific note & octave.
    /// </summary>
    /// <param name="channelIndex">The channel to apply the change to. (range: 0-16)</param>
    /// <param name="note">The note to apply the change to.</param>
    /// <param name="octave">The octave to apply the change to.</param>
    /// <param name="velocity">The velocity of the key press. (Only applicable when on = true).</param>
    /// <param name="on">Determines wether the note is on or off.</param>
    public void SetNote(int channelIndex, int note, int octave, float velocity, bool on)
    {
        AudioChannel channel = channels[channelIndex];
        if (!instruments.Contains(channel.instrument))
            return;

        if (on)
        {
            ClipInfo info = soundPack.GetSampleData(note, octave, channelIndex, velocity);
            if (info == null)
                return;

            info.Note = note;
            info.Octave = octave;
            info.Pressed = true;

            clips.Add(info);
            channel.Clips.Add(info);

            if (OnNotePressed != null)
                OnNotePressed(note, octave, velocity);
        }
        else
        {
            for (var i = 0; i < channel.Clips.Count; i++)
            {
                ClipInfo info = channel.Clips[i];
                if (info.Note != note || info.Octave != octave) continue;

                channel.Clips.RemoveAt(i);
                info.Pressed = false;

                break;
            }

            if (OnNoteReleased != null)
                OnNoteReleased(note, octave);
        }
    }

    /// <summary>
    ///  Called by the applicatiion to set the volume of a specific channel.
    /// </summary>
    /// <param name="channelIndex">The channel to apply the change to. (range: 0-16)</param>
    /// <param name="volume">Volume of the channel</param>
    /// <param name="isFine">Determines if the volume set is the coarse or fine volume.</param>
    public void SetVolume(int channelIndex, int volume, bool isFine)
    {
        AudioChannel c = channels[channelIndex];

        if (!isFine)
            c.coarseVolume = volume;
        else
            c.fineVolume = volume;

        CalculateVolume(channelIndex);
    }

    /// <summary>
    ///  Called by the application to set the expression of a specific channel.
    /// </summary>
    /// <param name="channelIndex">The channel to apply the change to. (range: 0-16)</param>
    /// <param name="expression">Expression of the channel</param>
    /// <param name="isFine">Determines if the expression set is the coarse or fine expression.</param>
    public void SetExpression(int channelIndex, int expression, bool isFine)
    {
        AudioChannel c = channels[channelIndex];

        if (!isFine)
            c.coarseExpression = expression;
        else
            c.fineExpression = expression;

        CalculateVolume(channelIndex);
    }

    /// <summary>
    ///  Called by the application to set the pan of a specific channel.
    /// </summary>
    /// <param name="channelIndex">The channel to apply the change to. (range: 0-16)</param>
    /// <param name="pan">Pan of the channel</param>
    /// <param name="isFine">Determines if the pan set is the coarse or fine pan.</param>
    public void SetPan(int channelIndex, int pan, bool isFine)
    {
        AudioChannel c = channels[channelIndex];

        if (!isFine)
            c.coarsePan = pan;
        else
            c.finePan = pan;
    }

    /// <summary>
    ///  Called by the application to set the reverb of a specific channel.
    /// </summary>
    /// <param name="channelIndex">The channel to apply the change to. (range: 0-16)</param>
    /// <param name="reverb">Reverb of the channel</param>
    public void SetReverb(int channelIndex, int reverb)
    {
        AudioChannel c = channels[channelIndex];
        c.SetReverb(reverb / 127.0f);
    }

    /// <summary>
    ///  Called by the application to set the sustain of a specific channel.
    /// </summary>
    /// <param name="channelIndex">The channel to apply the change to. (range: 0-16)</param>
    /// <param name="sustain">Sustain of the channel</param>
    public void SetSustain(int channelIndex, bool sustain)
    {
        AudioChannel c = channels[channelIndex];
        c.sustain = sustain;
    }

    /// <summary>
    ///  Called by the application to set the Pitch bend of a specific channel.
    /// </summary>
    /// <param name="channelIndex">The channel to apply the change to. (range: 0-16)</param>
    /// <param name="bend">Bend of the channel</param>
    public void SetPitchBend(int channelIndex, int bend)
    {
        AudioChannel c = channels[channelIndex];
        c.pitchBend = bend / 16384.0f;
    }

    /// <summary>
    ///  Called by the application to set the Instrument of a specific channel
    /// </summary>
    /// <param name="channelIndex">The channel to apply the change to. (range: 0-16)</param>
    /// <param name="instrument">Instrument of the channel</param>
    public void SetInstrument(int channelIndex, Instrument instrument)
    {
        AudioChannel c = channels[channelIndex];
        c.instrument = instrument;

        if (instruments.Contains(instrument)) return;

        Debug.LogError("Channel '" + channelIndex + "' switched to unimplemented instrument '" + instrument.ToString() + "'.");
        c.instrument = Instrument.AcousticGrandPiano;
    }
    #endregion

    #region Misc
    /// <summary>
    ///  Calculates the actual volume of the channel based on the volume and expression (both coarse and fine)
    /// </summary>
    /// <param name="channelIndex">The channel to apply the change to. (range: 0-16)</param>
    public void CalculateVolume(int channelIndex)
    {
        AudioChannel c = channels[channelIndex];

        int baseVolume = ((c.coarseVolume << 7) | c.fineVolume) * ((c.coarseExpression << 7) | c.fineExpression);
        float calculatedVolume = baseVolume / (((float)0x3FFF) * 0x3FFF);

        c.volume = calculatedVolume;
    }
    
    /// <summary>
    ///  Called by the Synthesizer when the Synthesizer starts playing.
    /// </summary>
    public void StartPlaying()
    {
        if (OnStartedPlaying != null)
            OnStartedPlaying();
    }

    /// <summary>
    ///  Called by the Synthesizer when the Synthesizer stops playing.
    /// </summary>
    public void StopPlaying()
    {
        if (OnStoppedPlaying != null)
            OnStoppedPlaying();

        for (int i = 0; i < 16; i++)
        {
            channels[i].Reset();
        }

        midi = null;
    }
    #endregion

    #region Audio Processing
    /// <summary>
    ///  Called by unity when new audio data is requested. This function processes active audio clips, puts them in the correct channel and applies dampening and volume changes
    ///  to the channel.
    /// </summary>
    /// <param name="data">Buffer to write the new audio data to</param>
    /// <param name="channels">Number of channels to request data for</param>
    protected void OnAudioFilterRead(float[] data, int channels)
    {
        double dt = AudioSettings.dspTime - dspTime;
        dspTime = AudioSettings.dspTime;

        if (midi != null)
        {
            if (!midi.Update(dt))
            {
                StopPlaying();
            }
        }

        if (clips == null)
        {
            return;
        }

        foreach (AudioChannel channel in this.channels)
        {
            channel.ResetData();
        }

        for (int j = clips.Count - 1; j >= 0; j--)
        {
            ClipInfo info = clips[j];
            AudioChannel c = this.channels[info.Channel];

            //Apply damping to the clip if the sustain pedal isn't down
            if (!c.sustain && !info.Pressed)
            {
                info.Damping *= DampSpeed;
            }

            float sum = 0.0f;
            for (int i = 0; i < c.data.Length; i += channels)
            {
                float current = c.data[i];
                float toAdd = info.Data[info.Index] * info.Velocity * info.Damping;

                c.data[i] = current + toAdd;
                sum += (toAdd * toAdd);

                current = c.data[i + 1];
                toAdd = info.Data[info.Index + 1] * info.Velocity * info.Damping;

                c.data[i + 1] = current + toAdd;
                sum += (toAdd * toAdd);

                info.Index += channels;

                if (info.Index >= info.Data.Length)
                {
                    break;
                }
            }

            //!in_range_of_clip || damping <= 0.001 || (!in_range_of_stop_index && (-SoundThreshold <= sum <= SoundThreshold))
            sum /= c.data.Length / 2;
            if (info.Index >= info.Data.Length || info.Damping <= 0.001f || (info.Index >= info.StopIndex && sum <= SoundThreshold && sum >= -SoundThreshold))
            {
                c.Clips.Remove(info);
                clips.RemoveAt(j);
            }
        }

        for (int j = 0; j < data.Length; j += channels)
        {
            float left = data[j];
            float right = data[j + 1];
            for (int i = 0; i < this.channels.Length; i++)
            {
                AudioChannel c = this.channels[i];
                c.Apply();

                left += c.data[j];
                right += c.data[j + 1];
            }

            if (useMixing)
            {
                left = (float)Mix(left, Threshold);
                right = (float)Mix(right, Threshold);
            }

            data[j] = left;
            data[j + 1] = right;
        }
    }

    /// <summary>
    ///  Mixing formula developed by Camiel. This function makes sure the audio sample stays within the -t/t range.
    /// </summary>
    /// <param name="sample"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    protected static double Mix(double sample, double threshold)
    {
        if (-threshold <= sample && sample <= threshold)
        {
            return sample;
        }

        double v = 1.0 - threshold;

        //-t <= x <= t
        //x>=0: t + (v - v * e ^ (-1/v) ^ (x - t))
        //x<0: -t - (v - v * e ^ (-1/v) ^ (-x - t))

        if (sample >= 0)
        {
            return threshold + (v - v * Math.Pow(Math.Pow(e, -1 / v), sample - threshold));
        }
        else
        {
            return -threshold - (v - v * Math.Pow(Math.Pow(e, -1 / v), -sample - threshold));
        }
    }
    #endregion
}
