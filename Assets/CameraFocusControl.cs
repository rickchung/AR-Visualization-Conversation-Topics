using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class CameraFocusControl : MonoBehaviour
{
    void Start()
    {
        var vuforiaInstance = VuforiaARController.Instance;
        vuforiaInstance.RegisterVuforiaStartedCallback(SetCameraAF);
        vuforiaInstance.RegisterOnPauseCallback(OnVuforiaPaused);
    }

    private void SetCameraAF()
    {
        bool focusModeSet = CameraDevice.Instance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
        if (!focusModeSet)
        {
            Debug.Log("Failed to set focus mode.");
        }
    }

    private void OnVuforiaPaused(bool paused)
    {
        if (!paused)
        {
            SetCameraAF();
        }
    }
}
