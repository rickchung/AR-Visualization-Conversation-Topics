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

    private int CLIP_SIZE = 5;
    private int SAMPLING_RATE = 16000;  // Recommended: 16000

    private float micSensitivity = 0.7f;

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
        ToggleMicrophone(!Microphone.IsRecording(micName));
    }

    public void ToggleMicrophone(bool value)
    {
        if (value)
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

    private AudioClip mergedClip;
    private int mergedClipCount;
    private const int MERGED_CLIP_LIMIT = 6;
    private List<float> mergedClipData;
    private string mergedClipName;

    private void RenewMergedClip()
    {
        mergedClip = null;
        mergedClipData = new List<float>();
        mergedClipName = "MergedMicAudio-" + (DateTime.Now.ToString()
            .Replace('/', '-').Replace(':', '-').Replace(' ', '-'));
        mergedClipCount = 0;
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

        RenewMergedClip();
        while (Microphone.IsRecording(micName))
        {
            yield return new WaitForSeconds(CLIP_SIZE);

            // Save the clip after waiting
            if (audioSource.clip != null)
            {
                // Copy all the data
                float[] clipData = new float[audioSource.clip.samples];
                audioSource.clip.GetData(clipData, 0);

                // Fire another coroutine here to process the data
                StartCoroutine(_ProcessMicClip(clipData));

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
            if (mergedFilePath != null)
            {
                networkManager.RequestSpeechToText(mergedFilePath);
            }
        }
    }

    private IEnumerator _ProcessMicClip(float[] clipData)
    {
        // Check volume
        var micVolume = Mathf.Max(clipData);
        // Debug.LogWarning("mic volume = " + micVolume);
        if (micVolume > micSensitivity)
        {
            // Append the new clip to the merged clip
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

            // To prevent using too much memory
            mergedClipCount++;
            if (mergedClipCount >= MERGED_CLIP_LIMIT)
            {
                RenewMergedClip();
            }
        }
        yield return null;
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
