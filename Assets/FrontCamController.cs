using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class FrontCamController : MonoBehaviour
{
    public RawImage frontCamTexture;
    public AspectRatioFitter frontCamAspFilter;
    private bool isFrontCamAvailable;
    private WebCamTexture frontCam;
    private float camTimeElapsed;
    private const float SHUTTER_SPEED = 1.0f;


    void Start()
    {
        frontCamTexture.gameObject.SetActive(false);
        InitFrontCam();
    }

    void Update()
    {
        if (isFrontCamAvailable)
        {
            UpdateCamTexture(frontCam, frontCamTexture, frontCamAspFilter);

            camTimeElapsed += Time.deltaTime;
            if (camTimeElapsed >= SHUTTER_SPEED)
            {
                SaveCamTexture(frontCam);
                camTimeElapsed = 0;
            }
        }
    }

    public void ToggleFrontCam(bool value)
    {
        if (frontCam)
        {
            if (value)
            {
                frontCamTexture.gameObject.SetActive(true);
                isFrontCamAvailable = true;
                frontCam.Play();
            }
            else
            {
                frontCamTexture.gameObject.SetActive(false);
                isFrontCamAvailable = false;
                frontCam.Pause();
            }
        }
    }

    /// <summary>
    /// Create device camera instances by WebCamTexture (Unity API).
    /// </summary>
    private void InitFrontCam()
    {
        // Get camera device list
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.Log("There is no available camera");
            isFrontCamAvailable = false;
            return;
        }
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing)
            {
                Debug.Log("Front facing camera found: " + devices[i].name);
                frontCam = new WebCamTexture(devices[i].name, 160, 120);
                frontCam.requestedFPS = 16;

            }
        }

        frontCamTexture.texture = frontCam;
    }


    /// <summary>
    /// This helper function is used in Update to render the device camera view on the RawImage
    /// background and apply the aspect ratio filter.
    /// </summary>
    /// <param name="cam">The camera to use (WebCamTexture)</param>
    /// <param name="background">The background image (RawImage)</param>
    /// <param name="ratioFilter">The aspect ratio filter (AspectRatioFilter)</param>
    private void UpdateCamTexture(WebCamTexture cam, RawImage background,
                                  AspectRatioFitter ratioFilter)
    {
        float ratio = (float)cam.width / (float)cam.height;
        ratioFilter.aspectRatio = ratio;

        float scaleY = cam.videoVerticallyMirrored ? -1f : 1f;
        background.rectTransform.localScale = new Vector3(1f, scaleY, 1f);

        // To correct the orientation
        int orient = -cam.videoRotationAngle;
        background.rectTransform.localEulerAngles = new Vector3(0, 0, orient);
    }

    /// <summary>
    /// Save one frame of camera image
    /// </summary>
    /// <param name="cam"></param>
    private void SaveCamTexture(WebCamTexture cam)
    {
        Texture2D snapshot = new Texture2D(cam.width, cam.height);
        snapshot.SetPixels(cam.GetPixels());
        snapshot.Apply();
        var image = snapshot.EncodeToJPG();
        DataLogger.LogImage(image, "FrontCam");
    }
}
