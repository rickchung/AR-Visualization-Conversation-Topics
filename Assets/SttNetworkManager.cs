using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Stt network manager gets an audio fild and sends it to the server. This
/// class handles all the network connection works.
/// </summary>
public class SttNetworkManager : MonoBehaviour
{

    private const string HOST_ADDR = "http://10.218.106.151:8081/main/upload/";
    private const int TIMEOUT = 10;

    public SpeechToTextController sttController;
    public PartnerSocket partnerSocket;
    public bool debugEchoUserStt;

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
            sttController.SaveTransResponse(stt);

            if (debugEchoUserStt) sttController.UpdateVis();

            // Broadcast the new transcript extracted from the user's speech
            partnerSocket.BroadcastNewTranscript(stt.transcript);
            partnerSocket.BroadcasetNewKeywords(stt.topics);
        }
    }

    public void ToggleEcho(bool activated)
    {
        debugEchoUserStt = activated;
    }
}
