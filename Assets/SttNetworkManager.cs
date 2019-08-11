using UnityEngine;
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
    public bool enableSttEchoLocal;
    [Tooltip("Whether to broadcast the user's transcription by server")]
    public bool enableSttBroadcastRemote;

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
            Debug.LogWarning("[WARNING] No Internet Connection");
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
        Debug.Log("[INFO] Sending Request to " + dest);

        // Obtain CSRF token first
        UnityWebRequest csrfRequest = UnityWebRequest.Get(dest);
        csrfRequest.timeout = TIMEOUT;
        yield return csrfRequest.SendWebRequest();

        if (csrfRequest.isNetworkError || csrfRequest.isHttpError)
        {
            Debug.LogError("[ERR] Nework Failed: CSRF: " + csrfRequest.error);
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

            Debug.Log("[INFO] CSRF request is successful: " + csrfToken);

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
                Debug.LogError("Network error: " + request.error);
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
        Debug.Log("STT Raw Result: " + result);

        if (sttController != null)
        {
            // Save the transcription locally
            sttController.SaveTransResponse(stt, isLocal: true);

            // Update the local UI if the option "echo" is enabled
            if (enableSttEchoLocal)
            {
                sttController.UpdateVis();
            }

            // Broadcast the new transcript extracted from the user's speech
            if (enableSttBroadcastRemote)
            {
                partnerSocket.BroadcastNewTranscript(stt.transcript);
                partnerSocket.BroadcasetNewKeywords(stt.topics);
            }
        }
    }

    public void ToggleSttEchoLocal(bool activated)
    {
        enableSttEchoLocal = activated;
    }

    // ==========
    // Testing functions

    private int testIndex = 0;
    private string[] testCases = {
        "{\"topics\": [\"data and variables\"], \"keywords\": [\"top\", \"development\", \"server\", \"and\", \"credential\", \"because\", \"elements\", \"in\", \"error\", \"when\"], \"examples\": [[]], \"msg\": \"valid\", \"transcript\": [\"top 10 the development server and credential\"], \"subtopics\": [[]]}",
        "{\"topics\": [\"the continue and break statements\", \"break and continue keywords\"], \"keywords\": [\"top\", \"development\", \"server\", \"and\", \"credential\", \"because\", \"elements\", \"in\", \"error\", \"when\"], \"examples\": [[]], \"msg\": \"valid\", \"transcript\": [\"the development server and credential\"], \"subtopics\": [[]]}",
        "{\"topics\": [\"integer numbers\", \"floating point numbers\", \"instantiation and constructors\"], \"keywords\": [\"top\", \"development\", \"server\", \"and\", \"credential\", \"because\", \"elements\", \"in\", \"error\", \"when\"], \"examples\": [[]], \"msg\": \"valid\", \"transcript\": [\"server and credential\"], \"subtopics\": [[]]}"
    };
    public void TestReceivingSttResult()
    {
        string result = testCases[testIndex];
        testIndex++;
        if (testIndex >= testCases.Length)
            testIndex = 0;

        ProcessSpeechToTextResult(result);
    }
}
