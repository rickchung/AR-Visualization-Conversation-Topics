﻿using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Stt network manager gets an audio fild and sends it to the server. This
/// class handles all the network connection tasks.
/// </summary>
public class SttNetworkManager : MonoBehaviour
{

    private const string HOST_ADDR = "http://10.218.106.151:8081/main/upload/";
    private const int TIMEOUT = 10;

    public SpeechToTextController sttController;
    public PartnerSocket partnerSocket;

    [Tooltip("Whether to echo the user's transcription")]
    public bool showSttResultLocal;
    [Tooltip("Whether to broadcast the user's transcription by server")]
    public bool showSttResultRemote;

    /// <summary>
    /// A wrapper function which requests the speech file to text.
    /// </summary>
    /// <param name="audio_path">Audio path.</param>
    public void RequestSpeechToText(string audio_path)
    {
        byte[] data = File.ReadAllBytes(audio_path);

        // Prepare form data for POST request
        var formData = new WWWForm();
        formData.AddBinaryData("file", data);

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            DataLogger.Log(this.gameObject, LogTag.SYSTEM_WARNING, "No Internet Connection.");
        }
        else
        {
            StartCoroutine(PostRequest(formData));
        }
    }

    /// <summary>
    /// Posts the speech-to-text request.
    /// </summary>
    /// <returns>The request.</returns>
    /// <param name="formData">Form data.</param>
    private IEnumerator PostRequest(WWWForm formData)
    {
        // Append location message into the address
        string dest = string.Format(HOST_ADDR);
        DataLogger.Log(this.gameObject, LogTag.SYSTEM, "Posting a request to " + dest);

        // Obtain CSRF token first
        UnityWebRequest csrfRequest = UnityWebRequest.Get(dest);
        csrfRequest.timeout = TIMEOUT;
        yield return csrfRequest.SendWebRequest();

        if (csrfRequest.isNetworkError || csrfRequest.isHttpError)
        {
            DataLogger.Log(
                this.gameObject, LogTag.SYSTEM_ERROR,
                "Nework Failed: CSRF: " + csrfRequest.error
            );

        }
        else
        {
            // Look for the CSRF token in the cookie
            string cookieStr = csrfRequest.GetResponseHeader("Set-Cookie");
            string[] cookieContent = cookieStr.Split(';');
            string csrfToken = "";
            foreach (string c in cookieContent)
            {
                string[] kv = cookieContent[0].Split('=');
                if (kv[0].Trim().Equals("csrftoken"))
                    csrfToken = kv[1].Trim();
            }

            DataLogger.Log(
                this.gameObject, LogTag.SYSTEM,
                "CSRF request is successful: " + csrfToken
            );

            // Add csrf token to the form
            formData.AddField("csrfmiddlewaretoken", csrfToken);

            // Prepare and send a POST request
            UnityWebRequest request = UnityWebRequest.Post(dest, formData);
            request.timeout = TIMEOUT;

            // Set the header (both Cookie and X-CSRFToken must be set...)
            // Note: It's known that, by test, on different platforms the cookie header
            // has different behaviors...
#if UNITY_EDITOR
            // Works on UnityEditor
            request.SetRequestHeader("Cookie", "csrftoken=" + csrfToken);
#endif
#if UNITY_ANDROID
            // Works on Android
            request.SetRequestHeader("X-CSRFToken", csrfToken);
#endif

            // Fire
            yield return request.SendWebRequest();

            // Resolve the response
            if (!(request.isNetworkError || request.isHttpError))
            {
                ProcessSpeechToTextResult(request.downloadHandler.text);
            }
            else
            {
                DataLogger.Log(
                    this.gameObject, LogTag.SYSTEM_ERROR,
                    "Network error: " + request.error
                );
            }
        }
    }

    /// <summary>
    /// Processes the speech to text result. This method will be invoked when the
    /// speech-to-text result is returned from the server.
    /// </summary>
    /// <param name="result">Result.</param>
    private void ProcessSpeechToTextResult(string result)
    {
        SpToTextResult stt = SpToTextResult.CreateFromJson(result);
        DataLogger.Log(this.gameObject, LogTag.SYSTEM, "STT Raw Result, " + result);

        if (sttController != null && stt.keywords.Length > 0)
        {
            // Save the transcription locally
            sttController.SaveTransResponse(stt, isLocal: true);

            // Show the STT result at the local device
            if (showSttResultLocal)
            {
                sttController.UpdateVis();
            }
            // Show the STT result at the remote device
            if (showSttResultRemote)
            {
                // partnerSocket.BroadcastNewTranscript(stt.transcript);
                // partnerSocket.BroadcasetNewKeywords(stt.keywords);
                partnerSocket.BroadcasetNewKeywords(stt.transcript);  // transcripts as keywords
            }
        }
    }

    public void ToggleSttEchoLocal(bool activated)
    {
        showSttResultLocal = activated;
    }

    // ==========
    // Testing functions

    private int testIndex = 0;
    private string[] testCases = {
        "{\"topics\": [\"data and variables\"], \"keywords\": [\"k11\", \"k12\", \"k13\"], \"examples\": [[]], \"msg\": \"valid\", \"transcript\": [\"top 10 the development server and credential\"], \"subtopics\": [[]]}",
        "{\"topics\": [\"the continue and break statements\", \"break and continue keywords\"], \"keywords\": [\"k21\", \"k22\", \"k23\"], \"examples\": [[]], \"msg\": \"valid\", \"transcript\": [\"the development server and credential\"], \"subtopics\": [[]]}",
        "{\"topics\": [\"integer numbers\", \"floating point numbers\", \"instantiation and constructors\"], \"keywords\": [\"k31\", \"k32\", \"k33\"], \"examples\": [[]], \"msg\": \"valid\", \"transcript\": [\"server and credential\"], \"subtopics\": [[]]}"
    };
    public void TestReceivingSttResult()
    {
        string result = testCases[testIndex];
        testIndex++;
        if (testIndex >= testCases.Length)
            testIndex = 0;

        ProcessSpeechToTextResult(result);
    }

    public void TestReceivingRemoteStt()
    {
        string result = testCases[testIndex];
        SpToTextResult stt = SpToTextResult.CreateFromJson(result);
        sttController.SaveTransResponse(stt, false);
        sttController.UpdateVis();
    }
}
