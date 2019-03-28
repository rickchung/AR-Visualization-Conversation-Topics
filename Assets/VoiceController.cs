using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class VoiceController : MonoBehaviour
{
    public NetworkManager networkManager;
    public AudioMixerGroup mixerGroupMic, mixerGroupMaster;
    public bool playMicrophoneInRealTime = false;

    private const int CLIP_SIZE = 4;
    private const int SAMPLING_RATE = 16000;
    private AudioSource audioSource;

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

    }

    public void ToggleMicrophone()
    {
        string micName = null;
        if (Microphone.devices.Length > 0)
        {
            micName = Microphone.devices[0];
        }

        if (!Microphone.IsRecording(micName))
        {
            Debug.Log("[LOG] Start recording at " + micName);
            StartMicrophone(micName);
        }
        else
        {
            Debug.Log("[LOG] Stop recording at " + micName);
            StopMicrophone(micName);
        }
    }

    private void StartMicrophone(string micName)
    {
        StartCoroutine(CheckMic(micName, CLIP_SIZE));
    }

    private IEnumerator CheckMic(string micName, int clipSizeSec)
    {
        int latency = 0;

        // Start the microphone
        audioSource.clip = Microphone.Start(micName, true, clipSizeSec, SAMPLING_RATE);
        audioSource.loop = true;

        // For real-time playback, set latency to 0.
        while (!(Microphone.GetPosition(micName) > latency)) { }
        audioSource.Play();

        while (Microphone.IsRecording(micName))
        {
            yield return new WaitForSeconds(clipSizeSec);
            // Save the clip after waiting
            if (audioSource.clip != null)
            {
                string filePath = SaveMicFile(audioSource.clip);

                // Speech-to-text here
                networkManager.RequestSpeechToText(filePath);
            }
        }
    }

    private void StopMicrophone(string micName)
    {
        // Stop the audio source is it's playing
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        // Stop the microphone (this will stop the coroutine)
        Microphone.End(micName);
        // Clear the audio source
        audioSource.clip = null;
    }

    private string SaveMicFile(AudioClip clip)
    {
        // Get current date time as file name
        string timestamp = DateTime.Now.ToString();
        timestamp = timestamp.Replace('/', '-').Replace(':', '-').Replace(' ', '-');
        string micFilename = "MicAudio-" + timestamp;

        // Save the clip
        string newFilePath = SavWav.Save(micFilename, clip);

        return newFilePath;
    }
}
