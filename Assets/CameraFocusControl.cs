using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class CameraFocusControl : MonoBehaviour
{
    public bool initWithARCamera;

    public Camera normalCamera;
    public Camera arCamera;
    public Transform arImageTarget;
    public Transform nonArTarget;
    public Transform viewContainer;

    private Quaternion prevRot;
    private Vector3 prevPos;

    public bool IsAREnabled
    {
        get
        {
            return arCamera.gameObject.activeSelf;
        }
    }

    void Start()
    {
        var vuforiaInstance = VuforiaARController.Instance;
        vuforiaInstance.RegisterVuforiaStartedCallback(SetCameraAF);
        vuforiaInstance.RegisterOnPauseCallback(OnVuforiaPaused);

        normalCamera.gameObject.SetActive(false);
        if (!initWithARCamera)
        {
            ToggleARCamera();
        }
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

    // ========

    public void ToggleARCamera()
    {
        normalCamera.gameObject.SetActive(!normalCamera.gameObject.activeSelf);
        arCamera.gameObject.SetActive(!arCamera.gameObject.activeSelf);

        if (normalCamera.gameObject.activeSelf)
        {
            prevRot = viewContainer.rotation;
            prevPos = viewContainer.position;

            viewContainer.SetParent(nonArTarget);
            viewContainer.rotation = new Quaternion();
            viewContainer.position = Vector3.zero;

            _SwitchArHelper(viewContainer, true);
        }

        if (arCamera.gameObject.activeSelf)
        {
            viewContainer.rotation = prevRot;
            viewContainer.position = prevPos;
            viewContainer.SetParent(arImageTarget);

            _SwitchArHelper(viewContainer, false);
        }
    }

    public void ToggleARCamera(bool enabled)
    {
        if (IsAREnabled == enabled)
            return;

        normalCamera.gameObject.SetActive(!enabled);
        arCamera.gameObject.SetActive(enabled);

        if (!enabled)
        {
            prevRot = viewContainer.rotation;
            prevPos = viewContainer.position;

            viewContainer.SetParent(nonArTarget);
            viewContainer.rotation = new Quaternion();
            viewContainer.position = Vector3.zero;

            _SwitchArHelper(viewContainer, true);
        }
        else
        {
            viewContainer.rotation = prevRot;
            viewContainer.position = prevPos;
            viewContainer.SetParent(arImageTarget);

            _SwitchArHelper(viewContainer, false);
        }
    }

    private void _SwitchArHelper(Transform child, bool enabled)
    {
        foreach (Transform c in child)
        {
            var t1 = c.GetComponent<MeshRenderer>();
            var t2 = c.GetComponent<BoxCollider>();
            if (t1 != null) t1.enabled = enabled;
            if (t2 != null) t2.enabled = enabled;
            _SwitchArHelper(c, enabled);
        }
    }
}
