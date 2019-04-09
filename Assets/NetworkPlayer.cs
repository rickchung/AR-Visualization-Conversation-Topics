using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkPlayer : NetworkBehaviour
{
    private GameObject localController, remoteController;

    void Start()
    {
        if (!isLocalPlayer)
        {
            remoteController = GameObject.Find("Controller");
        }
        else
        {
            localController = GameObject.Find("Controller");
        }
    }

    void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }
    }
}
