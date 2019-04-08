using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CamController : MonoBehaviour
{
    public Transform background;

    void Start()
    {
        // Adjust the camera rotation 
        if (Application.isMobilePlatform)
        {
            GameObject cameraParent = new GameObject("camParent");
            cameraParent.transform.position = transform.position;
            transform.parent = cameraParent.transform;
            cameraParent.transform.Rotate(Vector3.right, 90);
        }

        // Set camera background
        var meshRenderer = background.GetComponent<MeshRenderer>();
        var webcamtexture = new WebCamTexture();
        meshRenderer.material.mainTexture = webcamtexture;
        webcamtexture.Play();

        // Enable gyro 
        Input.gyro.enabled = true;
    }

    void Update()
    {
        Quaternion gyro = Input.gyro.attitude;
        Quaternion camRotValue = new Quaternion(gyro.x, gyro.y, -gyro.z, -gyro.w);
        transform.localRotation = camRotValue;
    }
}
