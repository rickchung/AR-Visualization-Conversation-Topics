using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class CameraFocusControl : MonoBehaviour
{
    public Camera normalCamera;
    public Camera arCamera;
    public Transform arImageTarget;
    public Transform nonArTarget;
    public Transform viewContainer;

    private Quaternion prevRot;
    private Vector3 prevPos;

    void Start()
    {
        var vuforiaInstance = VuforiaARController.Instance;
        vuforiaInstance.RegisterVuforiaStartedCallback(SetCameraAF);
        vuforiaInstance.RegisterOnPauseCallback(OnVuforiaPaused);

        normalCamera.gameObject.SetActive(false);
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

            foreach (Transform child in viewContainer)
            {
                child.GetComponent<MeshRenderer>().enabled = true;
                child.GetComponent<BoxCollider>().enabled = true;
            }
        }

        if (arCamera.gameObject.activeSelf)
        {
            viewContainer.rotation = prevRot;
            viewContainer.position = prevPos;
            viewContainer.SetParent(arImageTarget);

            foreach (Transform child in viewContainer)
            {
                child.GetComponent<MeshRenderer>().enabled = false;
                child.GetComponent<BoxCollider>().enabled = false;
            }
        }
    }
}
