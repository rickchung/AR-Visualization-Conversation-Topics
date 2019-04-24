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

    public int CLIP_SIZE;
    public int SAMPLING_RATE;
    public int WHOLE_CLIP_SIZE = 60;
    private AudioSource audioSource;

    private string micName;

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
            Debug.Log("[LOG] Start recording");
            StartMicInterval();
            //StartMicWholeClip();
        }
        else
        {
            Debug.Log("[LOG] Stop recording");
            StopMicInterval();
            //StopMicWholeClip();
        }
    }

    // ================================================

    private void StartMicWholeClip()
    {
        audioSource.clip = Microphone.Start(micName, false, WHOLE_CLIP_SIZE, SAMPLING_RATE);
        audioSource.loop = true;
        int latency = 0;
        while (!(Microphone.GetPosition(micName) > latency)) { }
        audioSource.Play();
    }

    private void StopMicWholeClip()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();
        Microphone.End(micName);

        string filePath = SaveMicFile(audioSource.clip);
        // Speech-to-text here
        networkManager.RequestSpeechToText(filePath);

        audioSource.clip = null;
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

                // Save the audio and send an STT request
                //string filePath = SaveMicFile(audioSource.clip);
                //networkManager.RequestSpeechToText(filePath);  // clip
                string mergedFilePath = SaveMicFile(mergedClipName, mergedClip);
                networkManager.RequestSpeechToText(mergedFilePath);  // merged

                // Cleanup the clip of mic
                float[] zeros = new float[audioSource.clip.samples];
                Array.Clear(zeros, 0, zeros.Length);
                audioSource.clip.SetData(zeros, 0);
            }
        }

        // Clear the audio source
        audioSource.clip = null;
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
