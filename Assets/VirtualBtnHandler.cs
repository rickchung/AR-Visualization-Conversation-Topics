using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class VirtualBtnHandler : MonoBehaviour, IVirtualButtonEventHandler
{
    private VirtualButtonBehaviour virtualButton;
    private Vector3 cellPos;

    void Start()
    {
        virtualButton = GetComponent<VirtualButtonBehaviour>();
        virtualButton.RegisterEventHandler(this); 
    }

    public void SetVbName(string name)
    {
        virtualButton.name = name;
    }

    public void OnButtonPressed(VirtualButtonBehaviour vb)
    {
        Debug.Log(vb.VirtualButtonName + "is pressed.");
    }

    public void OnButtonReleased(VirtualButtonBehaviour vb)
    {
        Debug.Log(vb.VirtualButtonName + "is released.");
    }
}
