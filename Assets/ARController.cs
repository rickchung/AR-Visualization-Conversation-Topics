using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARController : MonoBehaviour {

    public GameObject ARCamera;
    
	void Start() 
    {
        ARCamera.SetActive(false);	
	}

    public void ToggleARCamera()
    {
        ARCamera.SetActive(!ARCamera.activeSelf);
    }

}
