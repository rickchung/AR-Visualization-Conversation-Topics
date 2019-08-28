using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


/// <summary>
/// Voice controller. This class controls the microphone and audio files on the
/// device.
/// </summary>
public class VoiceController : MonoBehaviour
{
    public SttNetworkManager networkManager;
    public AudioMixerGroup mixerGroupMic, mixerGroupMaster;
    public bool playMicrophoneInRealTime;

    public bool sendEveryClip, sendCumulativeClip, sendFinalClip;

    public int CLIP_SIZE;
    public int SAMPLING_RATE;  // Recommended: 16000
    private AudioSource audioSource;

    private string micName;
    private string mergedFilePath;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (playMicrophoneInRealTime)
        {
            audioSource.outputAudioMixerGroup = mixerGroupMaster;
        }
        else
        {
            audioSource.outputAudioMixerGroup = mixerGroupMic;
        }

        if (Microphone.devices.Length > 0)
            micName = Microphone.devices[0];
    }

    // ================================================

    /// <summary>
    /// Toggles the microphone. This method is used in button callback events.
    /// </summary>
    public void ToggleMicrophone()
    {
        if (!Microphone.IsRecording(micName))
        {
            DataLogger.Log(this.gameObject, LogTag.AUDIO_CTRL, "Start recording.");
            StartMicInterval();
        }
        else
        {
            DataLogger.Log(this.gameObject, LogTag.AUDIO_CTRL, "Stop recording.");
            StopMicInterval();
        }
    }

    // ================================================

    private void StartMicInterval()
    {
        StartCoroutine(CheckMic());
    }

    private IEnumerator CheckMic()
    {
        // Start the microphone
        audioSource.clip = Microphone.Start(micName, true, CLIP_SIZE, SAMPLING_RATE);
        audioSource.loop = true;

        // For real-time playback, set latency to 0.
        int latency = 0;
        while (!(Microphone.GetPosition(micName) > latency)) { }
        audioSource.Play();

        // For the merged audio file
        int clipCount = 0;
        AudioClip mergedClip = null;
        var mergedClipData = new List<float>();
        string mergedClipName = "MergedMicAudio-" + (DateTime.Now.ToString()
            .Replace('/', '-').Replace(':', '-').Replace(' ', '-'));

        while (Microphone.IsRecording(micName))
        {
            yield return new WaitForSeconds(CLIP_SIZE);

            // Save the clip after waiting
            if (audioSource.clip != null)
            {
                clipCount++;

                // Copy all the data
                float[] clipData = new float[audioSource.clip.samples];
                audioSource.clip.GetData(clipData, 0);
                mergedClipData.AddRange(clipData);

                // Create a new merged clip
                mergedClip = AudioClip.Create(
                    "merged", mergedClipData.Count, audioSource.clip.channels,
                    audioSource.clip.frequency, false);
                mergedClip.SetData(mergedClipData.ToArray(), 0);

                // Save the merged file
                mergedFilePath = SaveMicFile(mergedClipName, mergedClip);

                // Send an STT request for every clip
                if (sendEveryClip)
                {
                    networkManager.RequestSpeechToText(SaveMicFile(audioSource.clip));
                }

                // Send an STT request for a cumulative clip
                if (sendCumulativeClip)
                {
                    networkManager.RequestSpeechToText(mergedFilePath);
                }

                // Cleanup the clip of mic
                float[] zeros = new float[audioSource.clip.samples];
                Array.Clear(zeros, 0, zeros.Length);
                audioSource.clip.SetData(zeros, 0);
            }
        }

        // Clear the audio source
        audioSource.clip = null;
        // Send one STT request for a final cumulative clip
        if (sendFinalClip)
        {
            networkManager.RequestSpeechToText(mergedFilePath);
        }
    }

    private void StopMicInterval()
    {
        // Stop the audio source is it's playing
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        // Stop the microphone (this will stop the coroutine)
        Microphone.End(micName);
    }

    // ================================================

    private string SaveMicFile(AudioClip clip)
    {
        // Get current date time as file name
        string timestamp = DateTime.Now.ToString();
        timestamp = timestamp.Replace('/', '-').Replace(':', '-').Replace(' ', '-');
        string micFilename = "MicAudio-" + timestamp;

        // Save the clip and return the new filename
        return SaveMicFile(micFilename, clip);
    }

    private string SaveMicFile(string filename, AudioClip clip)
    {
        return SavWav.Save(filename, clip);
    }
}
