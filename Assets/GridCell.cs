using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Triggered: " + gameObject.name + " by "  + other.gameObject.name);
    }

    private void OnTriggerExit(Collider other)
    {

    }
}
